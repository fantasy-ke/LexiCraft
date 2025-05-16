namespace LexiCraft.Infrastructure.Authorization;

public interface IPermissionCheck
{
    Task<bool> IsGranted(string authorizationNames);
}