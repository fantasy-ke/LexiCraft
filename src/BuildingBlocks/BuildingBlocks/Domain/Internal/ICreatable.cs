namespace BuildingBlocks.Domain.Internal;


public interface ICreatable<TUserKey> : ICreatable
{
    /// <summary>
    /// 创建人id
    /// </summary>
    public TUserKey? CreateById { get; set; }
}

public interface ICreatable
{
    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreateByName { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateAt { get; set; }
}