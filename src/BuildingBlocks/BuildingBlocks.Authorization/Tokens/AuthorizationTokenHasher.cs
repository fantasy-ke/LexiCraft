using System.Security.Cryptography;
using System.Text;

namespace BuildingBlocks.Authentication.Tokens;

/// <summary>
///     为访问令牌和刷新令牌生成不保存明文的 SHA-256 标识。
/// </summary>
public static class AuthorizationTokenHasher
{
    /// <summary>
    ///     计算令牌的 UTF-8 SHA-256 十六进制摘要。
    /// </summary>
    /// <param name="token">待计算的非空高熵令牌。</param>
    /// <returns>大写十六进制摘要。</returns>
    /// <remarks>摘要适合令牌等值索引，不适合保存用户密码；调用方不得记录输入的明文令牌。</remarks>
    /// <exception cref="ArgumentException"><paramref name="token"/> 为空或仅包含空白字符时抛出。</exception>
    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
