// ??????

using BuildingBlocks.Filters;
using LexiCraft.Services.Practice.Assessments.Features.SubmitAnswer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Practice.Assessments;

/// <summary>
///     ???????
/// </summary>
public static class AssessmentsConfigurations
{
    public const string Tag = "Assessments";
    private const string PracticePrefixUri = $"{ApplicationConfiguration.PracticeModulePrefixUri}";

    /// <summary>
    ///     ????????
    /// </summary>
    /// <param name="app">Web??????</param>
    /// <returns>???????????</returns>
    public static IEndpointRouteBuilder MapAssessmentsModuleEndpoints(this WebApplication app)
    {
        var assessmentsVersionGroup = app
            .NewVersionedApi(Tag)
            .WithTags(Tag);

        var assessmentsGroupV1 = assessmentsVersionGroup
            .MapGroup(PracticePrefixUri)
            .HasApiVersion(1.0)
            .AddEndpointFilter<ResultEndPointFilter>();

        assessmentsGroupV1.MapSubmitAnswerEndpoint();

        return app;
    }
}
