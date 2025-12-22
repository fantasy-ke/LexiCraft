namespace LexiCraft.Services.Identity.Shared.Authorize;

/// <summary>
/// OAuth提供者工厂
/// </summary>
public class OAuthProviderFactory(IEnumerable<IOAuthProvider> providers)
{
    private readonly Dictionary<string, IOAuthProvider> _providers 
        = providers.ToDictionary(p => p.ProviderName.ToLower(), p => p);

    /// <summary>
    /// 根据提供者名称获取OAuth提供者
    /// </summary>
    /// <param name="providerName">提供者名称</param>
    /// <returns>OAuth提供者实例</returns>
    public IOAuthProvider? GetProvider(string providerName)
    {
        return _providers.TryGetValue(providerName.ToLower(), out var provider) ? provider : null;
    }

    /// <summary>
    /// 获取所有支持的OAuth提供者名称
    /// </summary>
    /// <returns>提供者名称列表</returns>
    public IEnumerable<string> GetSupportedProviders()
    {
        return _providers.Keys.ToList();
    }
}