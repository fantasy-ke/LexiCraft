using BuildingBlocks.Authentication.Shared;
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
                options.Authority = oauthOptions.Authority;
                options.Audience = oauthOptions.Audience;
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

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
                        ? new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(oauthOptions.Secret)) 
                        : null
                };

                options.MapInboundClaims = false;
            });
        
        return builder;
    }
}
