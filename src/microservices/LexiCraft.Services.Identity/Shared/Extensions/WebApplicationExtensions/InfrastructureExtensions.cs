using BuildingBlocks.Cors;
using BuildingBlocks.SerilogLogging.Extensions;
using BuildingBlocks.SerilogLogging.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace LexiCraft.Services.Identity.Shared.Extensions.WebApplicationExtensions;

public static class WebApplicationExtensions
{
    public static void UseInfrastructure(this WebApplication app)
    {
        app.UseStaticFiles();
        
        app.UseDefaultCors();
        
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = SerilogRequestUtility.HttpMessageTemplate;
            options.GetLevel = SerilogRequestUtility.GetRequestLevel;
            options.EnrichDiagnosticContext = SerilogRequestUtility.EnrichFromRequest;
        });
        
        app.UseAuthentication();
        app.UseAuthorization();

    }
}
