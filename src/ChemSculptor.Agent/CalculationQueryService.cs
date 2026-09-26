using ChemSculptor.Compute;

namespace ChemSculptor.Agent;

/// <summary>
/// 基于计算仓储的查询服务。
/// </summary>
public sealed class CalculationQueryService : ICalculationQueryService
{
    private readonly ICalculationRepository _repository;

    /// <summary>创建查询服务。</summary>
    public CalculationQueryService(ICalculationRepository repository)
    {
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _repository = repository;
    }

    /// <summary>查询作业。</summary>
    public Task<CalculationJob?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetJobAsync(jobId, cancellationToken);
    }

    /// <summary>查询规范化计算结果。</summary>
    public Task<CalculationResult?> GetResultAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetResultAsync(jobId, cancellationToken);
    }
}
