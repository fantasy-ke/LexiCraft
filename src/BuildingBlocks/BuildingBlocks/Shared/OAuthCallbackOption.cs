namespace BuildingBlocks.Shared;

public class OAuthCallbackOption
{
    public GitHub GitHub { get; set; } = new();

    public Gitee Gitee { get; set; } = new();
}

public class GitHub
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;
}

public class Gitee
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;
}