using System.Text.Json.Serialization;

namespace LexiCraft.Application.Contract.Authorize.Dto;

public class OAuthTokenDto
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = null!;
}