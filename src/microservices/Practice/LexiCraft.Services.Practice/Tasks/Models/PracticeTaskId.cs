using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Practice.Tasks.Models;

/// <summary>
///     练习任务强类型 ID
/// </summary>
public record PracticeTaskId(string Value) : StrongId<string>(Value)
{
    public static implicit operator string(PracticeTaskId id)
    {
        return id.Value;
    }

    public static implicit operator PracticeTaskId(string value)
    {
        return new PracticeTaskId(value);
    }
}