namespace BuildingBlocks.Authentication.Abstractions;

public interface IJwtTokenProvider
{
    string GenerateAccessToken(Dictionary<string, string> dist, Guid userId, string[] roles);

    string GenerateRefreshToken();
}