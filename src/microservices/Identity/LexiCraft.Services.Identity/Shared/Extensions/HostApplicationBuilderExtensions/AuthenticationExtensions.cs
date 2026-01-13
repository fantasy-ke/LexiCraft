using BuildingBlocks.Authentication;
using BuildingBlocks.Extensions;
using BuildingBlocks.Shared;
using LexiCraft.Services.Identity.Shared.Authorize;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using OAuthOptions = BuildingBlocks.Authentication.Shared.OAuthOptions;

namespace LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddCustomAuthentication(this IHostApplicationBuilder builder)
    {
        var oauthOptions = builder.Configuration.BindOptions<OAuthOptions>();
        // builder.Services.AddAuthorization()
        //     .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //     .AddJwtBearer(options =>
        //     {
        //         options.TokenValidationParameters = new TokenValidationParameters
        //         {
        //             ValidateIssuer = oauthOptions.ValidateIssuer,
        //             ValidateAudience = oauthOptions.ValidateAudience,
        //             ValidateLifetime = oauthOptions.ValidateLifetime,
        //             ValidateIssuerSigningKey = true,
        //             ValidIssuer = oauthOptions.Issuer,
        //             ValidAudience = oauthOptions.Audience,
        //             IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(oauthOptions.Secret))
        //         };
        //     });
        builder
            .Services.AddAuthorization()
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // 对于自签名的对称加密 Token，通常不设置 Authority，除非有 OIDC 元数据服务
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(oauthOptions.Secret!))
                };
        
                options.MapInboundClaims = false;
            });
        builder.Services.AddConfigurationOptions<OAuthOption>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IOAuthProvider, GitHubOAuthProvider>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IOAuthProvider, GiteeOAuthProvider>());
        builder.Services.AddScoped<OAuthProviderFactory>();

        return builder;
    }
}
