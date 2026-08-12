using System.Security.Cryptography;
using System.Text;

namespace BuildingBlocks.Authentication.Contract;

public static class AuthorizationTokenHasher
{
    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
