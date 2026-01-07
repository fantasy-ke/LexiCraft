namespace LexiCraft.Services.Practice.PracticeTasks.Services;

/// <summary>
/// DTO for word information from vocabulary service
/// </summary>
public record WordDto(
    long Id,
    string Spelling,
    string? Phonetic,
    string? PronunciationUrl,
    string? Definitions,
    string? Examples,
    List<string> Tags);

/// <summary>
/// Client for communicating with the Vocabulary service
/// </summary>
public interface IVocabularyServiceClient
{
    /// <summary>
    /// Gets word information by word ID
    /// </summary>
    /// <param name="wordId">The word ID</param>
    /// <returns>Word information or null if not found</returns>
    Task<WordDto?> GetWordByIdAsync(long wordId);

    /// <summary>
    /// Gets multiple words by their IDs
    /// </summary>
    /// <param name="wordIds">List of word IDs</param>
    /// <returns>List of word information</returns>
    Task<List<WordDto>> GetWordsByIdsAsync(List<long> wordIds);
}