namespace LexiCraft.Domain.Internal;

public interface ISoftDeleted<TUserKey> : ISoftDeleted
{
    /// <summary>
    /// 删除人id
    /// </summary>
    public TUserKey? DeleteById { get; set; }
}

public interface ISoftDeleted
{
    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 删除人
    /// </summary>
    public string? DeleteByName { get; set; }
    
    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime? DeleteAt { get; set; }
    
}