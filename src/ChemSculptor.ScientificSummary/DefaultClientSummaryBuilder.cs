using System.Globalization;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificSummary.Abstractions;
using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.ScientificSummary;

/// <summary>
/// 默认客户端摘要生成器。
/// 只根据 ScientificResult 生成通用显示内容。
/// </summary>
public sealed class DefaultClientSummaryBuilder
    : IClientSummaryBuilder
{
    /// <summary>生成客户端摘要。</summary>
    public Task<ClientScientificSummary> BuildAsync(
        ScientificResult result,
        CancellationToken cancellationToken = default)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        ClientScientificSummary summary =
            new ClientScientificSummary();
        summary.ResultId = result.Id;
        summary.Title = result.Title;
        summary.Status = result.Status.ToString();
        summary.Summary = result.Summary;

        ClientScientificSummarySection pointsSection =
            new ClientScientificSummarySection();
        pointsSection.Id = "points";
        pointsSection.Title = "计算点";
        pointsSection.Order = 1;
        pointsSection.Text =
            "共 " +
            result.PointSet.Points.Count.ToString(
                CultureInfo.InvariantCulture) +
            " 个计算点。";
        summary.Sections.Add(pointsSection);

        string? correctionCount;
        result.Metadata.TryGetValue(
            "correctionCount",
            out correctionCount);
        ClientScientificSummarySection correctionSection =
            new ClientScientificSummarySection();
        correctionSection.Id = "corrections";
        correctionSection.Title = "矫正统计";
        correctionSection.Order = 2;
        correctionSection.Text =
            "矫正次数：" +
            (correctionCount ?? "0") +
            "。";
        summary.Sections.Add(correctionSection);

        for (int index = 0;
            index < result.Observables.Count;
            index++)
        {
            ScientificObservable observable =
                result.Observables[index];
            ClientScientificSummarySection observableSection =
                new ClientScientificSummarySection();
            observableSection.Id =
                "observable-" +
                observable.Id;
            observableSection.Title = observable.Name;
            observableSection.Order = 3 + index;
            observableSection.Text =
                observable.NumericValue.HasValue
                    ? observable.NumericValue.Value.ToString(
                        CultureInfo.InvariantCulture) +
                      " " +
                      observable.Unit
                    : observable.TextValue;
            summary.Sections.Add(observableSection);
        }

        summary.Metadata["rootWorkflowId"] =
            result.Metadata.ContainsKey("rootWorkflowId")
                ? result.Metadata["rootWorkflowId"]
                : string.Empty;
        summary.Metadata["acceptedPointId"] =
            result.Metadata.ContainsKey("acceptedPointId")
                ? result.Metadata["acceptedPointId"]
                : string.Empty;
        return Task.FromResult(summary);
    }
}
