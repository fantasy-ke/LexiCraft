using System.Security.Cryptography;
using System.Text;

namespace BuildingBlocks.Authentication.Contract;

/// <summary>
///     为访问令牌和刷新令牌生成不保存明文的 SHA-256 标识。
/// </summary>
public static class AuthorizationTokenHasher
{
    /// <summary>
    ///     计算令牌的 UTF-8 SHA-256 十六进制摘要。
    /// </summary>
    /// <param name="token">待计算的非空令牌。</param>
    /// <returns>大写十六进制摘要。</returns>
    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
