namespace BuildingBlocks.SerilogLogging;

public class SerilogOptions
{
    public bool EnableConsole { get; set; } = true;

    public string LogTemplate { get; set; } =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    public string? ApplicationName { get; set; }

    // Default File Sinks configuration
    public bool EnableAsync { get; set; } = true;
    public bool EnableFileLogging { get; set; } = true;
    public string LogPath { get; set; } = "AppData/Logs";
    public long FileSizeLimitBytes { get; set; } = 5000000; // 5MB
    public int RetainedFileCountLimit { get; set; } = 200;

    public SeqOptions Seq { get; set; } = new();

    // Overrides
    public Dictionary<string, string> MinimumLevelOverrides { get; set; } = new()
    {
        { "Microsoft", "Information" },
        { "System", "Warning" },
        { "Microsoft.AspNetCore", "Warning" },
        { "Microsoft.EntityFrameworkCore", "Warning" },
        { "Microsoft.Hosting.Lifetime", "Information" },
        { "Microsoft.EntityFrameworkCore.Database.Command", "Information" }
    };
}

public class SeqOptions
{
    public bool Enabled { get; set; }
    public string ServerUrl { get; set; } = "http://localhost:5341";
    public string ApiKey { get; set; } = string.Empty;
}