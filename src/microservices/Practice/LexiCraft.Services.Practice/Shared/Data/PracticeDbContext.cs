using BuildingBlocks.MongoDB;
using LexiCraft.Services.Practice.PracticeTasks.Models;
using LexiCraft.Services.Practice.AnswerEvaluation.Models;
using LexiCraft.Services.Practice.MistakeAnalysis.Models;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.Shared.Data;

public class PracticeDbContext : MongoDbContext
{
    public IMongoCollection<PracticeTask> PracticeTasks { get; set; }
    public IMongoCollection<AnswerRecord> AnswerRecords { get; set; }
    public IMongoCollection<MistakeItem> MistakeItems { get; set; }

    public PracticeDbContext(IMongoDatabase database, IMongoClient client) : base(database, client)
    {
        PracticeTasks = Database.GetCollection<PracticeTask>("practice_tasks");
        AnswerRecords = Database.GetCollection<AnswerRecord>("answer_records");
        MistakeItems = Database.GetCollection<MistakeItem>("mistake_items");
    }
}