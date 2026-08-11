using BuildingBlocks.Extensions;
using BuildingBlocks.OSS.Interface;
using BuildingBlocks.OSS.Interface.Service;
using BuildingBlocks.OSS.Providers;
using BuildingBlocks.OSS.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.OSS;

public static class OssServiceExtensions
{
    /// <summary>
    ///     添加支持命名多提供商配置的对象存储服务。
    /// </summary>
    public static IHostApplicationBuilder AddOssService(
        this IHostApplicationBuilder builder,
        Action<OSSOptions>? configure = null)
    {
        var configuredOptions = builder.Configuration.BindOptions(nameof(OSSOptions), configure);

        var optionsBuilder = builder.Services
            .AddOptions<OSSOptions>()
            .BindConfiguration(nameof(OSSOptions));
        if (configure != null) optionsBuilder.Configure(configure);
        optionsBuilder.ValidateOnStart();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<OSSOptions>, OSSOptionsValidator>());

        RegisterBaseServices(builder.Services);
        RegisterBuiltInProviders(builder.Services);
        RegisterProviderSpecificAliases(builder.Services, configuredOptions);

        builder.Services.TryAddSingleton<IOSSService>(serviceProvider =>
            serviceProvider.GetRequiredService<IOSSServiceFactory>().Create());

        return builder;
    }

    /// <summary>
    ///     注册自定义对象存储提供商。提供商实现应提供包含 <see cref="OSSProviderOptions" /> 参数的公开构造函数。
    /// </summary>
    public static IServiceCollection AddOssProvider<TService>(
        this IServiceCollection services,
        string providerType)
        where TService : class, IOSSService
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerType);
        RegisterProvider<TService>(services, providerType.Trim(), true);
        return services;
    }

    private static void RegisterBaseServices(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.TryAddSingleton<ICacheProvider, MemoryCacheProvider>();
        services.TryAddSingleton<IOSSServiceFactory, OssServiceFactory>();
    }

    private static void RegisterBuiltInProviders(IServiceCollection services)
    {
        RegisterProvider<MinioOssService>(services, OSSProviderNames.Minio, false);
        RegisterProvider<AliyunOssService>(services, OSSProviderNames.Aliyun, false);
        RegisterProvider<QCloudOssService>(services, OSSProviderNames.QCloud, false);
    }

    private static void RegisterProvider<TService>(
        IServiceCollection services,
        string providerType,
        bool replaceExisting)
        where TService : class, IOSSService
    {
        var existingDescriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IOSSProviderActivator)
                                 && descriptor.ImplementationInstance is OSSProviderRegistration registration
                                 && string.Equals(
                                     registration.ProviderType,
                                     providerType,
                                     StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (existingDescriptors.Count > 0 && !replaceExisting) return;
        foreach (var descriptor in existingDescriptors) services.Remove(descriptor);

        services.AddSingleton<IOSSProviderActivator>(
            new OSSProviderRegistration(
                providerType,
                (serviceProvider, options) =>
                    ActivatorUtilities.CreateInstance<TService>(serviceProvider, options)));
    }

    private static void RegisterProviderSpecificAliases(
        IServiceCollection services,
        OSSOptions options)
    {
        if (!options.Enable) return;

        var providers = OSSOptionsResolver.ResolveProviders(options);
        var defaultProviderName = OSSOptionsResolver.ResolveDefaultProviderName(options, providers);

        RegisterProviderAlias<IMinioOssService>(
            services,
            providers,
            defaultProviderName,
            OSSProviderNames.Minio);
        RegisterProviderAlias<IAliyunOssService>(
            services,
            providers,
            defaultProviderName,
            OSSProviderNames.Aliyun);
        RegisterProviderAlias<IQCloudOSSService>(
            services,
            providers,
            defaultProviderName,
            OSSProviderNames.QCloud);
    }

    private static void RegisterProviderAlias<TService>(
        IServiceCollection services,
        IReadOnlyDictionary<string, OSSProviderOptions> providers,
        string? defaultProviderName,
        string providerType)
        where TService : class, IOSSService
    {
        var matchedNames = providers
            .Where(pair => string.Equals(
                pair.Value.Type,
                providerType,
                StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Key)
            .ToList();

        var providerName = matchedNames.Count switch
        {
            0 => null,
            1 => matchedNames[0],
            _ when defaultProviderName != null && matchedNames.Contains(
                defaultProviderName,
                StringComparer.OrdinalIgnoreCase) => defaultProviderName,
            _ => null
        };

        if (providerName == null) return;

        services.TryAddSingleton<TService>(serviceProvider =>
            (TService)serviceProvider.GetRequiredService<IOSSServiceFactory>().Create(providerName));
    }
}
