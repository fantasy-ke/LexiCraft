using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LexiCraft.AuthServer.Infrastructure.EntityFrameworkCore;

public class LexiCraftDesignTimeFactory: IDesignTimeDbContextFactory<LexiCraftDbContext>
{
    public LexiCraftDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "LexiCraft.AuthServer.Api");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            // 从 appsettings.json 文件加载配置
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            // 也可以添加其他配置源，例如环境变量
            .AddEnvironmentVariables()
            // 或者命令行参数
            .Build();

        var builder= new DbContextOptionsBuilder<LexiCraftDbContext>();
        builder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        // Npgsql 6.0.0 之后的版本需要设置以下两个开关，否则会导致时间戳字段的值不正确
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

        return new LexiCraftDbContext(builder.Options);
    }

}