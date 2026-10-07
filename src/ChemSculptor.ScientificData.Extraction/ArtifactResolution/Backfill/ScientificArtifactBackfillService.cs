using ChemSculptor.Compute;
using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Backfill.Models;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution.Backfill;

/// <summary>
/// 从 CalculationJob 和 CalculationResult 回填旧科学点的文件引用。
/// 已存在文件引用的科学点不会被覆盖。
/// </summary>
public sealed class ScientificArtifactBackfillService
    : IScientificArtifactBackfillService
{
    private readonly IScientificDataRepository _scientificDataRepository;
    private readonly ICalculationRepository _calculationRepository;

    /// <summary>创建文件引用回填服务。</summary>
    public ScientificArtifactBackfillService(
        IScientificDataRepository scientificDataRepository,
        ICalculationRepository calculationRepository)
    {
        if (scientificDataRepository == null)
        {
            throw new ArgumentNullException(
                nameof(scientificDataRepository));
        }

        if (calculationRepository == null)
        {
            throw new ArgumentNullException(
                nameof(calculationRepository));
        }

        _scientificDataRepository =
            scientificDataRepository;
        _calculationRepository = calculationRepository;
    }

    /// <summary>回填一项科学成果。</summary>
    public async Task<ScientificArtifactBackfillResult> BackfillAsync(
        string resultId,
        CancellationToken cancellationToken = default)
    {
        ScientificArtifactBackfillResult backfillResult =
            new ScientificArtifactBackfillResult();
        backfillResult.ResultId = resultId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(resultId))
        {
            backfillResult.Error =
                "科学成果标识不能为空。";
            return backfillResult;
        }

        ScientificResult? result =
            await _scientificDataRepository.GetAsync(
                resultId,
                cancellationToken);

        if (result == null)
        {
            backfillResult.Error =
                "没有找到科学成果：" + resultId;
            return backfillResult;
        }

        bool changed = false;

        for (int index = 0;
            index < result.PointSet.Points.Count;
            index++)
        {
            CalculationPoint point =
                result.PointSet.Points[index];
            backfillResult.PointsScanned++;
            ScientificArtifactBackfillPointResult pointResult =
                await BackfillPointAsync(
                    point,
                    cancellationToken);
            backfillResult.Points.Add(pointResult);

            if (pointResult.Backfilled)
            {
                backfillResult.PointsBackfilled++;
                changed = true;
            }
            else
            {
                backfillResult.PointsSkipped++;
            }
        }

        if (changed)
        {
            await _scientificDataRepository.SaveAsync(
                result,
                cancellationToken);
        }

        backfillResult.Succeeded = true;
        return backfillResult;
    }

    /// <summary>逐项回填仓储中的全部科学成果。</summary>
    public async Task<IReadOnlyList<ScientificArtifactBackfillResult>>
        BackfillAllAsync(
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ScientificResult> results =
            _scientificDataRepository.List();
        List<ScientificArtifactBackfillResult> backfillResults =
            new List<ScientificArtifactBackfillResult>();

        for (int index = 0;
            index < results.Count;
            index++)
        {
            ScientificArtifactBackfillResult result =
                await BackfillAsync(
                    results[index].Id,
                    cancellationToken);
            backfillResults.Add(result);
        }

        return backfillResults;
    }

    private async Task<ScientificArtifactBackfillPointResult>
        BackfillPointAsync(
            CalculationPoint point,
            CancellationToken cancellationToken)
    {
        ScientificArtifactBackfillPointResult result =
            new ScientificArtifactBackfillPointResult();
        result.PointId = point.Id;
        result.CalculationJobId =
            point.CalculationJobId;

        if (point.Artifacts == null)
        {
            point.Artifacts =
                new List<PointArtifactReference>();
        }

        if (point.Artifacts.Count > 0)
        {
            result.ArtifactCount = point.Artifacts.Count;
            result.Message = "科学点已经有文件清单。";
            return result;
        }

        if (string.IsNullOrWhiteSpace(
            point.CalculationJobId))
        {
            result.Message =
                "科学点没有计算作业标识，无法回填。";
            return result;
        }

        CalculationJob? job =
            await _calculationRepository.GetJobAsync(
                point.CalculationJobId,
                cancellationToken);

        if (job == null)
        {
            result.Message =
                "没有找到计算作业：" +
                point.CalculationJobId;
            return result;
        }

        CalculationResult? calculationResult =
            await _calculationRepository.GetResultAsync(
                point.CalculationJobId,
                cancellationToken);

        if (calculationResult == null
            || calculationResult.Artifacts == null
            || calculationResult.Artifacts.Count == 0)
        {
            result.Message =
                "计算作业没有可回填的文件清单。";
            return result;
        }

        string formula = GetMetadataValue(
            point.Metadata,
            "formula");
        string role =
            ScientificArtifactReferenceFactory.ResolveRole(
                point);
        string canonicalStem =
            ScientificArtifactReferenceFactory
                .BuildCanonicalStem(
                    formula,
                    role,
                    point.ElectronicState.Multiplicity);
        List<PointArtifactReference> references =
            ScientificArtifactReferenceFactory
                .CreateReferences(
                    job,
                    calculationResult,
                    canonicalStem);

        if (references.Count == 0)
        {
            result.Message =
                "计算作业文件清单没有可转换的引用。";
            return result;
        }

        point.Artifacts.AddRange(references);
        result.Backfilled = true;
        result.ArtifactCount = point.Artifacts.Count;
        result.Message =
            "已回填 " +
            references.Count.ToString() +
            " 个文件引用。";
        return result;
    }

    private static string GetMetadataValue(
        Dictionary<string, string> metadata,
        string key)
    {
        string? value;

        if (metadata.TryGetValue(key, out value))
        {
            return value ?? string.Empty;
        }

        return string.Empty;
    }
}
