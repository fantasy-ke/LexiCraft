using BuildingBlocks.Authentication.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Authentication;

public static class AuthorizationExtensions
{
      public static IServiceCollection RegisterAuthorization(this IServiceCollection services)
      {
            services.AddScoped<IPermissionCheck, PermissionCheck>();
            services.AddTransient<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
            services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizeResultHandle>();
            services.AddSingleton<IAuthorizationHandler, AuthorizeHandler>();
            services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();
            services.AddScoped<IUserContext, UserContext>();
            return services;
      }
}