using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

/// <summary>
///     单词强类型 ID
/// </summary>
public record WordId(long Value) : StrongId<long>(Value)
{
    public static explicit operator long(WordId id) => id.Value;
    public static implicit operator WordId(long value) => new(value);
    public static WordId Empty => new(0);
}
