using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Authentication;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Authentication.Permission;
using BuildingBlocks.Authentication.Shared;
using BuildingBlocks.Caching.Configuration;
using BuildingBlocks.Caching.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.Authorization.Tests;

public sealed class DistributedAuthorizationTests
{
    [Fact]
    public async Task RedisAccessTokenValidator_RequiresTheExactCurrentAccessToken()
    {
        var userId = Guid.NewGuid();
        var cache = new TestAuthorizationCache();
        await cache.SetAsync(
            string.Format(UserInfoConst.RedisAuthorizationSessionKey, userId.ToString("N")),
            new AccessTokenCacheEntry(
                AuthorizationTokenHasher.Hash("current-token"),
                AuthorizationTokenHasher.Hash("refresh-token")),
            TimeSpan.FromMinutes(5));
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer current-token";
        var validator = new RedisAccessTokenValidator(
            new HttpContextAccessor { HttpContext = context },
            new TestUserContext { UserId = userId, IsAuthenticated = true },
            cache,
            NullLogger<RedisAccessTokenValidator>.Instance);

        var currentResult = await validator.ValidateAsync();
        Assert.True(currentResult.SessionValid);
        Assert.True(currentResult.ServiceAvailable);

        context.Request.Headers.Authorization = "Bearer stale-token";
        var staleResult = await validator.ValidateAsync();
        Assert.False(staleResult.SessionValid);
        Assert.True(staleResult.ServiceAvailable);
    }

    [Fact]
    public async Task RedisAccessTokenValidator_IgnoresLegacySessionKeys()
    {
        var userId = Guid.NewGuid();
        var cache = new TestAuthorizationCache();
        await cache.SetAsync(
            string.Format(UserInfoConst.RedisTokenKey, userId.ToString("N")),
            new AccessTokenCacheEntry(
                AuthorizationTokenHasher.Hash("current-token"),
                AuthorizationTokenHasher.Hash("refresh-token")),
            TimeSpan.FromMinutes(5));
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer current-token";
        var validator = new RedisAccessTokenValidator(
            new HttpContextAccessor { HttpContext = context },
            new TestUserContext { UserId = userId, IsAuthenticated = true },
            cache,
            NullLogger<RedisAccessTokenValidator>.Instance);

        var result = await validator.ValidateAsync();

        Assert.False(result.SessionValid);
        Assert.True(result.ServiceAvailable);
    }

    [Fact]
    public async Task RedisAccessTokenValidator_ReportsSessionStoreFailureAsUnavailable()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer current-token";
        var validator = new RedisAccessTokenValidator(
            new HttpContextAccessor { HttpContext = context },
            new TestUserContext { UserId = Guid.NewGuid(), IsAuthenticated = true },
            new FailingAuthorizationCache(),
            NullLogger<RedisAccessTokenValidator>.Instance);

        var result = await validator.ValidateAsync();

