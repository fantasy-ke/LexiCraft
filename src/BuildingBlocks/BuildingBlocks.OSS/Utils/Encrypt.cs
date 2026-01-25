using System.Text;

namespace BuildingBlocks.OSS.Utils;

internal static class Encrypt
{
    public static string MD5(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);

        var sb = new StringBuilder();
        foreach (var t in hashBytes) sb.Append(t.ToString("X2"));
        return sb.ToString();
    }
}