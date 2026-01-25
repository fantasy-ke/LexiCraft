using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Shared;
using IdGen;
using IdGen.DependencyInjection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Extensions;

public static class ServiceExtensions
{
    /// <param name="services"></param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public IServiceCollection ConfigureJson()
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
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

        public IServiceCollection WithCaptcha(IConfiguration configuration)
        {
            services.AddCaptcha(configuration);
            return services;
        }


        /// <summary>
        ///     添加Mapster映射
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