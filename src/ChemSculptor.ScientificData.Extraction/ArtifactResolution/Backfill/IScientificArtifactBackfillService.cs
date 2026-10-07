using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Backfill.Models;

namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution.Backfill;

/// <summary>把旧科学点按计算作业结果增量回填文件引用。</summary>
public interface IScientificArtifactBackfillService
{
    /// <summary>回填一项科学成果。</summary>
    Task<ScientificArtifactBackfillResult> BackfillAsync(
        string resultId,
        CancellationToken cancellationToken = default);

    /// <summary>逐项回填仓储中的全部科学成果。</summary>
    Task<IReadOnlyList<ScientificArtifactBackfillResult>>
        BackfillAllAsync(
            CancellationToken cancellationToken = default);
}
