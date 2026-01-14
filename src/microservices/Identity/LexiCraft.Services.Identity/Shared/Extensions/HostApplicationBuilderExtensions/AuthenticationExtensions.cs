using BuildingBlocks.Authentication;
using BuildingBlocks.Extensions;
using BuildingBlocks.Shared;
using LexiCraft.Services.Identity.Shared.Authorize;
using LexiCraft.Services.Identity.Shared.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using MrHuo.OAuth;
using MrHuo.OAuth.Github;
using OAuthOptions = BuildingBlocks.Authentication.Shared.OAuthOptions;

namespace LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddCustomAuthentication(this IHostApplicationBuilder builder)
    {
        var oauthOptions = builder.Configuration.BindOptions<OAuthOptions>();
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
        
        builder.AddOAuthProviders();

        return builder;
    }

    public static IHostApplicationBuilder AddOAuthProviders(this IHostApplicationBuilder builder)
    {
        var oauthCallbackOption = builder.Configuration.BindOptions<OAuthCallbackOption>();
        
        // 注册OAuth Provider
        builder.Services.AddScoped<IOAuthProvider>(sp =>
        {
            var oauth = new GithubOAuth(new OAuthConfig
            {
                AppId = oauthCallbackOption.GitHub.ClientId,
                AppKey = oauthCallbackOption.GitHub.ClientSecret,
                RedirectUri = ""
            });
            return new MrHuoOAuthProvider<DefaultAccessTokenModel, GithubUserModel>(oauth, "github", user => new OAuthUserDto
            {
                Id = user.Bio,
                Name = user.Name,
                Nickname = user.Name,
                Email = user.Email,
                AvatarUrl = user.Avatar
            });
        });
        builder.Services.AddScoped<IOAuthProvider>(sp =>
        {
            var oauth = new MrHuo.OAuth.Gitee.GiteeOAuth(new OAuthConfig
            {
                AppId = oauthCallbackOption.Gitee.ClientId,
                AppKey = oauthCallbackOption.Gitee.ClientSecret,
                RedirectUri = ""
            });
            return new MrHuoOAuthProvider<DefaultAccessTokenModel, MrHuo.OAuth.Gitee.GiteeUserModel>(oauth, "gitee", user => new OAuthUserDto
            {
                Id = user.Bio,
                Name = user.Name,
                Nickname = user.Name,
                Email = user.Email,
                AvatarUrl = user.Avatar
            });
        });
        builder.Services.AddScoped<OAuthProviderFactory>();

        return builder;
    }
}
