using BuildingBlocks.Cors;
using BuildingBlocks.SerilogLogging.Utils;
using LexiCraft.Services.Practice.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LexiCraft.Services.Practice.Shared.Extensions.WebApplicationExtensions;

public static class InfrastructureExtensions
{
    public static void UseInfrastructure(this WebApplication app)
    {
        if (Directory.Exists(app.Environment.WebRootPath))
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

        // 初始化数据库并在需要时运行种子数据
        using var scope = app.Services.CreateScope();
        var dataSeeder = scope.ServiceProvider.GetRequiredService<PracticeDbDataSeeder>();

        // 异步运行种子数据（为启动性能考虑，启动后即忽略）
        _ = Task.Run(async () =>
        {
            try
            {
                await dataSeeder.SeedAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to seed Practice database");
            }
        });
    }
}