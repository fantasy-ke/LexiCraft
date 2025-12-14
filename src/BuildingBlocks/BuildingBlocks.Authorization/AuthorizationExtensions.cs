using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Authentication.Permission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Authentication;

public static class AuthorizationExtensions
{
      public static IServiceCollection RegisterAuthorization(this IServiceCollection services)
      {
            // services.AddScoped<IPermissionCheck, PermissionCheck>();
            services.AddTransient<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
            services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizeResultHandle>();
            services.AddSingleton<IAuthorizationHandler, AuthorizeHandler>();
            services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();
            services.AddScoped<IUserContext, UserContext>();
            
            // 权限相关服务
            services.AddSingleton<IPermissionDefinitionManager, PermissionDefinitionManager>();
            
            return services;
      }
      
      /// <summary>
      /// 添加权限定义提供程序
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="services"></param>
      /// <returns></returns>
      public static IServiceCollection AddPermissionDefinitionProvider<T>(this IServiceCollection services)
        where T : PermissionDefinitionProvider
      {
          services.AddSingleton<PermissionDefinitionProvider, T>();
          return services;
      }
}