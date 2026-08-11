using BuildingBlocks.OSS.Interface;

namespace BuildingBlocks.OSS;

internal sealed class OSSProviderRegistration(
    string providerType,
    Func<IServiceProvider, OSSProviderOptions, IOSSService> factory) : IOSSProviderActivator
{
    public string ProviderType { get; } = providerType;

    public IOSSService Create(IServiceProvider serviceProvider, OSSProviderOptions options)
    {
        return factory(serviceProvider, options);
    }
}
