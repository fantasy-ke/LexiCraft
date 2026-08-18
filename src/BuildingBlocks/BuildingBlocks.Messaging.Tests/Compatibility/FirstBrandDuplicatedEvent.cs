namespace FirstBrand.Contracts;

/// <summary>
///     与 <see cref="SecondBrand.Contracts.DuplicatedEvent" /> 去掉首段后同名，用于验证歧义拒绝。
/// </summary>
internal sealed record DuplicatedEvent(Guid Id);
