using System.Security.Claims;
using LexiCraft.Infrastructure.Contract;
using Microsoft.AspNetCore.Http;

namespace LexiCraft.Infrastructure.Authorization;

public class UserContext(IHttpContextAccessor httpContextAccessor): IUserContext
{
    private ClaimsPrincipal _principal => httpContextAccessor.HttpContext?.User;
    /// <summary>
    /// 用户id
    /// </summary>
    public virtual Guid UserId => FindClaimValue<Guid>(ClaimTypes.Sid);

    /// <summary>
    /// 用户名称
    /// </summary>
    public virtual string UserName => FindClaimValue<string>(UserInfoConst.UserName);

    /// <summary>
    /// 用户账号
    /// </summary>
    public string UserAccount => FindClaimValue<string>(UserInfoConst.UserAccount);


    /// <summary>
    /// 是否授权
    /// </summary>
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    
    /// <summary>
    /// 角色
    /// </summary>
    public string[] Roles
    {
        get
        {
            var roleClaim = FindClaimValue<string>(ClaimTypes.Role);
            return string.IsNullOrEmpty(roleClaim) ? [] : roleClaim.Split(',');
        }
    }

    protected virtual Claim? FindClaim(string claimType)
    {
        return _principal.FindFirst(c => c.Type == claimType);
    }

    private TType FindClaimValue<TType>(string claimType)
    {
        var claimValue = FindClaim(claimType)?.Value;
        
        try
        {
            // 处理可空类型（Nullable<T>）
            var targetType = typeof(TType);
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType);
            }

            return (TType)Convert.ChangeType(claimValue, targetType);
        }
        catch (Exception ex)
        {
            // 处理转换异常（可记录日志或抛出特定异常）
            throw new InvalidOperationException($"{claimType} try get user claims info error -- {claimValue} 转换为类型 {typeof(TType)}", ex);
        }
    }
}