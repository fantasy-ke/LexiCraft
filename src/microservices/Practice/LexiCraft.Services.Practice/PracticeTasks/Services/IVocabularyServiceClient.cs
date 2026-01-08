namespace LexiCraft.Services.Practice.PracticeTasks.Services;

/// <summary>
/// 来自词汇服务的单词信息DTO
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
/// 用于与词汇服务通信的客户端
/// </summary>
public interface IVocabularyServiceClient
{
    /// <summary>
    /// 根据单词ID获取单词信息
    /// </summary>
    /// <param name="wordId">单词ID</param>
    /// <returns>单词信息，如果未找到则返回null</returns>
    Task<WordDto?> GetWordByIdAsync(long wordId);

    /// <summary>
    /// 根据ID获取多个单词
    /// </summary>
    /// <param name="wordIds">单词ID列表</param>
    /// <returns>单词信息列表</returns>
    Task<List<WordDto>> GetWordsByIdsAsync(List<long> wordIds);
}