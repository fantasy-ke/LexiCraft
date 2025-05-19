using System.Text.Json;
using System.Text.Json.Serialization;
using LexiCraf.AuthServer.Application.Contract.Events;
using LexiCraft.AuthServer.Application.EventHandlers;
using LexiCraft.AuthServer.Domain.Repository;
using LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore;
using LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore.Repository;
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
    
    public static IServiceCollection WithServiceLifetime(this IServiceCollection services)
    {
        //services.AddScoped<IAuthorizeService,AuthorizeService>();
        //services.AddScoped<IVerificationService,VerificationService>();
        services.AddScoped<IUserRepository<LexiCraftDbContext>, UserRepository>();
        services.AddScoped<IEventHandler<LoginEto>, LoginHandler>();
        return services;
    }
}