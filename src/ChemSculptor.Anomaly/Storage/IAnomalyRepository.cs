using ChemSculptor.Anomaly.Models;

namespace ChemSculptor.Anomaly.Storage;

/// <summary>异常处理记录仓储。</summary>
public interface IAnomalyRepository
{
    /// <summary>保存异常处理记录。</summary>
    Task SaveAsync(
        AnomalyRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>读取一条异常处理记录。</summary>
    Task<AnomalyRecord?> GetAsync(
        string jobId,
        string recordId,
        CancellationToken cancellationToken = default);

    /// <summary>列出指定作业的全部异常处理记录。</summary>
    Task<IReadOnlyList<AnomalyRecord>> ListByJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}
