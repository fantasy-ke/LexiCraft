﻿

using System.Reflection;
using BuildingBlocks.Extensions.System;
using BuildingBlocks.Redis;
using BuildingBlocks.Shared;
using IdGen;
using IdGen.DependencyInjection;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Z.FreeRedis;

namespace BuildingBlocks.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    /// 添加Scalar
    /// </summary>
    /// <param name="services"></param>
    /// <param name="openApiInfo"></param>
    /// <returns></returns>
    public static IServiceCollection WithScalar(this IServiceCollection services,OpenApiInfo openApiInfo)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = openApiInfo;
                return Task.CompletedTask;
            });
            
            //枚举展示描述
            options.AddSchemaTransformer((schema, context, _) =>
            {
                //找出枚举类型
                if (context.JsonTypeInfo.Type.BaseType == typeof(Enum))
                {
                    var list = new List<IOpenApiAny>();
                    //获取枚举项
                    foreach (var enumValue in schema.Enum.OfType<OpenApiString>())
                    {
                        //把枚举项转为枚举类型
                        if (Enum.TryParse(context.JsonTypeInfo.Type, enumValue.Value, out var result))
                        {
                            //通过枚举扩展方法获取枚举描述
                            var description = ((Enum)result).GetDescription();
                            //重新组织枚举值展示结构
                            list.Add(new OpenApiString($"{enumValue.Value} - {description}"));
                        }
                        else
                        {
                            list.Add(enumValue);
                        }
                    }
                    schema.Enum = list;
                }
                return Task.CompletedTask;
            });
        });

        return services;
    }
    

    /// <summary>
    /// 添加IdGen
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection WithIdGen(this IServiceCollection services)
    {
        // <idGenerator name="foo" id="123"  epoch="2016-01-02T12:34:56" timestampBits="39" generatorIdBits="11" sequenceBits="13" tickDuration="0:00:00.001" />
        //  <idGenerator name="bar" id="987"  epoch="2016-02-01 01:23:45" timestampBits="20" generatorIdBits="21" sequenceBits="22" />
        //  <idGenerator name="baz" id="2047" epoch="2016-02-29" timestampBits="21" generatorIdBits="21" sequenceBits="21" sequenceOverflowStrategy="SpinWait" />
        services.AddIdGen(123, () => new IdGeneratorOptions());  // Where 123 is the generator-id
        return services;
    }
    
    /// <summary>
    /// 跨域
    /// </summary>
    /// <param name="services"></param>
    /// <param name="corsOptions"></param>
    public static void ServicesCors(this IServiceCollection services, Action<CorsOptions> corsOptions)
    {
        var optionCor = new CorsOptions();
        corsOptions.Invoke(optionCor);
        services.AddCors(
            builder =>
                builder.AddPolicy(
                    name: optionCor.CorsName,
                    policyBuilder =>
                        policyBuilder
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .WithOrigins(optionCor.CorsArr)
                )
        );
    }


    public static IServiceCollection WithRedis(this IServiceCollection services,
        IConfiguration configuration)
    {
        //使用CsRedis
        var cacheOption = configuration.GetSection("App:RedisCache").Get<RedisCacheOptions>()!;

        if (cacheOption == null)
        {
            throw new Exception("无法获取App:Cache  redis缓存配置");
        }

        if (!cacheOption.Enable)
            return services;
        services.AddZRedis(cacheOption,options =>
        {
            options.Capacity = 6;
        });
        
        services.AddSingleton<ICacheManager, CacheManager>();
        
        return services;
    }
    
    /// <summary>
    /// 添加Mapster映射
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection WithMapster(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());
        var mapperConfig = new Mapper(config);
        services.AddSingleton<IMapper>(mapperConfig);
        return services;
    }
    
    /// <summary>
    /// 添加Scalar
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="title"></param>
    /// <returns></returns>
    public static IEndpointRouteBuilder UseScalar(this IEndpointRouteBuilder builder, string title)
    {
        builder.MapOpenApi();

        builder.MapScalarApiReference((options =>
        {
            options.WithTitle(title);
            options.WithTheme(ScalarTheme.BluePlanet);
            options.Authentication = new ScalarAuthenticationOptions()
            {
                PreferredSecurityScheme = "Bearer",
            };
        }));

        return builder;
    }
}