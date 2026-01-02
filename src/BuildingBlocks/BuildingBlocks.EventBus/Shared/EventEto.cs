namespace BuildingBlocks.EventBus.Shared;

/// <summary>
/// 事件传输对象
/// </summary>
public sealed class EventEto(string fullName, string data)
{
    public string FullName { get; set; } = fullName;
    public string Data { get; set; } = data;
}
