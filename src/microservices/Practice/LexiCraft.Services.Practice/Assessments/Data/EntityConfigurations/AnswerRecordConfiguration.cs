using LexiCraft.Services.Practice.Assessments.Models;
using MongoDB.Bson.Serialization;

namespace LexiCraft.Services.Practice.Assessments.Data.EntityConfigurations;

public static class AnswerRecordConfiguration
{
    public static void Configure()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(AnswerRecord)))
        {
            BsonClassMap.RegisterClassMap<AnswerRecord>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                
                // Don't map Id property - it's already handled by MongoEntity base class
                
                // Map properties with specific names
                cm.MapProperty(x => x.PracticeTaskItemId).SetElementName("practice_task_item_id");
                cm.MapProperty(x => x.UserInput).SetElementName("user_input");
                cm.MapProperty(x => x.Status).SetElementName("status");
                cm.MapProperty(x => x.Score).SetElementName("score");
                cm.MapProperty(x => x.ResponseTimeMs).SetElementName("response_time_ms");
                cm.MapProperty(x => x.SubmittedAt).SetElementName("submitted_at");
                cm.MapProperty(x => x.AssessmentType).SetElementName("assessment_type");
            });
        }
    }
}