using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Services.Vocabulary.Words.Models;

/// <summary>
///     词书强类型 ID
/// </summary>
public record WordListId(long Value) : StrongId<long>(Value)
{
    public static readonly WordListId Empty = new(0);

    public static explicit operator long(WordListId id)
    {
        return id.Value;
    }

    public static implicit operator WordListId(long value)
    {
        return new WordListId(value);
    }
}