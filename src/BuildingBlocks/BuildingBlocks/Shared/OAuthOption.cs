namespace BuildingBlocks.Shared;

public class OAuthOption
{
    public GitHub GitHub { get; set; }

    public Gitee Gitee { get; set; }
}

public class GitHub
{
    public string ClientId { get; set; }

    public string ClientSecret { get; set; }
}

public class Gitee
{
    public string ClientId { get; set; }

    public string ClientSecret { get; set; }
}