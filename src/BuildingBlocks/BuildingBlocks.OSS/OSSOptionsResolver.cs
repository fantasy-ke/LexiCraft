using BuildingBlocks.OSS.Models;

namespace BuildingBlocks.OSS;

internal static class OSSOptionsResolver
{
    public static IReadOnlyDictionary<string, OSSProviderOptions> ResolveProviders(OSSOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var providers = new Dictionary<string, OSSProviderOptions>(StringComparer.OrdinalIgnoreCase);
        if (!options.Enable) return providers;

        if (options.Providers.Count == 0)
        {
            var providerName = NormalizeProviderName(options.DefaultProvider);
            providers.Add(providerName, Normalize(options));
            return providers;
        }

        foreach (var pair in options.Providers)
        {
            var providerName = NormalizeProviderName(pair.Key);
            if (pair.Value == null)
                throw new InvalidOperationException($"OSS provider '{providerName}' configuration cannot be null.");
            if (!providers.TryAdd(providerName, Normalize(pair.Value)))
                throw new InvalidOperationException($"Duplicate OSS provider name '{providerName}'.");
        }

        return providers;
    }

    public static string? ResolveDefaultProviderName(
        OSSOptions options,
        IReadOnlyDictionary<string, OSSProviderOptions> providers)
    {
        if (!options.Enable || providers.Count == 0) return null;

        var configuredName = NormalizeProviderName(options.DefaultProvider);
        var matchedName = providers.Keys.FirstOrDefault(name =>
            string.Equals(name, configuredName, StringComparison.OrdinalIgnoreCase));
        if (matchedName != null) return matchedName;

        if (providers.Count == 1 && configuredName == DefaultOptionName.Name)
            return providers.Keys.Single();

        throw new InvalidOperationException(
            $"Default OSS provider '{configuredName}' was not found. Configured providers: {string.Join(", ", providers.Keys)}.");
    }

    public static OSSOptions ToServiceOptions(OSSProviderOptions options)
    {
        return new OSSOptions
        {
            Enable = true,
            Type = options.Type,
            Provider = options.Provider,
            DefaultBucket = options.DefaultBucket,
            Endpoint = options.Endpoint,
            AccessKey = options.AccessKey,
            SecretKey = options.SecretKey,
            Region = options.Region,
            IsEnableHttps = options.IsEnableHttps,
            IsEnableCache = options.IsEnableCache
        };
    }


    private static OSSProviderOptions Normalize(OSSProviderOptions options)
    {
        return new OSSProviderOptions
        {
            Type = options.Type?.Trim() ?? string.Empty,
            Provider = options.Provider,
            DefaultBucket = string.IsNullOrWhiteSpace(options.DefaultBucket)
                ? DefaultOptionName.Name
                : options.DefaultBucket.Trim(),
            Endpoint = options.Endpoint?.Trim() ?? string.Empty,
            AccessKey = options.AccessKey ?? string.Empty,
            SecretKey = options.SecretKey ?? string.Empty,
            Region = options.Region,
            IsEnableHttps = options.IsEnableHttps,
            IsEnableCache = options.IsEnableCache
        };
    }

    private static string NormalizeProviderName(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName)) return DefaultOptionName.Name;
        return providerName.Trim();
    }
}
