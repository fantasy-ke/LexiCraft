using LexiCraft.Application.Authorize;
using LexiCraft.Application.Contract.Authorize;
using LexiCraft.Application.Contract.Events;
using LexiCraft.Application.Contract.Files;
using LexiCraft.Application.Contract.Verification;
using LexiCraft.Application.EventHandlers;
using LexiCraft.Application.Files;
using LexiCraft.Application.Verification;
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