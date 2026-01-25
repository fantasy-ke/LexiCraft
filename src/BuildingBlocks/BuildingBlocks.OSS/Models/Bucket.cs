namespace BuildingBlocks.OSS.Models;

public class Bucket
{
    private DateTime _creationDate = DateTime.MinValue;

    /// <summary>
    ///     Bucket location getter/setter
    /// </summary>
    public string Location { get; internal set; } = string.Empty;

    /// <summary>
    ///     Bucket name getter/setter
    /// </summary>
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    ///     Bucket <see cref="Owner" /> getter/setter
    /// </summary>
    public Owner Owner { get; internal set; } = new();

    /// <summary>
    ///     Bucket creation time getter/setter
    /// </summary>
    public string CreationDate
    {
        get => _creationDate.ToString("yyyy-MM-dd HH:mm:ss");
        internal set
        {
            if (DateTime.TryParse(value, out var dt)) _creationDate = dt;
            _creationDate = DateTime.MinValue;
        }
    }
}