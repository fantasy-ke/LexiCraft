namespace BuildingBlocks.Resilience;

/// <summary>
/// 弹性配置选项
/// </summary>
public class ResilienceOptions
{

    /// <summary>
    /// 重试次数，默认3次
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// 基础延迟时间（秒），默认1秒
    /// </summary>
    public double BaseDelaySeconds { get; set; } = 1.0;

    /// <summary>
    /// 是否使用指数退避，默认true
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// 最大延迟时间（秒），默认30秒
    /// </summary>
    public double MaxDelaySeconds { get; set; } = 30.0;

    /// <summary>
    /// 抖动因子，默认0.1（10%的随机抖动）
    /// </summary>
    public double JitterFactor { get; set; } = 0.1;
}