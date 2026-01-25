using System.Text;
using BuildingBlocks.Extensions;
using BuildingBlocks.Shared;
using LexiCraft.Services.Identity.Shared.Authorize;
using LexiCraft.Services.Identity.Shared.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MrHuo.OAuth;
using MrHuo.OAuth.Gitee;
using MrHuo.OAuth.Github;
using OAuthOptions = BuildingBlocks.Authentication.Shared.OAuthOptions;

namespace LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddCustomAuthentication(this IHostApplicationBuilder builder)
    {
        var oauthOptions = builder.Configuration.BindOptions<OAuthOptions>();
        var requireHttpsMetadata = oauthOptions.RequireHttpsMetadata ?? !builder.Environment.IsDevelopment();

        builder
            .Services.AddAuthorization()
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = oauthOptions.Authority;
                options.Audience = oauthOptions.Audience;
                options.RequireHttpsMetadata = requireHttpsMetadata;

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
        builder.Services.AddScoped<IOAuthProvider>(_ =>
        {
            var oauth = new GithubOAuth(new OAuthConfig
            {
                AppId = oauthCallbackOption.GitHub.ClientId,
                AppKey = oauthCallbackOption.GitHub.ClientSecret,
                Scope = oauthCallbackOption.GitHub.Scope
            });
            return new OAuthProvider<DefaultAccessTokenModel, GithubUserModel>(oauth, "github", user => new OAuthUserDto
            {
                Id = user.Name,
                Name = user.Name,
                Nickname = user.Name,
                Email = user.Email,
                AvatarUrl = user.Avatar
            });
        });
        builder.Services.AddScoped<IOAuthProvider>(_ =>
        {
            var oauth = new GiteeOAuth(new OAuthConfig
            {
                AppId = oauthCallbackOption.Gitee.ClientId,
                AppKey = oauthCallbackOption.Gitee.ClientSecret,
                Scope = oauthCallbackOption.Gitee.Scope
            });
            return new OAuthProvider<DefaultAccessTokenModel, GiteeUserModel>(oauth, "gitee", user => new OAuthUserDto
            {
                Id = user.Name,
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