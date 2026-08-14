using BuildingBlocks.MongoDB.Context;
using LexiCraft.Services.Practice.Tasks.Models;
using MongoDB.Driver;

namespace LexiCraft.Services.Practice.Shared.Data;

public class PracticeDbContext : MongoDbContext
{
    public const string PracticeTasksCollectionName = "practice_tasks";

    public PracticeDbContext(IMongoDatabase database, IMongoClient client) : base(database, client)
    {
        PracticeTasks = Database.GetCollection<PracticeTask>(PracticeTasksCollectionName);
    }

    public IMongoCollection<PracticeTask> PracticeTasks { get; }
}
