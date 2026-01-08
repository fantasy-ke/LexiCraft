using LexiCraft.Services.Practice.PracticeTasks.Models;
using LexiCraft.Services.Practice.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace LexiCraft.Services.Practice.PracticeTasks.Services;

/// <summary>
/// 用于生成词汇学习练习任务的服务
/// </summary>
public class PracticeTaskGenerator(
    IVocabularyServiceClient vocabularyClient,
    IPracticeTaskRepository practiceTaskRepository,
    ILogger<PracticeTaskGenerator> logger)
    : IPracticeTaskGenerator
{
    private readonly IPracticeTaskRepository _practiceTaskRepository = practiceTaskRepository;

    public async Task<List<PracticeTask>> GenerateTasksAsync(Guid userId, List<long> wordIds, PracticeType type, int count)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        if (!wordIds.Any())
        {
            throw new ArgumentException("Word IDs list cannot be empty", nameof(wordIds));
        }

        if (count <= 0)
        {
            throw new ArgumentException("Count must be greater than zero", nameof(count));
        }

        if (!Enum.IsDefined(typeof(PracticeType), type))
        {
            throw new ArgumentException("Invalid practice type", nameof(type));
        }

        try
        {
            logger.LogInformation("Generating {Count} practice tasks for user {UserId} with type {Type}", 
                count, userId, type);

            // Take only the requested number of word IDs
            var selectedWordIds = wordIds.Take(count).ToList();
            
            // Get word information from vocabulary service
            var words = await vocabularyClient.GetWordsByIdsAsync(selectedWordIds);
            
            if (!words.Any())
            {
                logger.LogWarning("No words found for the provided word IDs");
                return new List<PracticeTask>();
            }

            var tasks = new List<PracticeTask>();
            
            foreach (var word in words)
            {
                var task = CreatePracticeTask(userId, word, type);
                tasks.Add(task);
            }

            logger.LogInformation("Generated {TaskCount} practice tasks for user {UserId}", 
                tasks.Count, userId);

            return tasks;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating practice tasks for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PracticeTask> GenerateSingleTaskAsync(Guid userId, long wordId, PracticeType type)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        if (wordId <= 0)
        {
            throw new ArgumentException("Word ID must be greater than zero", nameof(wordId));
        }

        if (!Enum.IsDefined(typeof(PracticeType), type))
        {
            throw new ArgumentException("Invalid practice type", nameof(type));
        }

        try
        {
            logger.LogInformation("Generating single practice task for user {UserId}, word {WordId}, type {Type}", 
                userId, wordId, type);

            // Get word information from vocabulary service
            var word = await vocabularyClient.GetWordByIdAsync(wordId);
            
            if (word == null)
            {
                throw new InvalidOperationException($"Word with ID {wordId} not found");
            }

            var task = CreatePracticeTask(userId, word, type);

            logger.LogInformation("Generated practice task {TaskId} for user {UserId}", 
                task.Id, userId);

            return task;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating single practice task for user {UserId}, word {WordId}", 
                userId, wordId);
            throw;
        }
    }

    private PracticeTask CreatePracticeTask(Guid userId, WordDto word, PracticeType type)
    {
        var task = new PracticeTask
        {
            UserId = userId,
            WordId = word.Id,
            WordSpelling = word.Spelling,
            WordDefinition = ExtractDefinition(word.Definitions),
            WordPhonetic = word.Phonetic,
            PronunciationUrl = word.PronunciationUrl,
            Type = type,
            Status = PracticeTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Set expected answer based on practice type
        task.ExpectedAnswer = type switch
        {
            PracticeType.Dictation => word.Spelling,
            PracticeType.DefinitionToWord => word.Spelling,
            _ => throw new ArgumentException($"Unsupported practice type: {type}")
        };

        return task;
    }

    private string ExtractDefinition(string? definitions)
    {
        if (string.IsNullOrEmpty(definitions))
        {
            return "No definition available";
        }

        // If definitions is JSON, try to extract the first definition
        // For now, return as-is, but this could be enhanced to parse JSON
        return definitions;
    }
}