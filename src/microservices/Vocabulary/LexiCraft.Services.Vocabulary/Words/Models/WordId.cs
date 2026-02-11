using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

/// <summary>
///     单词强类型 ID
/// </summary>
public record WordId(long Value) : StrongId<long>(Value)
{
    public static readonly WordId Empty = new(0);

    public static explicit operator long(WordId id)
    {
        return id.Value;
    }

    public static implicit operator WordId(long value)
    {
        return new WordId(value);
    }
}