using BuildingBlocks.Exceptions.Handler;
using Microsoft.AspNetCore.Builder;

namespace LexiCraft.Services.Practice.Shared.Extensions.WebApplicationExtensions;

public static partial class WebApplicationExtensions
{
    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        app.UseExceptionHandler();
        
        app.UseCors();
        
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}