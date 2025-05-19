using System.Text;
using BuildingBlocks.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

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
            if (authenticationSchemes.Any(authScheme => authScheme.Name == JwtBearerDefaults.AuthenticationScheme))
            {
                var requirements = new Dictionary<string, OpenApiSecurityScheme>
                {
                    [JwtBearerDefaults.AuthenticationScheme] = new()
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = JwtBearerDefaults.AuthenticationScheme.ToLower(),
                        In = ParameterLocation.Header,
                        BearerFormat = "Json Web Token"
                    }
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = requirements;
                foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
                {
                    operation.Value.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = JwtBearerDefaults.AuthenticationScheme,
                                Type = ReferenceType.SecurityScheme
                            }
                        }] = Array.Empty<string>()
                    });
                }
            }
        }
    }

}