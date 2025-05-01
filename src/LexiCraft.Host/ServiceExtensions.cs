using LexiCraft.Application.Contract.Events;
using LexiCraft.Application.EventHandlers;
using LexiCraft.Domain.Repository;
using LexiCraft.Infrastructure.EntityFrameworkCore;
using LexiCraft.Infrastructure.EntityFrameworkCore.Repository;
using Z.EventBus;

namespace LexiCraft.Host;

public static class ServiceExtensions
{
    public static IServiceCollection WithServiceLifetime(this IServiceCollection services)
    {
        //services.AddScoped<IAuthorizeService,AuthorizeService>();
        //services.AddScoped<IVerificationService,VerificationService>();
        services.AddScoped<IUserRepository<LexiCraftDbContext>, UserRepository>();
        services.AddScoped<IEventHandler<LoginEto>, LoginHandler>();
        return services;
    }
}