namespace BuildingBlocks.EntityFrameworkCore.Postgres.Configuration;

/// <summary>定义 PostgreSQL EF Core 提供程序的连接与迁移配置。</summary>
public class PostgresOptions
{
    /// <summary>获取或设置 PostgreSQL 连接字符串。</summary>
    /// <remarks>仅在指定的 <c>ConnectionStrings</c> 项为空时作为回退值；不应把真实凭据提交到源码仓库。</remarks>
    public string? ConnectionString { get; set; }

    /// <summary>获取或设置包含 EF Core 迁移的程序集名称。</summary>
    /// <remarks>未设置时使用数据库上下文所在程序集。</remarks>
    public string? MigrationAssembly { get; set; }
}
