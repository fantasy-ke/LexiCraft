namespace LexiCraft.Services.Identity.Shared;

public static class CacheKeys
{
    public static string GetUserPermissionByUserId(Guid guid)
    {
        return $"user_permissions_{guid}";
    }
}