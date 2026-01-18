// 兼容性命名空间别名
// 这个文件提供向后兼容性，新代码应该使用 BuildingBlocks.Caching.Abstractions

using BuildingBlocks.Caching.Abstractions;

namespace BuildingBlocks.Caching.Redis
{
    /// <summary>
    /// 缓存管理器接口（兼容层）
    /// </summary>
    [System.Obsolete("请使用 BuildingBlocks.Caching.Abstractions.ICacheManager 替代")]
    public interface ICacheManager : BuildingBlocks.Caching.Abstractions.ICacheManager
    {
    }
}