using BuildingBlocks.Authentication;
using BuildingBlocks.Extensions;
using BuildingBlocks.Shared;
using LexiCraft.Services.Identity.Shared.Authorize;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ServiceExtensions = BuildingBlocks.Authentication.ServiceExtensions;

namespace LexiCraft.Services.Identity.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddCustomAuthentication(this IHostApplicationBuilder builder)
    {
        var oauthOptions = builder.Configuration.BindOptions<JwtOptions>();
        builder
            .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        builder.Services.AddConfigurationOptions<OAuthOption>();
        builder.Services.AddScoped<IPermissionCheck, DatabasePermissionCheck>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IOAuthProvider, GitHubOAuthProvider>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IOAuthProvider, GiteeOAuthProvider>());
        builder.Services.AddScoped<OAuthProviderFactory>();

        builder.Services.AddOpenApi(options =>
        {
            //可输入token
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        return builder;
    }

    public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
        : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
            if (authenticationSchemes.All(authScheme => authScheme.Name != JwtBearerDefaults.AuthenticationScheme))
            {
                return;
            }

            // 定义全局 Bearer 安全方案（注意接口类型）
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme.ToLowerInvariant(),
                In = ParameterLocation.Header,
                BearerFormat = "Json Web Token"
            };

            // 构造 SecurityRequirement，键为 OpenApiSecuritySchemeReference（新版本签名）
            var securitySchemeReference =
                new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)
                {
                    Reference = new JsonSchemaReference()
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    },
                };

            var securityRequirement = new OpenApiSecurityRequirement
            {
                [securitySchemeReference] = []
            };

            // 给所有操作附加安全要求
            foreach (var path in document.Paths.Values)
            {
                if (path.Operations is null || path.Operations.Count == 0)
                {
                    continue;
                }

                foreach (var operation in path.Operations.Values)
                {
                    operation.Security ??= new List<OpenApiSecurityRequirement>();
                    operation.Security.Add(securityRequirement);
                }
            }
        }
    }
}
