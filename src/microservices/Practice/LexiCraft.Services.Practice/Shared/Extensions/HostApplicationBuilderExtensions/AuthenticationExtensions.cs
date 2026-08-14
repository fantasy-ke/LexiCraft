using System.Text;
using BuildingBlocks.Authentication.Options;
using BuildingBlocks.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LexiCraft.Services.Practice.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddCustomAuthentication(this IHostApplicationBuilder builder)
    {
        var oauthOptions = builder.Configuration.BindOptions<OAuthOptions>();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Audience = oauthOptions.Audience;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = oauthOptions.ValidateIssuer,
                    ValidIssuers = oauthOptions.ValidIssuers,
                    ValidateAudience = oauthOptions.ValidateAudience,
                    ValidAudiences = oauthOptions.ValidAudiences,
                    ValidateLifetime = oauthOptions.ValidateLifetime,
                    ClockSkew = oauthOptions.ClockSkew,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = !string.IsNullOrEmpty(oauthOptions.Secret)
                        ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(oauthOptions.Secret))
                        : null
                };

                options.MapInboundClaims = false;
            });

        return builder;
    }
}