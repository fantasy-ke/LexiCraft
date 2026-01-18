namespace BuildingBlocks.Caching.Serialization;

/// <summary>
/// 缓存压缩器接口
/// </summary>
public interface ICacheCompressor
{
    /// <summary>
    /// 压缩数据
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <returns>压缩后的数据</returns>
    byte[] Compress(byte[] data);

    /// <summary>
    /// 解压缩数据
    /// </summary>
    /// <param name="compressedData">压缩的数据</param>
    /// <returns>解压缩后的数据</returns>
    byte[] Decompress(byte[] compressedData);

    /// <summary>
    /// 压缩器名称
    /// </summary>
    string Name { get; }
}