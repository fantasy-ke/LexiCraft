using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Authentication.Shared;
using BuildingBlocks.Caching.Abstractions;
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

    public void Dispose()
    {
        Dispose(true);
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        AuthorizeRequirement requirement)
    {
        AuthorizationFailureReason failureReason;

        var currentEndpoint = contextAccessor.HttpContext?.GetEndpoint();

        if (currentEndpoint is null)
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
        if (authorizeData is [])
        {
            context.Succeed(requirement);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Fail();
            return;
        }

        // Redis白名单/黑名单校验：检查Token是否有效（是否在Redis中存在）
        if (!await CheckTokenValidityAsync(context)) return;

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

    /// <summary>
    ///     检查Token有效性（Redis白名单/黑名单校验）
    /// </summary>
    private async Task<bool> CheckTokenValidityAsync(AuthorizationHandlerContext context)
    {
        var cacheService = _scope.ServiceProvider.GetRequiredService<ICacheService>();
        var userContext = _scope.ServiceProvider.GetRequiredService<IUserContext>();

        if (userContext.UserId == Guid.Empty)
        {
            var failureReason = new AuthorizationFailureReason(this, "Token无效，缺少用户ID Claim");
            context.Fail(failureReason);
            return false;
        }

        // 检查Redis中是否存在该用户的Token记录
        var cacheKey = string.Format(UserInfoConst.RedisTokenKey, userContext.UserId.ToString("N"));
        var tokenExists = await cacheService.ExistsAsync(cacheKey);

        if (tokenExists) return true;
        {
            var failureReason = new AuthorizationFailureReason(this, "Token已失效或用户已登出，请重新登录");
            context.Fail(failureReason);
            return false;
        }
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
            // TODO 在此释放托管资源
            _scope.Dispose();
    }
}