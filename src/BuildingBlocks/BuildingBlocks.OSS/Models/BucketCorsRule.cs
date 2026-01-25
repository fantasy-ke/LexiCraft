namespace BuildingBlocks.OSS.Models;

public class BucketCorsRule
{
    public string? Origin { get; set; }

    public HttpMethod Method { get; set; } = HttpMethod.Post;

    public string? AllowedHeader { get; set; }

    public string? ExposeHeader { get; set; }
}