using System.Text;

namespace BuildingBlocks.OSS.Utils;

static class Encrypt
{
    public static string MD5(string input)
    {
        using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        StringBuilder sb = new StringBuilder();
        foreach (var t in hashBytes)
        {
            sb.Append(t.ToString("X2"));
        }
        return sb.ToString();
    }
}