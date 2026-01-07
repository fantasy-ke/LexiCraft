using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.PracticeTasks.Services;

/// <summary>
/// HTTP client implementation for communicating with the Vocabulary service
/// </summary>
public class VocabularyServiceClient : IVocabularyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VocabularyServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public VocabularyServiceClient(HttpClient httpClient, ILogger<VocabularyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<WordDto?> GetWordByIdAsync(long wordId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/vocabulary/words/{wordId}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Word with ID {WordId} not found", wordId);
                    return null;
                }
                
                _logger.LogError("Failed to get word {WordId}. Status: {StatusCode}", wordId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var word = JsonSerializer.Deserialize<WordDto>(content, _jsonOptions);
            
            return word;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting word {WordId} from vocabulary service", wordId);
            return null;
        }
    }

    public async Task<List<WordDto>> GetWordsByIdsAsync(List<long> wordIds)
    {
        if (!wordIds.Any())
        {
            return new List<WordDto>();
        }

        try
        {
            var idsQuery = string.Join(",", wordIds);
            var response = await _httpClient.GetAsync($"api/v1/vocabulary/words/batch?ids={idsQuery}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get words by IDs. Status: {StatusCode}", response.StatusCode);
                return new List<WordDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var words = JsonSerializer.Deserialize<List<WordDto>>(content, _jsonOptions);
            
            return words ?? new List<WordDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting words by IDs from vocabulary service");
            return new List<WordDto>();
        }
    }
}