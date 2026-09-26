namespace ChemSculptor.Agent;

/// <summary>
/// 计算作业完成监控配置。
/// </summary>
public sealed class CalculationJobMonitorOptions
{
    /// <summary>两次状态查询之间的等待时间，单位毫秒。</summary>
    public int PollingIntervalMilliseconds { get; set; } = 500;

    /// <summary>创建默认配置。</summary>
    public static CalculationJobMonitorOptions CreateDefault()
    {
        CalculationJobMonitorOptions options = new CalculationJobMonitorOptions();
        options.PollingIntervalMilliseconds = 500;
        return options;
    }
}
