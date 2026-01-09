using LexiCraft.Services.Practice.Tasks.Models;
using MongoDB.Bson.Serialization;

namespace LexiCraft.Services.Practice.Tasks.Data.EntityConfigurations;

public static class PracticeTaskConfiguration
{
    public static void Configure()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(PracticeTask)))
        {
            BsonClassMap.RegisterClassMap<PracticeTask>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                
                cm.MapProperty(x => x.UserId).SetElementName("user_id");
                cm.MapProperty(x => x.TaskType).SetElementName("task_type");
                cm.MapProperty(x => x.SourceType).SetElementName("source_type");
                cm.MapProperty(x => x.Category).SetElementName("category");
                cm.MapProperty(x => x.Status).SetElementName("status");
                cm.MapProperty(x => x.StartedAt).SetElementName("started_at");
                cm.MapProperty(x => x.FinishedAt).SetElementName("finished_at");
                cm.MapProperty(x => x.Items).SetElementName("items");
                cm.MapProperty(x => x.Answers).SetElementName("answers");
            });
        }
    }
}