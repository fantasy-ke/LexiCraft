using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Authentication.Contract;
using BuildingBlocks.Shared;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Authentication;

public class JwtTokenProvider(IOptions<JwtOptions> options):IJwtTokenProvider
{
    readonly JwtOptions _options = options.Value;
    
    public string GenerateAccessToken(Dictionary<string, string> dist, Guid userId, string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, string.Join(',', roles)),
            new(ClaimTypes.Sid, userId.ToString())
        };

        foreach (var item in dist)
        {
            claims.Add(new Claim(item.Key, item.Value));
        }

        // 2. 从 appsettings.json 中读取SecretKey
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));

        // 3. 选择加密算法
        var algorithm = SecurityAlgorithms.HmacSha256;

        // 4. 生成Credentials
        var signingCredentials = new SigningCredentials(secretKey, algorithm);

        // 5. 根据以上，生成token
        var jwtSecurityToken = new JwtSecurityToken(
            _options.Issuer, //Issuer
            _options.Audience, //Audience
            claims, //Claims,
            DateTime.Now, //notBefore
            DateTime.Now.AddDays(_options.ExpireMinute), //expires
            signingCredentials //Credentials
        );

        // 6. 将token变为string
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