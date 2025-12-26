using BuildingBlocks.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace BuildingBlocks.OpenApi.AspnetOpenApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseAspnetOpenApi(this WebApplication app)
    {
        app.MapOpenApi();

        if (OpenApiOptions.IsOpenApiBuild || app.Environment.IsBuild())
            Environment.Exit(0);

        if (!app.Environment.IsDevelopment())
            return app;

        var descriptions = app.DescribeApiVersions();


        app.MapScalarApiReference(scalarOptions =>
        {
            scalarOptions.AddDocuments(descriptions.Select(b =>
                new ScalarDocument(b.GroupName, $"Document{b.ApiVersion}", $"/openapi/{b.GroupName}.json")));
            scalarOptions.Theme = ScalarTheme.BluePlanet;
            scalarOptions.DefaultFonts = false;
            scalarOptions.ShowDeveloperTools = DeveloperToolsVisibility.Always;
            scalarOptions.Authentication = new ScalarAuthenticationOptions()
            {
                PreferredSecuritySchemes = new List<string>() { "Bearer" },
            };
        });

        return app;
    }
}
