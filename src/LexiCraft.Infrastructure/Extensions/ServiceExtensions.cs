using System.Reflection;
using System.Text;
using IdGen;
using IdGen.DependencyInjection;
using LexiCraft.Infrastructure.Authorization;
using LexiCraft.Infrastructure.Contract;
using LexiCraft.Infrastructure.EntityFrameworkCore;
using LexiCraft.Infrastructure.EntityFrameworkCore.Extensions;
using LexiCraft.Infrastructure.Redis;
using LexiCraft.Infrastructure.Shared;
using Mapster;
using MapsterMapper;
using Z.FreeRedis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
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
    
    public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
            if (authenticationSchemes.Any(authScheme => authScheme.Name == JwtBearerDefaults.AuthenticationScheme))
            {
                var requirements = new Dictionary<string, OpenApiSecurityScheme>
                {
                    [JwtBearerDefaults.AuthenticationScheme] = new()
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = JwtBearerDefaults.AuthenticationScheme.ToLower(),
                        In = ParameterLocation.Header,
                        BearerFormat = "Json Web Token"
                    }
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = requirements;
                foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
                {
                    operation.Value.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = JwtBearerDefaults.AuthenticationScheme,
                                Type = ReferenceType.SecurityScheme
                            }
                        }] = Array.Empty<string>()
                    });
                }
            }
        }
    }
    
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
            
            //可输入token
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
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
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
#if DEBUG
         options.EnableSensitiveDataLogging();
         options.EnableDetailedErrors();
#endif
        });
        services.Configure<ContextOption>(configuration.GetSection("DbContextOptions"));
        services.WithRepository<LexiCraftDbContext>();
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
            options.Title = title;
            options.Authentication = new ScalarAuthenticationOptions()
            {
                PreferredSecurityScheme = "Bearer",
            };
        }));

        return builder;
    }
}