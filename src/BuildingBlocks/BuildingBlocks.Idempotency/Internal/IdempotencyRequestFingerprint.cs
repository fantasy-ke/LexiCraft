using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Idempotency.Internal;

/// <summary>
///     根据请求方法、路径、查询与请求体计算稳定的 SHA-256 指纹。
/// </summary>
/// <remarks>
///     指纹用于检测同一幂等键但内容不同的冲突请求。请求体过大时返回 <see langword="null"/>。
/// </remarks>
internal static class IdempotencyRequestFingerprint
{
    /// <summary>
    ///     读取请求并计算指纹。
    /// </summary>
    /// <param name="request">当前 HTTP 请求。</param>
    /// <param name="maxRequestBodyBytes">参与指纹计算的最大请求体字节数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>十六进制指纹；请求体超限时返回 <see langword="null"/>。</returns>
    public static async Task<string?> CreateAsync(
        HttpRequest request,
        long maxRequestBodyBytes,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength > maxRequestBodyBytes)
            return null;

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendText(hash, request.Method);
        AppendText(hash, request.PathBase.Value);
        AppendText(hash, request.Path.Value);
        AppendText(hash, request.QueryString.Value);
        AppendText(hash, request.ContentType);

        request.EnableBuffering();
        var originalPosition = request.Body.CanSeek ? request.Body.Position : 0;
        var buffer = ArrayPool<byte>.Shared.Rent(81920);

        try
        {
            if (request.Body.CanSeek)
                request.Body.Position = 0;

            long totalBytes = 0;
            while (true)
            {
                var read = await request.Body
                    .ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                    break;

                totalBytes += read;
                if (totalBytes > maxRequestBodyBytes)
                    return null;

                hash.AppendData(buffer, 0, read);
            }

            return Convert.ToHexString(hash.GetHashAndReset());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            if (request.Body.CanSeek)
                request.Body.Position = originalPosition;
        }
    }

    /// <summary>
    ///     将一段文本追加进哈希，并以分隔符避免字段粘连。
    /// </summary>
    /// <param name="hash">增量哈希实例。</param>
    /// <param name="value">待追加文本；空值仅写入分隔符。</param>
    private static void AppendText(IncrementalHash hash, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            hash.AppendData(Encoding.UTF8.GetBytes(value));

        hash.AppendData([0]);
    }
}