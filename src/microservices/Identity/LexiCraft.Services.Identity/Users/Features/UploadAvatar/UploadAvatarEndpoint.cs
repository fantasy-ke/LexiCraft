using BuildingBlocks.Authentication.Contract;
using Humanizer;
using LexiCraft.Shared.Permissions;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Identity.Users.Features.UploadAvatar;

public static class UploadAvatarEndpoint
{
    internal static RouteHandlerBuilder MapUploadAvatarEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("uploadAvatar", Handle)
            .RequireAuthorization(UsersPermissions.UploadAvatar)
            .WithName(nameof(UploadAvatar))
            .WithDisplayName(nameof(UploadAvatar).Humanize())
            .WithSummary("上传用户头像".Humanize())
            .WithDescription(nameof(UploadAvatar).Humanize())
            .DisableAntiforgery();

        async Task<UploadAvatarResponse> Handle(
            [AsParameters] UploadAvatarRequestParameters requestParameters)
        {
            var (avatar, mediator, userContext, cancellationToken) = requestParameters;
            
            if (avatar == null)
            {
                throw new ArgumentException("头像文件不能为空");
            }
            
            var result = await mediator.Send(new UploadAvatarCommand(avatar, userContext.UserId), cancellationToken);
            return result.Adapt<UploadAvatarResponse>();
        }
    }
}

/// <summary>
/// 上传用户头像请求参数
/// </summary>
/// <param name="Mediator"></param>
/// <param name="UserContext"></param>
/// <param name="Avatar"></param>
/// <param name="CancellationToken"></param>
internal record UploadAvatarRequestParameters(
    [FromForm]  IFormFile? Avatar,
    IMediator Mediator,
    IUserContext UserContext,
    CancellationToken CancellationToken
);

/// <summary>
/// 上传头像响应
/// </summary>
/// <param name="AvatarUrl">头像URL</param>
/// <param name="FileId">文件ID</param>
internal record UploadAvatarResponse(
    string AvatarUrl,
    Guid? FileId
);