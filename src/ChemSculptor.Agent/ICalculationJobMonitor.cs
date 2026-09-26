using ChemSculptor.Compute;

namespace ChemSculptor.Agent;

/// <summary>
/// 计算作业完成监控器。
/// 负责在后台等待进程结束，并在结束后触发输出解析和结果保存。
/// </summary>
public interface ICalculationJobMonitor
{
    /// <summary>开始监控一个已经提交的计算作业。</summary>
    void Start(CalculationJob job);
}
