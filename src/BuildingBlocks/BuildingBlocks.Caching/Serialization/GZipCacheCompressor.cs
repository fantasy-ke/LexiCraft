using System;
using System.IO;
using System.IO.Compression;

namespace BuildingBlocks.Caching.Serialization;

/// <summary>
/// GZip 缓存压缩静态帮助类
/// </summary>
public static class GZipCacheCompressor
{
    /// <summary>
    /// 压缩数据
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <returns>压缩后的数据</returns>
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
    /// 解压缩数据
    /// </summary>
    /// <param name="compressedData">压缩的数据</param>
    /// <returns>解压缩后的数据</returns>
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
