using LexiCraft.Services.Practice.Assessments.Models;
using MongoDB.Bson.Serialization;

namespace LexiCraft.Services.Practice.Assessments.Data.EntityConfigurations;

public static class MistakeItemConfiguration
{
    public static void Configure()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(MistakeItem)))
        {
            BsonClassMap.RegisterClassMap<MistakeItem>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                
                // Don't map Id property - it's already handled by MongoEntity base class
                
                // Map properties with specific names
                cm.MapProperty(x => x.UserId).SetElementName("user_id");
                cm.MapProperty(x => x.WordId).SetElementName("word_id");
                cm.MapProperty(x => x.AnswerRecordId).SetElementName("answer_record_id");
                cm.MapProperty(x => x.MistakeType).SetElementName("mistake_type");
                cm.MapProperty(x => x.UserInput).SetElementName("user_input");
                cm.MapProperty(x => x.CorrectSpelling).SetElementName("correct_spelling");
                cm.MapProperty(x => x.OccurredAt).SetElementName("occurred_at");
            });
        }
    }
}