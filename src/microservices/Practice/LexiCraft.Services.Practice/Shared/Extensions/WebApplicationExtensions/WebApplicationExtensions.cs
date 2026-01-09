using LexiCraft.Services.Practice.Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace LexiCraft.Services.Practice.Shared.Extensions.WebApplicationExtensions;

public static class WebApplicationExtensions
{
    public static IEndpointRouteBuilder UseInfrastructure(this IEndpointRouteBuilder app)
    {
        // 初始化数据库并在需要时运行种子数据
        using var scope = app.ServiceProvider.CreateScope();
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

        return app;
    }
}