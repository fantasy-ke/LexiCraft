using BuildingBlocks.OSS.Models.Policy;

namespace BuildingBlocks.OSS.Models;

public class PolicyInfo
{
    /// <summary>
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// </summary>
    public required List<StatementItem> Statement { get; set; }
}