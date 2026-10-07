using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.Api;

/// <summary>按计算作业标识查找所属科学成果。</summary>
public static class ScientificResultLookup
{
    /// <summary>
    /// 查找根作业、恢复作业或派生作业所属的科学成果。
    /// </summary>
    public static ScientificResult? FindByJobId(
        IScientificDataRepository repository,
        string jobId)
    {
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        if (string.IsNullOrWhiteSpace(jobId))
        {
            return null;
        }

        IReadOnlyList<ScientificResult> results =
            repository.List();

        for (int resultIndex = 0;
            resultIndex < results.Count;
            resultIndex++)
        {
            ScientificResult result = results[resultIndex];

            if (string.Equals(
                result.Id,
                "scientific-result-" + jobId,
                StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            string? rootWorkflowId;

            if (result.Metadata.TryGetValue(
                "rootWorkflowId",
                out rootWorkflowId)
                && string.Equals(
                    rootWorkflowId,
                    jobId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            if (ContainsJob(result, jobId))
            {
                return result;
            }
        }

        return null;
    }

    private static bool ContainsJob(
        ScientificResult result,
        string jobId)
    {
        for (int index = 0;
            index < result.PointSet.Points.Count;
            index++)
        {
            CalculationPoint point =
                result.PointSet.Points[index];

            if (string.Equals(
                point.CalculationJobId,
                jobId,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
