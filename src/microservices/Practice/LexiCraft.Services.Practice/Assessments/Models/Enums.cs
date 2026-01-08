namespace LexiCraft.Services.Practice.Assessments.Models;

public enum AnswerStatus
{
    Correct,
    Partial,
    Wrong,
    NoAnswer
}

public enum MistakeType
{
    SpellingError,
    CompletelyWrong,
    NoAnswer
}

public enum AssessmentType
{
    Exact,
    Fuzzy
}