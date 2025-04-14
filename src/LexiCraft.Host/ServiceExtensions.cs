using LexiCraft.Application.Authorize;
using LexiCraft.Application.Contract.Authorize;
using LexiCraft.Application.Contract.Verification;
using LexiCraft.Application.Verification;
using LexiCraft.Domain.Repository;
using LexiCraft.Infrastructure.EntityFrameworkCore;
using LexiCraft.Infrastructure.EntityFrameworkCore.Repository;

namespace LexiCraft.Host;

public static class ServiceExtensions
{
    public static IServiceCollection WithServiceLifetime(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository<LexiCraftDbContext>, UserRepository>();
        services.AddScoped<IAuthorizeService,AuthorizeService>();
        services.AddScoped<IVerificationService,VerificationService>();
        return services;
    }
}