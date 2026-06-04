using System.IdentityModel.Tokens.Jwt;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureDbLoginHelper;

public sealed class TokenService
{
    public const string PostgresTokenScope = "https://ossrdbms-aad.database.windows.net/.default";
    public const string PimActivationUrl =
        "https://portal.azure.com/?feature.msaljs=true#view/Microsoft_Azure_PIMCommon/ActivationMenuBlade/~/aadgroup/provider/aadgroup";

    private readonly ILogger<TokenService> _logger;
    private readonly AzureDbLoginOptions _options;
    private InteractiveBrowserCredential? _credential;

    public TokenService(ILogger<TokenService> logger, IOptions<AzureDbLoginOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public bool HasRoles => _options.HasRoles;

    public DbRole? GetInitialRole() => _options.ResolveActiveRole();

    public async Task<TokenResult> AcquireTokenAsync(
        bool forceFresh,
        DbRole? role = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (forceFresh)
                _credential = null;

            var credential = GetCredential();
            var ctx = new TokenRequestContext(new[] { PostgresTokenScope });
            var token = await credential.GetTokenAsync(ctx, cancellationToken);

            if (string.IsNullOrEmpty(token.Token))
            {
                _logger.LogWarning("MSAL returned an empty token");
                return TokenResult.Fail("MSAL returned an empty token.");
            }

            if (role is { } activeRole && !string.IsNullOrWhiteSpace(activeRole.GroupObjectId))
            {
                if (!TokenContainsGroup(token.Token, activeRole.GroupObjectId, out var overage))
                {
                    var msg = overage
                        ? "Too many group memberships: Entra omitted the 'groups' claim. " +
                          "Postgres auth may fail — ask an admin about group-claim configuration."
                        : $"Token is missing the '{activeRole.Key}' group claim. " +
                          "Activate PIM for this role, then use Regenerate token.";

                    _logger.LogWarning(
                        "Group claim check failed for role {Role}. Overage={Overage}", activeRole.Key, overage);
                    return TokenResult.Fail(msg);
                }
            }

            return TokenResult.Ok(token.Token);
        }
        catch (AuthenticationFailedException ex)
        {
            _logger.LogError(ex, "MSAL authentication failed");
            return TokenResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring access token via MSAL");
            return TokenResult.Fail(ex.Message);
        }
    }

    public void OpenPimActivation()
    {
        PlatformLauncher.OpenUrl(PimActivationUrl);
        _logger.LogInformation("Opened PIM activation in browser");
    }

    private InteractiveBrowserCredential GetCredential() =>
        _credential ??= new InteractiveBrowserCredential(
            new InteractiveBrowserCredentialOptions
            {
                TenantId = string.IsNullOrWhiteSpace(_options.TenantId) ? null : _options.TenantId,
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = "AzureDbLoginHelper"
                }
            });

    private static bool TokenContainsGroup(string jwt, string groupObjectId, out bool overage)
    {
        overage = false;
        try
        {
            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

            var inGroups = parsed.Claims
                .Where(c => c.Type == "groups")
                .Any(c => string.Equals(c.Value, groupObjectId, StringComparison.OrdinalIgnoreCase));
            if (inGroups) return true;

            overage = parsed.Claims.Any(c => c.Type is "_claim_names" or "hasgroups");
            return false;
        }
        catch
        {
            return true;
        }
    }
}

public readonly record struct TokenResult(bool Success, string? Token, string? ErrorMessage)
{
    public static TokenResult Ok(string token) => new(true, token, null);
    public static TokenResult Fail(string message) => new(false, null, message);
}
