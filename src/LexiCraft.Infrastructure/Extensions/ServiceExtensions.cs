﻿using System.Text;
using IdGen;
using IdGen.DependencyInjection;
using LexiCraft.Infrastructure.Authorization;
using LexiCraft.Infrastructure.Contract;
using LexiCraft.Infrastructure.EntityFrameworkCore;
using LexiCraft.Infrastructure.EntityFrameworkCore.Extensions;
using LexiCraft.Infrastructure.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace LexiCraft.Infrastructure.Extensions;

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
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = openApiInfo;
                return Task.CompletedTask;
            });
            
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
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
    /// 添加Jwt访问
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection WithJwt(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();
        services.AddScoped<IUserContext, UserContext>();

        var option = configuration.GetSection(JwtOptions.Name);

        var jwtOption = option.Get<JwtOptions>();

        services.Configure<JwtOptions>(option);

        services.AddAuthorization()
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOption!.Issuer,
                    ValidAudience = jwtOption.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOption.Secret))
                };
            });

        return services;
    }


    /// <summary>
    /// 添加数据库访问
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection WithLexiCraftDbAccess(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.WithDbAccess<LexiCraftDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("LexiCraftDb"));
#if DEBUG
         options.EnableSensitiveDataLogging();
         options.EnableDetailedErrors();
#endif
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
    

    public static IEndpointRouteBuilder UseScalar(this IEndpointRouteBuilder builder, string title)
    {
        builder.MapOpenApi();

        builder.MapScalarApiReference((options =>
        {
            options.Title = title;
            options.Authentication = new ScalarAuthenticationOptions()
            {
                PreferredSecurityScheme = "Bearer",
            };
        }));

        return builder;
    }
}