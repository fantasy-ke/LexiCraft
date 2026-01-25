namespace BuildingBlocks.EventBus.Shared;

/// <summary>
///     事件方案特性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EventSchemeAttribute(string? eventName = null) : Attribute
{
    public string? EventName { get; set; } = eventName;
    public bool SingleReader { get; set; } = true;
    public bool SingleWriter { get; set; } = true;
    public bool AllowSynchronousContinuations { get; set; } = true;
}