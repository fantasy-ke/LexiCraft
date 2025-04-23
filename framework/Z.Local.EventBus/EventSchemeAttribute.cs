namespace Z.Local.EventBus;

[AttributeUsage(AttributeTargets.Class)]
public class EventSchemeAttribute:Attribute
{
    /// <summary>
    /// 事件名称
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    ///   是否单读
    /// </summary>
    public bool SingleReader { get; set; } = true;

    /// <summary>
    ///   是否单写
    /// </summary>
    public bool SingleWriter { get; set; } = true;

    /// <summary>
    ///   是否允许同步 continuation
    /// </summary>
    public bool AllowSynchronousContinuations { get; set; } = true;

    public EventSchemeAttribute(
        string eventName)
    {
        EventName = eventName;
    }
}