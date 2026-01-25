using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Authentication.Shared;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Authentication;

public class JwtTokenProvider(IOptionsMonitor<OAuthOptions> options) : IJwtTokenProvider
{
    public string GenerateAccessToken(Dictionary<string, string> dist, Guid userId, string[] roles)
    {
        var currentOptions = options.CurrentValue;

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, string.Join(',', roles)),
            new(ClaimTypes.Sid, userId.ToString())
        };

        foreach (var item in dist) claims.Add(new Claim(item.Key, item.Value));

        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(currentOptions.Secret ?? string.Empty));

        var algorithm = SecurityAlgorithms.HmacSha256;

        var signingCredentials = new SigningCredentials(secretKey, algorithm);

        var jwtSecurityToken = new JwtSecurityToken(
            currentOptions.Issuer,
            currentOptions.Audience,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(currentOptions.ExpireMinute),
            signingCredentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

        return token;
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}