using System.Security.Claims;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace BuildingBlocks.Authorization.Tests;

public sealed class AuthorizeHandlerTests
{
    [Fact]
    public async Task PermissionStoreFailure_IsReportedAsServiceUnavailable()
    {
        var httpContext = new DefaultHttpContext();
        var requirement = new AuthorizeRequirement("Pages.Test.Query");
        var authorizationContext = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "Bearer")),
            httpContext);
        var handler = new AuthorizeHandler(
            new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true },
            new CurrentAccessTokenValidator(),
            new ThrowingPermissionCheck(),
            new HttpContextAccessor { HttpContext = httpContext },
            NullLogger<AuthorizeHandler>.Instance);

        await handler.HandleAsync(authorizationContext);

        Assert.True(authorizationContext.HasFailed);
        Assert.True(httpContext.Items.TryGetValue(
            "BuildingBlocks.Authorization.ServiceUnavailable",
            out var marker));
        Assert.Equal(true, marker);
    }

    private sealed class CurrentAccessTokenValidator : IAccessTokenValidator
    {
        public Task<AccessTokenValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AccessTokenValidationResult.Current);
        }
    }

    private sealed class ThrowingPermissionCheck : IPermissionCheck
    {
        public Task<PermissionValidationResult> CheckAsync(
            IReadOnlyCollection<string> permissionNames,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Permission store is unavailable");
        }
    }
}
