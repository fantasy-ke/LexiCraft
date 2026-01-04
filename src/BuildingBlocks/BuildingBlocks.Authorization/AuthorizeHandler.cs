using BuildingBlocks.Authentication.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Authentication;

public sealed class AuthorizeHandler(
    IServiceProvider serviceProvider,
    IHttpContextAccessor contextAccessor) : AuthorizationHandler<AuthorizeRequirement>, IDisposable
{
    private readonly AsyncServiceScope _scope = serviceProvider.CreateAsyncScope();

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizeRequirement requirement)
    {
        AuthorizationFailureReason failureReason;

        var currentEndpoint = contextAccessor.HttpContext?.GetEndpoint();

        if(currentEndpoint is null)
        {
            failureReason = new AuthorizationFailureReason(this, "非法路由，What are you doing, man ?");
            context.Fail(failureReason);

            return;
        }

        var allowAnonymous = currentEndpoint.Metadata.GetMetadata<IAllowAnonymous>();
        // 如果是匿名访问，直接通过
        if (allowAnonymous is not null)
        {
            context.Succeed(requirement);
            return;
        }

        var authorizeData = currentEndpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().ToList();
        //默认授权策略
        if (!authorizeData.Any())
        {
            context.Succeed(requirement);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Fail();
            return;
        }
        
        // 检查是否启用Redis权限验证
        var oauthOptions = _scope.ServiceProvider.GetRequiredService<IOptionsMonitor<OAuthOptions>>();
        var redisEnabled = oauthOptions.CurrentValue.OAuthRedis.Enable;
        // 如果未启用Redis权限验证，直接通过（降级策略）
        if (!redisEnabled)
           goto next;
        
        var permissionCheck = _scope.ServiceProvider.GetRequiredService<IPermissionCheck>();
        // 检查所有需要的权限
        foreach (var permission in requirement.AuthorizeName)
        {
            if (await permissionCheck.IsGranted(permission)) continue;
            failureReason = new AuthorizationFailureReason(this,
                $"权限不足，缺少权限: {permission}，无法请求接口 {contextAccessor.HttpContext?.Request.Path ?? string.Empty}");

            context.Fail(failureReason);
            return;
        }
        next:
        context.Succeed(requirement);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TODO 在此释放托管资源
            _scope.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
}