using BuildingBlocks.Authentication;
using BuildingBlocks.Extensions;
using BuildingBlocks.Shared;
using LexiCraft.Services.Vocabulary.Shared.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LexiCraft.Services.Vocabulary.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddCustomAuthentication(this IHostApplicationBuilder builder)
    {
        var oauthOptions = builder.Configuration.BindOptions<JwtOptions>();
        
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
                };

                options.MapInboundClaims = false;
            });

        // 默认使用基于 JWT Claims 或权限定义管理的基础检查。
        // 如果需要跨服务从数据库/缓存读取，后续可实现自定义 IPermissionCheck。
        // builder.Services.AddScoped<IPermissionCheck, PermissionCheck>();
        
        return builder;
    }
}
