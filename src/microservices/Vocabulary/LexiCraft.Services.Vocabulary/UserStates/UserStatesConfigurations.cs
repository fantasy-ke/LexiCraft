using BuildingBlocks.Filters;
using LexiCraft.Services.Vocabulary.UserStates.Features.GetWeakWords;
using LexiCraft.Services.Vocabulary.UserStates.Features.UpdateState;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Vocabulary.UserStates;

internal static class UserStatesConfigurations
{
    public const string Tag = "UserStates";
    private const string VocabularyPrefixUri = $"{ApplicationConfiguration.VocabularyModulePrefixUri}";

    internal static WebApplicationBuilder AddUserStateModuleServices(this WebApplicationBuilder builder)
    {
        return builder;
    }

    public static IEndpointRouteBuilder MapUserStateModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var statesVersionGroup = endpoints
            .NewVersionedApi(Tag)
            .WithTags(Tag);

        var statesGroupV1 = statesVersionGroup
            .MapGroup(VocabularyPrefixUri)
            .HasApiVersion(1.0)
            .AddEndpointFilter<ResultEndPointFilter>();

        statesGroupV1.MapUpdateStateEndpoint();
        statesGroupV1.MapGetWeakWordsEndpoint();

        return endpoints;
    }
}