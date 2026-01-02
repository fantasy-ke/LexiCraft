using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace LexiCraft.Services.Vocabulary.Shared.Extensions.WebApplicationExtensions;

public static class WebApplicationExtensions
{
    public static IEndpointRouteBuilder UseInfrastructure(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
