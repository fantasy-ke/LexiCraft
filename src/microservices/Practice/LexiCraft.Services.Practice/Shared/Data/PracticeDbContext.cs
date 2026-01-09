using BuildingBlocks.Abstractions;
using BuildingBlocks.MongoDB;
using Humanizer;
using LexiCraft.Services.Practice.Tasks.Models;
using LexiCraft.Services.Practice.Assessments.Models;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.Shared.Data;

public class PracticeDbContext : MongoDbContext
{
    public PracticeDbContext(IMongoDatabase database, IMongoClient client) : base(database, client)
    {
        PracticeTasks = Database.GetCollection<PracticeTask>(nameof(PracticeTask).Underscore());
        AnswerRecords = Database.GetCollection<AnswerRecord>(nameof(AnswerRecord).Underscore());
        MistakeItems = Database.GetCollection<MistakeItem>(nameof(MistakeItem).Underscore());
        PracticeTaskItems = Database.GetCollection<PracticeTaskItem>(nameof(PracticeTaskItem).Underscore());
    }

    // Collections for all domain models
    public IMongoCollection<PracticeTask> PracticeTasks { get; }
    public IMongoCollection<AnswerRecord> AnswerRecords { get; }
    public IMongoCollection<MistakeItem> MistakeItems { get; }
    public IMongoCollection<PracticeTaskItem> PracticeTaskItems { get; }
}