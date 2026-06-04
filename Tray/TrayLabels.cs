namespace AzureDbLoginHelper;

internal static class TrayLabels
{
    public const string PlainTokenMenuItem = "Plain token (no role check)";

    public static string TrayText(DbRole? activeRole) =>
        activeRole is { } role
            ? $"Azure DB Login — {role.DisplayName}"
            : "Azure DB Login";

    public static string RegenerateTokenMenuItem(bool hasRoles) =>
        hasRoles
            ? "Regenerate token (after PIM activation)"
            : "Regenerate token";

    public static string TokenCopiedMessage(DbRole? activeRole) =>
        activeRole is { } role
            ? $"{role.DisplayName} token is on the clipboard."
            : "Token is on the clipboard.";
}
