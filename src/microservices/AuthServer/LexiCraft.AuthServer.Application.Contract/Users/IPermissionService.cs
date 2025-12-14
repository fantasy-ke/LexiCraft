namespace LexiCraft.AuthServer.Application.Contract.Users;

public interface IPermissionService
{
    Task AssignDefaultPermissionsAsync(Guid userId);
    Task<HashSet<string>> GetUserAllPermissionsAsync(Guid userId);
}