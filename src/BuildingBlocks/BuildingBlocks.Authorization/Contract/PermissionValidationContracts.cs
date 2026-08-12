namespace BuildingBlocks.Authentication.Contract;

public sealed record PermissionValidationRequest(string[] Permissions);

public sealed record PermissionValidationResult(
    bool Granted,
    bool SessionValid,
    bool ServiceAvailable,
    string[] MissingPermissions)
{
    public static PermissionValidationResult Allowed { get; } = new(true, true, true, []);

    public static PermissionValidationResult InvalidSession { get; } = new(false, false, true, []);

    public static PermissionValidationResult Unavailable { get; } = new(false, true, false, []);

    public static PermissionValidationResult Denied(IEnumerable<string> permissions)
    {
        return new PermissionValidationResult(
            false,
            true,
            true,
            permissions.Where(permission => !string.IsNullOrWhiteSpace(permission))
                .Distinct(StringComparer.Ordinal)
                .ToArray());
    }
}