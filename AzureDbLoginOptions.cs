namespace AzureDbLoginHelper;

public class AzureDbLoginOptions
{
    public const string SectionName = "AzureDbLogin";

    public string? TenantId { get; set; }
    public string PostgresHost { get; set; } = "";
    public string Database { get; set; } = "";

    /// <summary>Key of the role selected at startup when <see cref="Roles"/> is configured.</summary>
    public string DefaultRole { get; set; } = "";

    public List<DbRole> Roles { get; set; } = new();

    public bool HasRoles => Roles.Count > 0;

    public DbRole? FindRole(string key) =>
        Roles.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the active role, or null when no roles are configured.</summary>
    public DbRole? ResolveActiveRole(string? preferredKey = null)
    {
        if (!HasRoles)
            return null;

        if (!string.IsNullOrWhiteSpace(preferredKey))
        {
            var match = FindRole(preferredKey);
            if (match != null)
                return match;
        }

        if (!string.IsNullOrWhiteSpace(DefaultRole))
        {
            var match = FindRole(DefaultRole);
            if (match != null)
                return match;
        }

        return Roles.FirstOrDefault();
    }
}

public class DbRole
{
    /// <summary>Short identifier, e.g. "read" or "write".</summary>
    public string Key { get; set; } = "";

    /// <summary>Human-readable label shown in the tray menu.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>The PostgreSQL role / username to connect as (matches the Entra group name).</summary>
    public string PgUser { get; set; } = "";

    /// <summary>Object id of the Entra group; must be present in the token's 'groups' claim.</summary>
    public string GroupObjectId { get; set; } = "";
}
