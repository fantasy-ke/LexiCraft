using BuildingBlocks.Extensions;
using BuildingBlocks.MongoDB.Extensions;
using LexiCraft.Services.Practice.Shared.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexiCraft.Services.Practice.Shared.Extensions.HostApplicationBuilderExtensions;

public static partial class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddPracticeStorage(this IHostApplicationBuilder builder)
    {
        builder.AddResilience();
        builder.AddMongoDbContext<PracticeDbContext>();
        builder.Services.AddScoped<PracticeDbDataSeeder>();
        return builder;
    }
}
