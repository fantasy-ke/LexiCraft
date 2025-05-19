namespace Z.Local.EventBus;

/// <summary>
/// Eto for event.
/// </summary>
public sealed class EventEto
{
    /// <summary>
    /// Gets or sets the full name of the event.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Gets or sets the data of the event.
    /// </summary>
    public string Data { get; set; }

    public EventEto(string fullName, string data)
    {
        FullName = fullName;
        Data = data;
    }
}