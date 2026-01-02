using LexiCraft.Services.Vocabulary.Words.Features.GetWordLists;
using LexiCraft.Services.Vocabulary.Words.Features.GetWordsByList;
using LexiCraft.Services.Vocabulary.Words.Features.ImportWords;
using LexiCraft.Services.Vocabulary.Words.Features.SearchWord;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Vocabulary.Words;

internal static class WordsConfigurations
{
    public const string Tag = "Words";
    private const string VocabularyPrefixUri = $"{ApplicationConfiguration.VocabularyModulePrefixUri}";

    internal static WebApplicationBuilder AddWordsModuleServices(this WebApplicationBuilder builder)
    {
        return builder;
    }

    public static IEndpointRouteBuilder MapWordsModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var wordsVersionGroup = endpoints
            .NewVersionedApi(Tag)
            .WithTags(Tag);

        var wordsGroupV1 = wordsVersionGroup
            .MapGroup(VocabularyPrefixUri)
            .HasApiVersion(1.0);

        wordsGroupV1.MapSearchWordEndpoint();
        wordsGroupV1.MapGetWordListsEndpoint();
        wordsGroupV1.MapGetWordsByListEndpoint();
        wordsGroupV1.MapImportWordsEndpoint();

        return endpoints;
    }
}
