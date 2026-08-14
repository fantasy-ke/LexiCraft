using BuildingBlocks.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using BuildingBlocks.Authentication.Policies;

namespace BuildingBlocks.Authorization.Tests;

public sealed class AuthorizationResultHandlerTests
{
    [Fact]
    public async Task ServiceFailure_Returns503InsteadOfPermissionDenied()
    {
        var context = CreateHttpContext();
        context.Items["BuildingBlocks.Authorization.ServiceUnavailable"] = true;
        var handler = CreateHandler();

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            CreatePolicy(),
            PolicyAuthorizationResult.Forbid());

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvalidSessionAndMissingPermission_Return401And403()
    {
        var invalidSessionContext = CreateHttpContext();
        invalidSessionContext.Items["BuildingBlocks.Authorization.InvalidSession"] = true;
        var forbiddenContext = CreateHttpContext();
        var handler = CreateHandler();

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            invalidSessionContext,
            CreatePolicy(),
            PolicyAuthorizationResult.Forbid());
        await handler.HandleAsync(
            _ => Task.CompletedTask,
            forbiddenContext,
            CreatePolicy(),
            PolicyAuthorizationResult.Forbid());

        Assert.Equal(StatusCodes.Status401Unauthorized, invalidSessionContext.Response.StatusCode);
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenContext.Response.StatusCode);
    }

    private static AuthorizeResultHandle CreateHandler()
    {
        return new AuthorizeResultHandle(
            NullLogger<AuthorizeResultHandle>.Instance,
            new TestOptionsMonitor<JsonOptions>(new JsonOptions()));
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static AuthorizationPolicy CreatePolicy()
    {
        return new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    }
}