        Assert.True(result.SessionValid);
        Assert.False(result.ServiceAvailable);
    }

    [Fact]
    public async Task IdentityApiPermissionCheck_ForwardsBearerTokenAndReturnsRemoteResult()
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            Assert.Equal("Bearer source-token", request.Headers.Authorization?.ToString());
            var payload = await request.Content!.ReadFromJsonAsync<PermissionValidationRequest>();
            Assert.Equal(["permission-a"], payload!.Permissions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(PermissionValidationResult.Allowed)
            };
        });
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer source-token";
        var check = CreateIdentityApiPermissionCheck(handler, context);

        var result = await check.CheckAsync(["permission-a"]);

        Assert.True(result.Granted);
        Assert.True(result.SessionValid);
        Assert.True(result.ServiceAvailable);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, false, true)]
    [InlineData(HttpStatusCode.InternalServerError, true, false)]
    public async Task IdentityApiPermissionCheck_DistinguishesInvalidSessionFromServiceFailure(
        HttpStatusCode statusCode,
        bool expectedSessionValid,
        bool expectedServiceAvailable)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer source-token";
        var check = CreateIdentityApiPermissionCheck(
            new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(statusCode))),
            context);

        var result = await check.CheckAsync(["permission-a"]);

        Assert.False(result.Granted);
        Assert.Equal(expectedSessionValid, result.SessionValid);
        Assert.Equal(expectedServiceAvailable, result.ServiceAvailable);
    }

    [Fact]
    public async Task RedisAuthorizationSynchronization_UsesTheAuthorizationRedisInstance()
    {
        var lockProvider = new TestDistributedLockProvider();
        var synchronization = new RedisAuthorizationSynchronization(lockProvider);

        var result = await synchronization.ExecuteAsync("permission:user-id", _ => Task.FromResult(42));

        Assert.Equal(42, result);
        Assert.Equal("authorization:permission:user-id", lockProvider.LastLockKey);
        Assert.Equal(AuthorizationExtensions.AuthorizationRedisInstanceName, lockProvider.LastRedisInstanceName);
    }

    [Fact]
    public async Task RedisAuthorizationSynchronization_ReportsLockFailureAsServiceUnavailable()
    {
        var synchronization = new RedisAuthorizationSynchronization(new FailingDistributedLockProvider());

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            synchronization.ExecuteAsync("permission:user-id", _ => Task.FromResult(42)));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    [Fact]
    public async Task RedisPermissionCache_ReportsInvalidationFailureAsServiceUnavailable()
    {
        var cache = new RedisPermissionCache(
            new FailingAuthorizationCache(),
            NullLogger<RedisPermissionCache>.Instance);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            cache.RemoveUserPermissionsAsync(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    [Fact]
    public void AddAuthorizationRedis_AppliesConfiguredDatabaseAndTimeouts()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RedisCache:ConnectTimeout"] = "1111",
            ["RedisCache:SyncTimeout"] = "2222",
            ["OAuthOptions:OAuthRedis:Enable"] = "true",
            ["OAuthOptions:OAuthRedis:ConnectionString"] = "localhost:6379,abortConnect=false",
            ["OAuthOptions:OAuthRedis:DefaultDatabase"] = "10",
            ["OAuthOptions:OAuthRedis:ConnectTimeout"] = "4321",
            ["OAuthOptions:OAuthRedis:SyncTimeout"] = "5432"
        });

        builder.Services.AddCaching(builder.Configuration);
        builder.RegisterAuthorization();
        builder.AddAuthorizationRedis();
        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RedisConnectionOptions>>().Value;
        var redisConfiguration = options.CreateConfigurationOptions(
            AuthorizationExtensions.AuthorizationRedisInstanceName);

        Assert.Equal(1111, options.ConnectTimeout);
        Assert.Equal(2222, options.SyncTimeout);
        Assert.Equal(10, redisConfiguration.DefaultDatabase);
        Assert.Equal(4321, redisConfiguration.ConnectTimeout);
        Assert.Equal(5432, redisConfiguration.SyncTimeout);
        Assert.Equal(5432, redisConfiguration.AsyncTimeout);
    }

    [Fact]
    public void AddAuthorizationRedis_RejectsDisabledAuthorizationRedis()
    {
        var builder = Host.CreateApplicationBuilder();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddAuthorizationRedis());

        Assert.Contains("must be enabled", exception.Message, StringComparison.Ordinal);
    }

    private static IdentityApiPermissionCheck CreateIdentityApiPermissionCheck(
        HttpMessageHandler handler,
        HttpContext context)
    {
        return new IdentityApiPermissionCheck(
            new HttpClient(handler) { BaseAddress = new Uri("http://identity/") },
            new HttpContextAccessor { HttpContext = context },
            new TestOptionsMonitor<PermissionAuthorizationOptions>(new PermissionAuthorizationOptions
            {
                IdentityApiBaseAddress = "http://identity/",
                IdentityApiValidationPath = "/api/v1/identity/permissions/validate"
            }),
            NullLogger<IdentityApiPermissionCheck>.Instance);
    }


    private sealed class FailingAuthorizationCache : IAuthorizationCache
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Redis is unavailable");
        }

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan expiration,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request);
        }
    }
}
