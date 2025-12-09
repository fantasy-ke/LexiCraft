using System.Reflection;
using System.Text.Json.Nodes;
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
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Z.FreeRedis;

namespace BuildingBlocks.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    ///     添加Scalar
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="title"></param>
    /// <returns></returns>
    public static IEndpointRouteBuilder UseScalar(this IEndpointRouteBuilder builder, string title)
    {
        builder.MapOpenApi();

        builder.MapScalarApiReference((options =>
        {
            options.ShowDeveloperTools = DeveloperToolsVisibility.Always;
            options.WithTitle(title);
            options.WithTheme(ScalarTheme.BluePlanet);
            options.Authentication = new ScalarAuthenticationOptions()
            {
                PreferredSecuritySchemes = new List<string>() { "Bearer" },
            };
        }));

        return builder;
    }

    /// <param name="services"></param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     添加Scalar
        /// </summary>
        /// <param name="openApiInfo"></param>
        /// <returns></returns>
        public IServiceCollection WithScalar(OpenApiInfo openApiInfo)
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
                    if (context.JsonTypeInfo.Type.BaseType != typeof(Enum)) return Task.CompletedTask;
                    var list = new List<JsonNode>();
                    //获取枚举项
                    foreach (var enumValue in schema.Enum?.OfType<JsonNode>() ?? Enumerable.Empty<JsonNode>())
                    {
                        //把枚举项转为枚举类型
                        if (Enum.TryParse(context.JsonTypeInfo.Type, enumValue.GetValue<string>(), out var result))
                        {
                            //通过枚举扩展方法获取枚举描述
                            var description = ((Enum)result).GetDescription();
                            //重新组织枚举值展示结构（作为字符串 Json 节点加入）
                            list.Add(JsonValue.Create($"{enumValue.GetValue<string>()} - {description}"));
                        }
                        else
                        {
                            list.Add(enumValue);
                        }
                    }
            
                    schema.Enum = list;
                    return Task.CompletedTask;
                });
            });

            return services;
        }

        /// <summary>
        ///     添加IdGen
        /// </summary>
        /// <returns></returns>
        public void WithIdGen()
        {
            // <idGenerator name="foo" id="123"  epoch="2016-01-02T12:34:56" timestampBits="39" generatorIdBits="11" sequenceBits="13" tickDuration="0:00:00.001" />
            //  <idGenerator name="bar" id="987"  epoch="2016-02-01 01:23:45" timestampBits="20" generatorIdBits="21" sequenceBits="22" />
            //  <idGenerator name="baz" id="2047" epoch="2016-02-29" timestampBits="21" generatorIdBits="21" sequenceBits="21" sequenceOverflowStrategy="SpinWait" />
            services.AddIdGen(123, () => new IdGeneratorOptions()); // Where 123 is the generator-id
        }

        /// <summary>
        ///     跨域
        /// </summary>
        /// <param name="corsOptions"></param>
        public void ServicesCors(Action<CorsOptions> corsOptions)
        {
            var optionCor = new CorsOptions();
            corsOptions.Invoke(optionCor);
            services.AddCors(builder =>
                builder.AddPolicy(
                    optionCor.CorsName,
                    policyBuilder =>
                        policyBuilder
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .WithOrigins(optionCor.CorsArr!)
                )
            );
        }

        public void WithRedis(IConfiguration configuration)
        {
            //使用CsRedis
            var cacheOption = configuration.GetSection("App:RedisCache").Get<RedisCacheOptions>()!;

            if (cacheOption == null) throw new Exception("无法获取App:Cache  redis缓存配置");

            if (!cacheOption.Enable) return;
            services.AddZRedis(cacheOption, options => { options.Capacity = 6; });

            services.AddSingleton<ICacheManager, CacheManager>();
        }

        /// <summary>
        /// 添加Mapster映射
        /// </summary>
        /// <returns></returns>
        public IServiceCollection WithMapster()
        {
            var config = TypeAdapterConfig.GlobalSettings;
            config.Scan(Assembly.GetExecutingAssembly());
            var mapperConfig = new Mapper(config);
            services.AddSingleton<IMapper>(mapperConfig);
            return services;
        }
    }
}