namespace BuildingBlocks.MongoDB.Configuration;

public class MongoOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool DisableTracing { get; set; }
}
