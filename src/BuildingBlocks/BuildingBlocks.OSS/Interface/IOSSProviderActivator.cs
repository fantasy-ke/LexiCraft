using BuildingBlocks.OSS;

namespace BuildingBlocks.OSS.Interface;

/// <summary>
///     根据单个提供商配置创建对象存储服务。
/// </summary>
public interface IOSSProviderActivator
{
    string ProviderType { get; }

    IOSSService Create(IServiceProvider serviceProvider, OSSOptions options);
}
