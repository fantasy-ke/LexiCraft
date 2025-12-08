using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Grpc.Contracts.FileGrpc;
using LexiCraft.AuthServer.Application.Contract.Events;
using LexiCraft.AuthServer.Application.EventHandlers;
using LexiCraft.AuthServer.Domain.Repository;
using LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore;
using LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore.Repository;
using ProtoBuf.Grpc.ClientFactory;
using Z.EventBus;

namespace LexiCraft.AuthServer.Api;

public static class ServiceExtensions
{

    public static IServiceCollection ConfigureJson(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
        return services;
    }
    
    public static IServiceCollection AddGrpcService(this IServiceCollection services,  IConfiguration configuration)
    {
        //Grpc Services
        services.AddCodeFirstGrpcClient<IFilesService>(options =>
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
        return services;
    }
    
    public static IServiceCollection WithServiceLifetime(this IServiceCollection services)
    {
        //services.AddScoped<IAuthorizeService,AuthorizeService>();
        //services.AddScoped<IVerificationService,VerificationService>();
        services.AddScoped<IUserRepository<LexiCraftDbContext>, UserRepository>();
        services.AddScoped<IEventHandler<LoginEto>, LoginHandler>();
        return services;
    }
}