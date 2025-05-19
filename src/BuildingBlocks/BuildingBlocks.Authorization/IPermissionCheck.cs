namespace BuildingBlocks.Authentication;

public interface IPermissionCheck
{
    Task<bool> IsGranted(string authorizationNames);
}