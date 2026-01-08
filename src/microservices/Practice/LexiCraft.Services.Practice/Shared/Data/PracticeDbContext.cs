using BuildingBlocks.Abstractions;
using BuildingBlocks.MongoDB;
using LexiCraft.Services.Practice.Tasks.Models;
using LexiCraft.Services.Practice.Assessments.Models;
using LexiCraft.Services.Practice.Tasks.Data.EntityConfigurations;
using LexiCraft.Services.Practice.Assessments.Data.EntityConfigurations;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.Shared.Data;

public class PracticeDbContext : MongoDbContext
{
    public PracticeDbContext(IMongoDatabase database, IMongoClient client) : base(database, client)
    {
        // Register all entity configurations
        ConfigureEntityMappings();
    }

    // Collections for all domain models
    public IMongoCollection<PracticeTask> PracticeTasks => Database.GetCollection<PracticeTask>("practice_tasks");
    public IMongoCollection<AnswerRecord> AnswerRecords => Database.GetCollection<AnswerRecord>("answer_records");
    public IMongoCollection<MistakeItem> MistakeItems => Database.GetCollection<MistakeItem>("mistake_items");
    public IMongoCollection<PracticeTaskItem> PracticeTaskItems => Database.GetCollection<PracticeTaskItem>("practice_task_items");

    private static void ConfigureEntityMappings()
    {
        // Configure all entity mappings for MongoDB serialization
        PracticeTaskConfiguration.Configure();
        PracticeTaskItemConfiguration.Configure();
        AnswerRecordConfiguration.Configure();
        MistakeItemConfiguration.Configure();
    }
}