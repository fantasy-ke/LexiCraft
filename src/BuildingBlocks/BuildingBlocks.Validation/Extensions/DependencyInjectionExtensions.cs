using System.Reflection;
using BuildingBlocks.Exceptions.Problem;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace BuildingBlocks.Validation.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCustomValidators(this IServiceCollection services, Assembly assembly)
    {
        services.Scan(scan =>
            scan.FromAssemblies(assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces()
                .WithTransientLifetime()
        );

        services.AddSingleton<IProblemCodeMapper, DefaultProblemCodeMapper>();

        return services;
    }
}