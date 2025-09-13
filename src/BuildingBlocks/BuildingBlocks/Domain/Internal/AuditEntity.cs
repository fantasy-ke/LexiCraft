namespace BuildingBlocks.Domain.Internal;

public abstract class AuditEntity<TKey,TUserKey> : Entity<TKey>,
    ICreatable<TUserKey>, IUpdatable<TUserKey>, ISoftDeleted<TUserKey>
{
    public string? CreateByName { get; set; }
    
    public DateTime CreateAt { get; set; }
    
    public TUserKey? CreateById { get; set; }
    
    public string? UpdateByName { get; set; }
    
    public DateTime? UpdateAt { get; set; }
    
    public TUserKey? UpdateById { get; set; }
    
    public bool IsDeleted { get; set; }
    
    public string? DeleteByName { get; set; }
    
    public DateTime? DeleteAt { get; set; }
    
    public TUserKey? DeleteById { get; set; }
}



public abstract class AuditEntity<TKey> : SimpleAuditEntity<TKey,Guid?>;


public abstract class SimpleAuditEntity<TKey,TUserKey> : Entity<TKey>,
    ICreatable<TUserKey>, IUpdatable<TUserKey>
{
    public string? CreateByName { get; set; }
    
    public DateTime CreateAt { get; set; }
    
    public TUserKey? CreateById { get; set; }
    
    public string? UpdateByName { get; set; }
    
    public DateTime? UpdateAt { get; set; }
    
    public TUserKey? UpdateById { get; set; }
}

public abstract class SimpleAuditEntity<TKey> : SimpleAuditEntity<TKey,Guid?>;