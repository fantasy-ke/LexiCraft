using BuildingBlocks.OSS.Interface;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.OSS;

internal sealed class OSSOptionsValidator : IValidateOptions<OSSOptions>
{
    private readonly HashSet<string> _registeredProviderTypes;

    public OSSOptionsValidator(IEnumerable<IOSSProviderActivator> providerActivators)
    {
        _registeredProviderTypes = providerActivators
            .Select(activator => activator.ProviderType)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public ValidateOptionsResult Validate(string? name, OSSOptions options)
    {
        if (!options.Enable) return ValidateOptionsResult.Success;

        IReadOnlyDictionary<string, OSSProviderOptions> providers;
        try
        {
            providers = OSSOptionsResolver.ResolveProviders(options);
            _ = OSSOptionsResolver.ResolveDefaultProviderName(options, providers);
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }

        var failures = new List<string>();
        foreach (var pair in providers)
            ValidateProvider(pair.Key, pair.Value, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private void ValidateProvider(
        string providerName,
        OSSProviderOptions options,
        ICollection<string> failures)
    {
        var providerType = options.Type;
        if (string.IsNullOrWhiteSpace(providerType))
        {
            failures.Add($"OSS provider '{providerName}' must configure Type.");
            return;
        }

        if (!_registeredProviderTypes.Contains(providerType))
        {
            failures.Add(
                $"OSS provider type '{providerType}' for '{providerName}' is not registered. " +
                "Register it with services.AddOssProvider<TService>(providerType).");
            return;
        }

        if (!IsBuiltInProvider(providerType)) return;

        if (string.IsNullOrWhiteSpace(options.Endpoint))
            failures.Add($"OSS provider '{providerName}' ({providerType}) must configure Endpoint.");
        if (string.IsNullOrWhiteSpace(options.AccessKey))
            failures.Add($"OSS provider '{providerName}' ({providerType}) must configure AccessKey.");
        if (string.IsNullOrWhiteSpace(options.SecretKey))
            failures.Add($"OSS provider '{providerName}' ({providerType}) must configure SecretKey.");
        if ((IsProvider(providerType, OSSProviderNames.Minio)
             || IsProvider(providerType, OSSProviderNames.QCloud))
            && string.IsNullOrWhiteSpace(options.Region))
            failures.Add($"OSS provider '{providerName}' ({providerType}) must configure Region.");
    }

    private static bool IsBuiltInProvider(string providerType)
    {
        return IsProvider(providerType, OSSProviderNames.Minio)
               || IsProvider(providerType, OSSProviderNames.Aliyun)
               || IsProvider(providerType, OSSProviderNames.QCloud);
    }

    private static bool IsProvider(string providerType, string expected)
    {
        return string.Equals(providerType, expected, StringComparison.OrdinalIgnoreCase);
    }
}