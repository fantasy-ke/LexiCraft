using BuildingBlocks.MongoDB;
using LexiCraft.Services.Practice.Assessments.Models;

namespace LexiCraft.Services.Practice.Tasks.Models;

public class PracticeTask : MongoEntity
{
    public string UserId { get; private set; } = string.Empty;
    public PracticeTaskType TaskType { get; private set; }
    public PracticeTaskSource SourceType { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public PracticeStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    
    public List<PracticeTaskItem> Items { get; private set; } = new();
    public List<AnswerRecord> Answers { get; private set; } = new();
    
    public static PracticeTask Create(string userId, PracticeTaskType type, PracticeTaskSource source, string category, List<PracticeTaskItem> items)
    {
        var task = new PracticeTask
        {
            UserId = userId,
            TaskType = type,
            SourceType = source,
            Category = category,
            Status = PracticeStatus.Created,
            Items = items
            // Id is initialized by MongoEntity base
        };
        return task;
    }

    public void Start()
    {
        if (Status == PracticeStatus.Created)
        {
            Status = PracticeStatus.InProgress;
            StartedAt = DateTime.UtcNow;
        }
    }

    public AssessmentResult SubmitAnswer(Guid itemId, string? input, AssessmentType assessmentType)
    {
        Start(); // Ensure started

        var item = Items.FirstOrDefault(x => x.Id == itemId);
        if (item == null) throw new InvalidOperationException("Item not found in this task");

        // Logic for checking correctness
        var correctSpelling = item.SpellingSnapshot;
        var normalizedInput = input?.Trim().ToLowerInvariant();
        var normalizedCorrect = correctSpelling.Trim().ToLowerInvariant();

        var status = AnswerStatus.Wrong;
        var score = 0;

        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            status = AnswerStatus.NoAnswer;
        }
        else if (normalizedInput == normalizedCorrect)
        {
            status = AnswerStatus.Correct;
            score = 100;
        }
        // Simplified fuzzy logic
        else if (assessmentType == AssessmentType.Fuzzy && normalizedInput.Length > 0 && Math.Abs(normalizedInput.Length - normalizedCorrect.Length) <= 2)
        {
            status = AnswerStatus.Partial;
            score = 50; 
        }

        var record = new AnswerRecord(itemId, input, status, score, 0, assessmentType);
        Answers.Add(record);
        
        return new AssessmentResult(record.Id.ToString(), status, score, correctSpelling);
    }

    public void Complete()
    {
        if (Status == PracticeStatus.Finished) return;
        Status = PracticeStatus.Finished;
        FinishedAt = DateTime.UtcNow;
    }
}

public record AssessmentResult(string AnswerId, AnswerStatus Status, int Score, string CorrectSpelling);