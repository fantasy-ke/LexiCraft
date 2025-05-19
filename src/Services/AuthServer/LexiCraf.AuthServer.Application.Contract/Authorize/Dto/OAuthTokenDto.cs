using System.Text.Json.Serialization;

namespace LexiCraf.AuthServer.Application.Contract.Authorize.Dto;

public class OAuthTokenDto
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = null!;
}