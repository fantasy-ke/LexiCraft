using LexiCraft.Services.Practice.Tasks.Models;
using MongoDB.Bson.Serialization;

namespace LexiCraft.Services.Practice.Tasks.Data.EntityConfigurations;

public static class PracticeTaskItemConfiguration
{
    public static void Configure()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(PracticeTaskItem)))
        {
            BsonClassMap.RegisterClassMap<PracticeTaskItem>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                
                cm.MapProperty(x => x.Id).SetElementName("id");
                cm.MapProperty(x => x.WordId).SetElementName("word_id");
                cm.MapProperty(x => x.SpellingSnapshot).SetElementName("spelling_snapshot");
                cm.MapProperty(x => x.PhoneticSnapshot).SetElementName("phonetic_snapshot");
                cm.MapProperty(x => x.PronunciationUrl).SetElementName("pronunciation_url");
                cm.MapProperty(x => x.DefinitionSnapshot).SetElementName("definition_snapshot");
                cm.MapProperty(x => x.OrderIndex).SetElementName("order_index");
            });
        }
    }
}