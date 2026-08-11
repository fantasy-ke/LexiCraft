using System.Collections.Concurrent;
using System.Threading;
using BuildingBlocks.OSS.Interface;
using BuildingBlocks.OSS.Services;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.OSS;

public sealed class OssServiceFactory : IOSSServiceFactory
{
    private readonly ConcurrentDictionary<string, Lazy<IOSSService>> _services =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly string? _defaultProviderName;
    private readonly DisabledOSSService _disabledService = new();
    private readonly IReadOnlyDictionary<string, OSSProviderOptions> _providerOptions;
    private readonly IReadOnlyDictionary<string, IOSSProviderActivator> _providerRegistrations;
    private readonly IServiceProvider _serviceProvider;

    public OssServiceFactory(
        IOptions<OSSOptions> options,
        IEnumerable<IOSSProviderActivator> providerRegistrations,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(providerRegistrations);

        Options = options.Value;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _providerOptions = OSSOptionsResolver.ResolveProviders(Options);
        _defaultProviderName = OSSOptionsResolver.ResolveDefaultProviderName(Options, _providerOptions);
        _providerRegistrations = providerRegistrations.ToDictionary(
            registration => registration.ProviderType,
            StringComparer.OrdinalIgnoreCase);
    }

    private OSSOptions Options { get; }

    public bool IsEnabled => Options.Enable;

    public string? DefaultProviderName => _defaultProviderName;

    public string? DefaultBucket =>
        _defaultProviderName == null
            ? null
            : _providerOptions[_defaultProviderName].DefaultBucket;

    public IReadOnlyCollection<string> ProviderNames => _providerOptions.Keys.ToArray();

    public IOSSService Create()
    {
        return !IsEnabled
            ? _disabledService
            : Create(_defaultProviderName!);
    }

    public IOSSService Create(string providerName)
    {
        if (!IsEnabled) return _disabledService;
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("OSS provider name cannot be empty.", nameof(providerName));

        var matchedName = _providerOptions.Keys.FirstOrDefault(name =>
            string.Equals(name, providerName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (matchedName == null)
            throw new KeyNotFoundException(
                $"OSS provider '{providerName}' was not found. Configured providers: {string.Join(", ", ProviderNames)}.");

        return _services.GetOrAdd(
                matchedName,
                name => new Lazy<IOSSService>(
                    () => CreateProvider(name, _providerOptions[name]),
                    LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;
    }

    public string GetDefaultBucket(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("OSS provider name cannot be empty.", nameof(providerName));

        var matchedName = _providerOptions.Keys.FirstOrDefault(name =>
            string.Equals(name, providerName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (matchedName == null)
            throw new KeyNotFoundException(
                $"OSS provider '{providerName}' was not found. Configured providers: {string.Join(", ", ProviderNames)}.");

        return _providerOptions[matchedName].DefaultBucket;
    }

    private IOSSService CreateProvider(string providerName, OSSProviderOptions options)
    {
        var providerType = options.GetProviderType();
        if (!_providerRegistrations.TryGetValue(providerType, out var registration))
            throw new InvalidOperationException(
                $"OSS provider type '{providerType}' for '{providerName}' is not registered. " +
                "Register it with services.AddOssProvider<TService>(providerType).");

        var serviceOptions = OSSOptionsResolver.ToServiceOptions(options);
        return registration.Create(_serviceProvider, serviceOptions);
    }
}
