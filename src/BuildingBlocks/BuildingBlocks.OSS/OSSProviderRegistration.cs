using BuildingBlocks.OSS.Interface;

namespace BuildingBlocks.OSS;

internal sealed class OSSProviderRegistration(
    string providerType,
    Func<IServiceProvider, OSSOptions, IOSSService> factory) : IOSSProviderActivator
{
    public string ProviderType { get; } = providerType;

    public IOSSService Create(IServiceProvider serviceProvider, OSSOptions options)
    {
        return factory(serviceProvider, options);
    }
}
