using BuildingBlocks.Abstractions;
using LexiCraft.Services.Practice.Tasks.Models;
using LexiCraft.Services.Practice.Assessments.Models;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.Shared.Data;

/// <summary>
/// Data seeder for PracticeDbContext
/// </summary>
public class PracticeDbDataSeeder
{
    private readonly PracticeDbContext _context;

    public PracticeDbDataSeeder(PracticeDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Create indexes for better performance
        await CreateIndexesAsync(cancellationToken);

        // Add initial seed data if needed
        await SeedInitialDataAsync(cancellationToken);
    }

    private async Task CreateIndexesAsync(CancellationToken cancellationToken = default)
    {
        // Create indexes for PracticeTasks
        var practiceTaskIndexes = new[]
        {
            new CreateIndexModel<PracticeTask>(
                Builders<PracticeTask>.IndexKeys.Ascending(x => x.UserId)),
            new CreateIndexModel<PracticeTask>(
                Builders<PracticeTask>.IndexKeys.Ascending(x => x.Status)),
            new CreateIndexModel<PracticeTask>(
                Builders<PracticeTask>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.Status))
        };

        await _context.PracticeTasks.Indexes.CreateManyAsync(practiceTaskIndexes, cancellationToken: cancellationToken);

        // Create indexes for AnswerRecords
        var answerRecordIndexes = new[]
        {
            new CreateIndexModel<AnswerRecord>(
                Builders<AnswerRecord>.IndexKeys.Ascending(x => x.PracticeTaskItemId)),
            new CreateIndexModel<AnswerRecord>(
                Builders<AnswerRecord>.IndexKeys.Ascending(x => x.SubmittedAt))
        };

        await _context.AnswerRecords.Indexes.CreateManyAsync(answerRecordIndexes, cancellationToken: cancellationToken);

        // Create indexes for MistakeItems
        var mistakeItemIndexes = new[]
        {
            new CreateIndexModel<MistakeItem>(
                Builders<MistakeItem>.IndexKeys.Ascending(x => x.UserId)),
            new CreateIndexModel<MistakeItem>(
                Builders<MistakeItem>.IndexKeys.Ascending(x => x.WordId)),
            new CreateIndexModel<MistakeItem>(
                Builders<MistakeItem>.IndexKeys.Ascending(x => x.AnswerRecordId))
        };

        await _context.MistakeItems.Indexes.CreateManyAsync(mistakeItemIndexes, cancellationToken: cancellationToken);

        // Create indexes for PracticeTaskItems
        var practiceTaskItemIndexes = new[]
        {
            new CreateIndexModel<PracticeTaskItem>(
                Builders<PracticeTaskItem>.IndexKeys.Ascending(x => x.WordId)),
            new CreateIndexModel<PracticeTaskItem>(
                Builders<PracticeTaskItem>.IndexKeys.Ascending(x => x.OrderIndex))
        };

        await _context.PracticeTaskItems.Indexes.CreateManyAsync(practiceTaskItemIndexes, cancellationToken: cancellationToken);
    }

    private async Task SeedInitialDataAsync(CancellationToken cancellationToken = default)
    {
        // Add any initial seed data here if needed
        // For now, we'll keep this empty as practice data is user-generated
        await Task.CompletedTask;
    }
}