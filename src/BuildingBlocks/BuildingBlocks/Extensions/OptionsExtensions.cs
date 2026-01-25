using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfigurationOptions<T>(
        this IServiceCollection services,
        string? key = null,
        Action<T>? configurator = null
    )
        where T : class
    {
        var optionBuilder = services.AddOptions<T>().BindConfiguration(key ?? typeof(T).Name);

        if (configurator is not null) optionBuilder = optionBuilder.Configure(configurator);
        return services;
    }

    public static IServiceCollection AddValidationOptions<T>(
        this IServiceCollection services,
        string? key = null,
        Action<T>? configurator = null,
        Func<T, bool>? validator = null
    )
        where T : class
    {
        validator ??= RequiredConfigurationValidator.Validate;

        var optionBuilder = services.AddOptions<T>().BindConfiguration(key ?? typeof(T).Name);

        if (configurator is not null) optionBuilder = optionBuilder.Configure(configurator);

        optionBuilder.Validate(validator);
        return services;
    }
}

public static class RequiredConfigurationValidator
{
    public static bool Validate<T>(T arg)
        where T : class
    {
        var requiredProperties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => Attribute.IsDefined(x, typeof(RequiredMemberAttribute)));

        foreach (var requiredProperty in requiredProperties)
        {
            var propertyValue = requiredProperty.GetValue(arg);
            if (propertyValue is null) throw new Exception($"Required property '{requiredProperty.Name}' was null");
        }

        return true;
    }
}