namespace BuildingBlocks.Shared;

public class OAuthCallbackOption
{
    public GitHub GitHub { get; set; }

    public Gitee Gitee { get; set; }
}

public class GitHub
{
    public string ClientId { get; set; }

    public string ClientSecret { get; set; }
    
    public string Scope { get; set; }
}

public class Gitee
{
    public string ClientId { get; set; }

    public string ClientSecret { get; set; }
    
    public string Scope { get; set; }
}