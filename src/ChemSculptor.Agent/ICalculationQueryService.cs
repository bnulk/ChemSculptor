using ChemSculptor.Compute;

namespace ChemSculptor.Agent;

/// <summary>
/// 计算作业查询服务。
/// </summary>
public interface ICalculationQueryService
{
    /// <summary>查询作业。</summary>
    Task<CalculationJob?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>查询规范化计算结果。</summary>
    Task<CalculationResult?> GetResultAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}
