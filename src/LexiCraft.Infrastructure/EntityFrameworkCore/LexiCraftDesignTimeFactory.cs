using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LexiCraft.Infrastructure.EntityFrameworkCore;

public class LexiCraftDesignTimeFactory: IDesignTimeDbContextFactory<LexiCraftDbContext>
{
    public LexiCraftDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "LexiCraft.Host");

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

        return new LexiCraftDbContext(builder.Options);
    }

}