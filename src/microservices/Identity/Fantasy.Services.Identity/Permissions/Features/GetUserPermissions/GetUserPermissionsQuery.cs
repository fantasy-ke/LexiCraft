using System.Net;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Abstractions;
using BuildingBlocks.Mediator;
using Fantasy.Shared.Models;
using Fantasy.Shared.Permissions;
using BuildingBlocks.Contexts;
using BuildingBlocks.Authentication.Permissions;

namespace Fantasy.Services.Identity.Permissions.Features.GetUserPermissions;

public record GetUserPermissionsQuery(UserId UserId) : IQuery<GetUserPermissionsResult>;

public record GetUserPermissionsResult(UserId UserId, List<string> Permissions);

public sealed class GetUserPermissionsQueryHandler(
    IUserPermissionStore permissionStore,
    IUserContext userContext,
    IPermissionCheck permissionCheck)
    : IQueryHandler<GetUserPermissionsQuery, GetUserPermissionsResult>
{
    public async Task<GetUserPermissionsResult> Handle(
        GetUserPermissionsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId.Value != userContext.UserId)
        {
            var authorizationResult = await permissionCheck.CheckAsync(
                [IdentityPermissions.Permissions.Query],
                cancellationToken);

            if (!authorizationResult.ServiceAvailable)
            {
                throw new HttpRequestException(
                    "The Identity authorization service is unavailable",
                    null,
                    HttpStatusCode.ServiceUnavailable);
            }

            if (!authorizationResult.SessionValid)
            {
                throw new HttpRequestException(
                    "The access token is no longer the current user session",
                    null,
                    HttpStatusCode.Unauthorized);
            }

            if (!authorizationResult.Granted)
                throw new UnauthorizedAccessException("No permission to query another user's permissions");
        }

        var permissions = await permissionStore.GetUserPermissionsAsync(
            query.UserId.Value,
            cancellationToken);

        return new GetUserPermissionsResult(query.UserId, permissions.ToList());
    }
}
