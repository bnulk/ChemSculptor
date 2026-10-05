using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.ScientificData.Storage;

/// <summary>
/// 科学数据仓储。
/// 计算层和报告层通过该接口交换科学成果。
/// </summary>
public interface IScientificDataRepository
{
    /// <summary>保存一项科学成果。</summary>
    Task SaveAsync(
        ScientificResult result,
        CancellationToken cancellationToken = default);

    /// <summary>按标识读取一项科学成果。</summary>
    Task<ScientificResult?> GetAsync(
        string resultId,
        CancellationToken cancellationToken = default);

    /// <summary>列出全部科学成果。</summary>
    IReadOnlyList<ScientificResult> List();
}
