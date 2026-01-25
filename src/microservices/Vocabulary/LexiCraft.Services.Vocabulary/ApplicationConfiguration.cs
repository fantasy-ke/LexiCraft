using BuildingBlocks.Extensions;
using BuildingBlocks.Mediator;
using LexiCraft.Services.Vocabulary.Shared.Extensions.HostApplicationBuilderExtensions;
using LexiCraft.Services.Vocabulary.Shared.Extensions.WebApplicationExtensions;
using LexiCraft.Services.Vocabulary.UserStates;
using LexiCraft.Services.Vocabulary.Words;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Vocabulary;

public static class ApplicationConfiguration
{
    public const string VocabularyModulePrefixUri = "api/v{version:apiVersion}/vocabulary";

    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.AddVocabularyStorage();
        builder.Services.AddMediator<VocabularyMetadata>();
        builder.AddInfrastructure();
        builder.AddWordsModuleServices();
        builder.AddUserStateModuleServices();
        builder.Services.WithMapster();
        builder.Services.WithIdGen();
        return builder;
    }

    public static IEndpointRouteBuilder UseApplication(this WebApplication app)
    {
        app.UseInfrastructure();

        app.MapWordsModuleEndpoints();
        app.MapUserStateModuleEndpoints();

        return app;
    }
}