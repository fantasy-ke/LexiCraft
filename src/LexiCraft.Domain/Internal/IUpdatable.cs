namespace LexiCraft.Domain.Internal;


public interface IUpdatable<TUserKey> : IUpdatable
{
    /// <summary>
    /// 更新人id
    /// </summary>
    public TUserKey UpdateById { get; set; }
}

public interface IUpdatable
{
    /// <summary>
    /// 更新人
    /// </summary>
    public string UpdateByName { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateAt { get; set; }
}