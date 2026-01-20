using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProtoBuf.Grpc.ClientFactory;

namespace BuildingBlocks.Grpc.Contracts.Extensions;

public static class DependencyInjectionExtensions
{
    public static IHostApplicationBuilder AddGrpcService<T>(this IHostApplicationBuilder builder,
        IConfiguration configuration) where T : class
    {
        //Grpc Services
        builder.Services.AddCodeFirstGrpcClient<T>(options =>
        {
            options.Address = new Uri(configuration["GrpcSettings:FilesUrl"]!);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            return handler;
        });
        return builder;
    }
}