using BuildingBlocks.Authentication.Contract;
using Humanizer;
using LexiCraft.Services.Identity.Users.Features.GetUserInfo;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Users.Features.UpdateUserInfo;

public static class UpdateUserInfoEndpoint
{
    internal static RouteHandlerBuilder MapUpdateUserInfoEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPut("users/info", Handle)
            .RequireAuthorization()
            .WithName(nameof(UpdateUserInfo))
            .WithDisplayName(nameof(UpdateUserInfo).Humanize())
            .WithSummary("更新当前用户信息".Humanize())
            .WithDescription(nameof(UpdateUserInfo).Humanize());

        async Task<GetUserInfoResponse> Handle(
            [AsParameters] UpdateUserInfoRequestParameters requestParameters)
        {
            var (mediator, userContext, request, cancellationToken) = requestParameters;

            var command = new UpdateUserInfoCommand(
                userContext.UserId,
                request.Username,
                request.Avatar);

            var result = await mediator.Send(command, cancellationToken);

            return new GetUserInfoResponse(
                result.UserId,
                result.UserName,
                result.Email,
                result.Phone,
                result.Avatar
            );
        }
    }
}

internal record UpdateUserInfoRequestParameters(
    IMediator Mediator,
    IUserContext UserContext,
    [FromBody] UpdateUserInfoRequest Request,
    CancellationToken CancellationToken
);

internal record UpdateUserInfoRequest(
    string? Username,
    string? Avatar
);

