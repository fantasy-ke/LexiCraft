using LexiCraft.Services.Practice.Tasks.Models;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.Shared.Data;

/// <summary>
///     Creates the indexes used by practice task queries.
/// </summary>
public class PracticeDbDataSeeder(PracticeDbContext context)
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<PracticeTask>(
                Builders<PracticeTask>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Ascending(x => x.Status)
                    .Descending(x => x.FinishedAt),
                new CreateIndexOptions { Name = "ix_practice_tasks_user_status_finished_at" }),
            new CreateIndexModel<PracticeTask>(
                Builders<PracticeTask>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Ascending(x => x.TaskType),
                new CreateIndexOptions { Name = "ix_practice_tasks_user_task_type" }),
            new CreateIndexModel<PracticeTask>(
                Builders<PracticeTask>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Ascending(x => x.SourceType),
                new CreateIndexOptions { Name = "ix_practice_tasks_user_source_type" })
        };

        return context.PracticeTasks.Indexes.CreateManyAsync(indexes, cancellationToken);
    }
}
