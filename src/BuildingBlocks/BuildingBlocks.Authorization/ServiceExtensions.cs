using System.Text;
using BuildingBlocks.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace BuildingBlocks.Authentication;

public static class ServiceExtensions
{
    /// <summary>
    /// 添加Jwt访问
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection WithJwt(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        var option = configuration.GetSection(JwtOptions.Name);

        var jwtOption = option.Get<JwtOptions>();

        services.Configure<JwtOptions>(option);

        services.AddAuthorization()
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOption!.Issuer,
                    ValidAudience = jwtOption.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOption.Secret))
                };
            });
        
        services.AddOpenApi(options =>
        {
            //可输入token
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        return services;
    }
    
    public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
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
            var securitySchemeReference = new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)
            {
                Reference = new JsonSchemaReference()
                {
                    Id =  JwtBearerDefaults.AuthenticationScheme,
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