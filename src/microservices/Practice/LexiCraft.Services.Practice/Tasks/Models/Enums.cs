namespace LexiCraft.Services.Practice.Tasks.Models;

public enum PracticeTaskType
{
    AudioDictation,
    MeaningDictation
}

public enum PracticeTaskSource
{
    NewWord,
    Review,
    Recommendation,
    Manual
}

public enum PracticeStatus
{
    Created,
    InProgress,
    Finished
}