using Asp.Versioning;
using BuildingBlocks.Extensions;
using BuildingBlocks.OpenApi.AspnetOpenApi.Transformers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;

public static class DependencyInjectionExtensions
{
    public static IHostApplicationBuilder AddAspnetOpenApi(this IHostApplicationBuilder builder, string[] versions)
    {
        builder.Services.AddConfigurationOptions<OpenApiOptions>();

        foreach (var documentName in versions)
            builder.Services.AddOpenApi(
                documentName,
                options =>
                {
                    options.AddDocumentTransformer<OpenApiVersioningDocumentTransformer>();
                    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                    // options.AddOperationTransformer<OperationDeprecatedStatusTransformers>();
                    // options.AddDocumentTransformer<SecuritySchemeDocumentTransformer>();
                    // options.AddOperationTransformer<OpenApiDefaultValuesOperationTransformer>();
                    // options.AddSchemaTransformer<SchemaNullableFalseTransformers>();
                    options.AddSchemaTransformer<EnumSchemaTransformer>();
                }
            );

        return builder;
    }

    public static IHostApplicationBuilder AddCustomVersioning(this IHostApplicationBuilder builder)
    {
        // 复制官方示例的https://github.com/dotnet/aspnet-api-versioning/blob/main/examples/AspNetCore/WebApi/MinimalOpenApiExample/Program.cs
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
            .EnableApiVersionBinding();

        return builder;
    }
}