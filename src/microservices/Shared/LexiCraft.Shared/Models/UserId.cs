using BuildingBlocks.Domain.Internal;

namespace LexiCraft.Shared.Models;

/// <summary>
///     用户强类型 ID
/// </summary>
/// <param name="Value"></param>
public record UserId(Guid Value) : StrongId<Guid>(Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static explicit operator Guid(UserId id) => id.Value;
    public static implicit operator UserId(Guid value) => new(value);
    public static UserId Empty => new(Guid.Empty);
}
