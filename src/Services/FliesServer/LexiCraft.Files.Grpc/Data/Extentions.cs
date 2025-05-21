using Microsoft.EntityFrameworkCore;

namespace LexiCraft.Files.Grpc.Data;

public static class Extentions
{
    public static IApplicationBuilder UseMigration(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
        dbContext.Database.MigrateAsync();

        return app;
    }
}