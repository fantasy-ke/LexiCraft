namespace BuildingBlocks.Authentication.Shared;

public sealed class PermissionAuthorizationOptions
{
    public string AdministratorRole { get; set; } = "admin";

    public string IdentityApiBaseAddress { get; set; } = "https+http://lexicraft-identity-api";

    public string IdentityApiValidationPath { get; set; } = "/api/v1/identity/permissions/validate";
}
