using System.Security.Claims;
using BuildingBlocks.Authentication.Contract;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Authentication;

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

    
    
    public string[] UserAllPermissions
    {
        get
        {
            var permClaim = FindClaimValue<string[]>(UserInfoConst.UserAllPermissions);
            return permClaim;
        }
    }

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

        if (claimValue == null)
        {
            return default;
        }
        try
        {
            // 处理可空类型（Nullable<T>）
            var targetType = typeof(TType);
            bool isNullable = false;
            Type underlyingType = targetType;
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Nullable.GetUnderlyingType(targetType);
            }
            // 如果原始值为空且目标类型可空，返回 null
            if (string.IsNullOrEmpty(claimValue) && isNullable)
                return default;
            

            object result = null;

            // 特殊处理 Guid 类型
            if (underlyingType == typeof(Guid))
            {
                if (Guid.TryParse(claimValue, out Guid guid))
                    result = guid;
                else if (!isNullable)
                    throw new InvalidCastException("字符串格式不符合 Guid");
            }
            // 其他基础值类型使用 Convert.ChangeType
            else if (underlyingType.IsValueType)
            {
                try
                {
                    result = Convert.ChangeType(claimValue, underlyingType);
                }
                catch
                {
                    if (!isNullable)
                        throw new InvalidCastException($"无法将 '{claimValue}' 转换为 {underlyingType.FullName}");
                    return default;
                }
            }
            // 字符串类型直接赋值
            else if (underlyingType == typeof(string))
            {
                result = claimValue;
            }
            else
            {
                throw new InvalidCastException($"不支持从字符串转换为 {underlyingType.FullName}");
            }
            return (TType)result;
        }
        catch (Exception ex)
        {
            // 处理转换异常（可记录日志或抛出特定异常）
            throw new InvalidOperationException($"{claimType} try get user claims info error -- {claimValue} 转换为类型 {typeof(TType)}", ex);
        }
    }
}