using BuildingBlocks.Authentication.Contract;

namespace BuildingBlocks.Authentication;

public interface IPermissionCheck
{
    Task<PermissionValidationResult> CheckAsync(IReadOnlyCollection<string> permissionNames,
        CancellationToken cancellationToken = default);
}
