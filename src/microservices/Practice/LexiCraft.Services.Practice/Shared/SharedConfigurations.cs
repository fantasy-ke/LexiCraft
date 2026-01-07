using BuildingBlocks.Filters;
using LexiCraft.Services.Practice.Shared.Features.GetPerformanceMetrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Practice.Shared;

public static class SharedConfigurations
{
    public const string Tag = "Shared";
    private const string SharedPrefixUri = $"{ApplicationConfiguration.PracticeModulePrefixUri}";

    public static IEndpointRouteBuilder MapSharedModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var sharedVersionGroup = endpoints
            .NewVersionedApi(Tag);

        var sharedGroupV1 = sharedVersionGroup
            .MapGroup(SharedPrefixUri)
            .HasApiVersion(1.0)
            .AddEndpointFilter<ResultEndPointFilter>();

        sharedGroupV1.MapGetPerformanceMetricsEndpoint();

        return endpoints;
    }
}