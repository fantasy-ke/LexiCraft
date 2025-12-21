using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace LexiCraft.Services.Identity.Shared.Extensions.WebApplicationExtensions;

public static class WebApplicationExtensions
{
    public static void UseInfrastructure(this WebApplication app)
    {
        app.UseStaticFiles();
        
        app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });

        
        app.UseAuthentication();
        app.UseAuthorization();

        // map registered minimal endpoints
    }
}
