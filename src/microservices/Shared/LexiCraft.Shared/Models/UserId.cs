using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Shared.Models;

/// <summary>
///     用户强类型 ID
/// </summary>
/// <param name="Value"></param>
public record UserId(Guid Value) : StrongId<Guid>(Value)
{
    public static readonly UserId Empty = new(Guid.Empty);

    public static UserId New()
    {
        return new UserId(Guid.NewGuid());
    }

    public static explicit operator Guid(UserId id)
    {
        return id.Value;
    }

    public static implicit operator UserId(Guid value)
    {
        return new UserId(value);
    }
}