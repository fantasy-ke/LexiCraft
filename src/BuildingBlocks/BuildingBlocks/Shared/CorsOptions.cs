namespace BuildingBlocks.Shared;

public class CorsOptions
{
    public string CorsName { get; set; } = string.Empty;

    public string?[] CorsArr { get; set; } = Array.Empty<string?>();
}