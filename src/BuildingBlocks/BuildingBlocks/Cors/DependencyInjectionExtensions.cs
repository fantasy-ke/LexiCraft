using BuildingBlocks.Core.Extensions;
using BuildingBlocks.Extensions;
using BuildingBlocks.Web.Cors;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Cors;

public static class Extensions
{
    private const string AllowCustomCorsPolicy = "AllowCustomPolicy";

    public static IHostApplicationBuilder AddDefaultCors(this IHostApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test"))
        {
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                });
            });
        }
        else
        {
            builder.Services.AddValidationOptions<CorsOptions>();

            builder.Services.AddCors(options =>
            {
                var corsOptions = builder.Configuration.BindOptions<CorsOptions>();
                options.AddPolicy(
                    AllowCustomCorsPolicy,
                    policy =>
                    {
                        policy.WithOrigins(corsOptions.AllowedOrigins);
                        policy.AllowAnyHeader().AllowAnyMethod();
                    }
                );
            });
        }

        return builder;
    }

    public static void UseDefaultCors(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // Use default cors policy for development environment
            // https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-9.0#cors-with-default-policy-and-middleware
            app.UseCors();
        }
        else
        {
            app.UseCors(AllowCustomCorsPolicy);
        }
    }
}
