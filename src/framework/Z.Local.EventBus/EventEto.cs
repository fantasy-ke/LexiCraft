namespace Z.Local.EventBus;

/// <summary>
/// Eto for event.
/// </summary>
public sealed class EventEto(string fullName, string data)
{
    /// <summary>
    /// Gets or sets the full name of the event.
    /// </summary>
    public string FullName { get; set; } = fullName;

    /// <summary>
    /// Gets or sets the data of the event.
    /// </summary>
    public string Data { get; set; } = data;
}