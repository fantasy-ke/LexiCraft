using System.IO.Compression;

namespace BuildingBlocks.Caching.Redis.Serialization;

/// <summary>
///     GZip 缓存压缩静态帮助类
/// </summary>
/// <remarks>
///     只对 Redis String 值使用，且不写入任何自定义头部或压缩标记。是否压缩由
///     <see cref="Options.CacheServiceOptions.EnableCompression"/> 与 1024 字节阈值共同决定，因此同一键上
///     可能同时存在压缩与未压缩的数据；读取端只能尝试解压并在失败时回退到原始字节。
///     压缩换取的是更小的网络与内存开销，代价是每次读写额外的 CPU 消耗，短值不适合开启。
/// </remarks>
internal static class GZipCacheCompressor
{
    /// <summary>
    ///     压缩数据
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <returns>压缩后的数据；输入为 <see langword="null"/> 或空数组时返回空数组。</returns>
    /// <exception cref="InvalidOperationException">压缩过程失败时抛出，内部异常保留原因。</exception>
    public static byte[] Compress(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return Array.Empty<byte>();

        try
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                gzip.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"GZip 压缩失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     解压缩数据
    /// </summary>
    /// <param name="compressedData">压缩的数据</param>
    /// <returns>解压缩后的数据；输入为 <see langword="null"/> 或空数组时返回空数组。</returns>
    /// <exception cref="InvalidOperationException">
    ///     数据不是合法 GZip 流或解压失败时抛出；调用方据此判断数据实际未被压缩。
    /// </exception>
    public static byte[] Decompress(byte[] compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return Array.Empty<byte>();

        try
        {
            using var input = new MemoryStream(compressedData);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            gzip.CopyTo(output);
            return output.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"GZip 解压缩失败: {ex.Message}", ex);
        }
    }
}