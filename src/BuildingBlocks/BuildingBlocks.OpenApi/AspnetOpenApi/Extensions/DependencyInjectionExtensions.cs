using Asp.Versioning;
using BuildingBlocks.Extensions;
using BuildingBlocks.OpenApi.AspnetOpenApi.Transformers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;

public static class DependencyInjectionExtensions
{
    public static IHostApplicationBuilder AddAspnetOpenApi(this IHostApplicationBuilder builder)
    {
        builder.Services.AddConfigurationOptions<OpenApiOptions>();

        builder
            .Services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;

                options.ApiVersionReader = ApiVersionReader.Combine(
                    new HeaderApiVersionReader("api-version"),
                    new QueryStringApiVersionReader(),
                    new UrlSegmentApiVersionReader()
                );
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);

                options
                    .Policies.Sunset(0.9)
                    .Effective(DateTimeOffset.Now.AddDays(60))
                    .Link("policy.html")
                    .Title("Versioning Policy")
                    .Type("text/html");
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            })
            .AddOpenApi(options =>
            {
                options.Document.AddDocumentTransformer<OpenApiVersioningDocumentTransformer>();
                options.Document.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                options.Document.AddSchemaTransformer<EnumSchemaTransformer>();
            });

        return builder;
    }
}