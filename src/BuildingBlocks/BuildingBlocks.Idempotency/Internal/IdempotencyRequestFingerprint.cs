using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Idempotency.Internal;

internal static class IdempotencyRequestFingerprint
{
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

    private static void AppendText(IncrementalHash hash, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            hash.AppendData(Encoding.UTF8.GetBytes(value));

        hash.AppendData([0]);
    }
}