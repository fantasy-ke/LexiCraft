using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace BuildingBlocks.OpenApi.AspnetOpenApi.Transformers;

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-9.0#customize-openapi-documents-with-transformers
public class OpenApiVersioningDocumentTransformer(
    IApiVersionDescriptionProvider apiVersionDescriptionProvider,
    IOptionsMonitor<OpenApiOptions> options
) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var openApiOptions = options.CurrentValue;

        var apiDescription = apiVersionDescriptionProvider.ApiVersionDescriptions.SingleOrDefault(description =>
            description.GroupName == context.DocumentName
        );

        if (apiDescription is null) return Task.CompletedTask;

        document.Info.License = new OpenApiLicense
        {
            Name = openApiOptions?.LicenseName,
            Url = openApiOptions?.LicenseUrl
        };

        document.Info.Contact = new OpenApiContact
        {
            Name = openApiOptions?.AuthorName,
            Url = openApiOptions?.AuthorUrl,
            Email = openApiOptions?.AuthorEmail
        };

        document.Info.Version = apiDescription.ApiVersion.ToString();

        document.Info.Title = openApiOptions?.Title;

        document.Info.Description = openApiOptions?.Description;

        return Task.CompletedTask;
    }
}