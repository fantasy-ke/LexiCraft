using System.Security.Claims;
using BuildingBlocks.Authentication.Contract;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Authentication;

public sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId => FindGuidClaim(ClaimTypes.Sid, "sid", UserInfoConst.UserId);

    public string UserName => FindClaimValue(UserInfoConst.UserName) ?? string.Empty;

    public string UserAccount => FindClaimValue(UserInfoConst.UserAccount) ?? string.Empty;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string[] Roles => Principal?.Claims
        .Where(claim => claim.Type is ClaimTypes.Role or "role")
        .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];

    private Guid FindGuidClaim(params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = FindClaimValue(claimType);
            if (Guid.TryParse(value, out var result))
                return result;
        }

        return Guid.Empty;
    }

    private string? FindClaimValue(string claimType)
    {
        return Principal?.FindFirst(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal))?.Value;
    }
}
