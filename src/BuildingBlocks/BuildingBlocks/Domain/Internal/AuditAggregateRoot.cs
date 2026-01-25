namespace BuildingBlocks.Domain.Internal;

/// <summary>
///     带有审计功能的聚合根基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TUserKey"></typeparam>
public abstract class AuditAggregateRoot<TKey, TUserKey> : AuditEntity<TKey, TUserKey>, IAggregateRoot;

/// <summary>
///     带有审计功能的聚合根基类 (默认用户主键为 Guid?)
/// </summary>
/// <typeparam name="TKey"></typeparam>
public abstract class AuditAggregateRoot<TKey> : AuditAggregateRoot<TKey, Guid?>;

/// <summary>
///     带有简单审计功能的聚合根基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TUserKey"></typeparam>
public abstract class SimpleAuditAggregateRoot<TKey, TUserKey> : SimpleAuditEntity<TKey, TUserKey>, IAggregateRoot;

/// <summary>
///     带有简单审计功能的聚合根基类 (默认用户主键为 Guid?)
/// </summary>
/// <typeparam name="TKey"></typeparam>
public abstract class SimpleAuditAggregateRoot<TKey> : SimpleAuditAggregateRoot<TKey, Guid?>;