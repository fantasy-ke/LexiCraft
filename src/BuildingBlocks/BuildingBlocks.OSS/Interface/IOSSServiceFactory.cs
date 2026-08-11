namespace BuildingBlocks.OSS.Interface;

public interface IOSSServiceFactory
{
    bool IsEnabled { get; }

    string? DefaultProviderName { get; }

    string? DefaultBucket { get; }

    IReadOnlyCollection<string> ProviderNames { get; }

    IOSSService Create();

    IOSSService Create(string providerName);

    string GetDefaultBucket(string providerName);
}