using Newtonsoft.Json;

namespace BuildingBlocks.OSS.Models.Policy;

public class StatementItem
{
    /// <summary>
    /// </summary>
    public string Effect { get; set; } = string.Empty;

    /// <summary>
    /// </summary>
    public Principal Principal { get; set; } = new();

    /// <summary>
    /// </summary>
    public List<string> Action { get; set; } = new();

    /// <summary>
    /// </summary>
    public List<string> Resource { get; set; } = new();

    /// <summary>
    ///     是否删除
    /// </summary>
    [JsonIgnore]
    public bool IsDelete { get; set; } = false;
}