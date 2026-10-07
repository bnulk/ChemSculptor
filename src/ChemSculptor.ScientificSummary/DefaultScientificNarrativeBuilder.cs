using System.Globalization;
using System.Text;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificSummary.Abstractions;
using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.ScientificSummary;

/// <summary>
/// 默认叙述生成器。
/// 只读取 ScientificResult，不读取计算输出、客户端消息或外部结论。
/// </summary>
public sealed class DefaultScientificNarrativeBuilder
    : IScientificNarrativeBuilder
{
    /// <summary>生成科学成果叙述包。</summary>
    public Task<ScientificNarrativePackage> BuildAsync(
        ScientificResult result,
        CancellationToken cancellationToken = default)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        ScientificNarrativePackage package =
            new ScientificNarrativePackage();
        package.ResultId = result.Id;
        package.RootJobId = GetMetadataValue(
            result.Metadata,
            "rootWorkflowId");
        package.Relations.AddRange(result.PointSet.Relations);
        package.Observables.AddRange(result.Observables);

        for (int index = 0;
            index < result.PointSet.Points.Count;
            index++)
        {
            CalculationPoint point =
                result.PointSet.Points[index];
            ScientificPointNarrative narrative =
                new ScientificPointNarrative();
            narrative.PointId = point.Id;
            narrative.Sequence = index + 1;
            narrative.PointSummary =
                BuildPointSummary(
                    point,
                    index + 1);
            narrative.Provenance =
                BuildProvenance(
                    point,
                    index + 1);
            package.Points.Add(narrative);
        }

        package.Organization =
            BuildOrganization(result, package.Points);
        package.FinalSummary =
            BuildFinalSummary(
                result,
                package.Points,
                package.Observables);
        return Task.FromResult(package);
    }

    private static string BuildPointSummary(
        CalculationPoint point,
        int sequence)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("PointId：" + point.Id);
        builder.AppendLine(
            "Sequence：" +
            sequence.ToString(
                CultureInfo.InvariantCulture));
        builder.AppendLine("名称：" + point.Name);
        builder.AppendLine("状态：" + point.Status);
        builder.AppendLine("类型：" + point.Kind);

        string formula = GetMetadataValue(
            point.Metadata,
            "formula");

        if (!string.IsNullOrWhiteSpace(formula))
        {
            builder.AppendLine("分子式：" + formula);
        }

        builder.AppendLine(
            "原子数：" +
            point.Geometry.Atoms.Count.ToString(
                CultureInfo.InvariantCulture));
        builder.AppendLine("电荷：" + point.ElectronicState.Charge);
        builder.AppendLine(
            "多重度：" +
            point.ElectronicState.Multiplicity);
        builder.AppendLine(
            "计算程序：" +
            point.CalculationModel.Program);
        builder.AppendLine(
            "方法：" +
            point.CalculationModel.Method);
        builder.AppendLine(
            "基组：" +
            point.CalculationModel.Basis);

        builder.AppendLine(
            "文件清单：" +
            point.Artifacts.Count.ToString(
                CultureInfo.InvariantCulture) +
            " 个文件。");

        for (int index = 0;
            index < point.Properties.Count;
            index++)
        {
            ScientificProperty property =
                point.Properties[index];
            builder.AppendLine(
                "性质：" +
                property.Name +
                " = " +
                FormatProperty(property));
        }

        for (int index = 0;
            index < point.Validations.Count;
            index++)
        {
            PointValidationRecord validation =
                point.Validations[index];
            builder.AppendLine(
                "验证：" +
                validation.Code +
                " = " +
                validation.Status +
                "；" +
                validation.Summary);
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildProvenance(
        CalculationPoint point,
        int sequence)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("PointId：" + point.Id);
        builder.AppendLine(
            "Sequence：" +
            sequence.ToString(
                CultureInfo.InvariantCulture));
        builder.AppendLine(
            "InitialJobId：" +
            point.Provenance.InitialJobId);
        builder.AppendLine(
            "AcceptedJobId：" +
            point.Provenance.AcceptedJobId);
        builder.AppendLine(
            "RootWorkflowId：" +
            point.Provenance.RootWorkflowId);
        builder.AppendLine(
            "ParentPointId：" +
            point.Provenance.ParentPointId);
        builder.AppendLine(
            "CorrectionCount：" +
            point.Provenance.CorrectionCount.ToString(
                CultureInfo.InvariantCulture));
        builder.AppendLine(
            "AcceptedBy：" +
            point.Provenance.AcceptedBy);
        builder.AppendLine(
            "AcceptanceSummary：" +
            point.Provenance.AcceptanceSummary);
        return builder.ToString().TrimEnd();
    }

    private static string BuildOrganization(
        ScientificResult result,
        List<ScientificPointNarrative> pointNarratives)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("ResultId：" + result.Id);
        builder.AppendLine("点集：" + result.PointSet.Name);
        builder.AppendLine();
        builder.AppendLine("科学点：");

        for (int index = 0;
            index < pointNarratives.Count;
            index++)
        {
            ScientificPointNarrative point =
                pointNarratives[index];
            builder.Append("- PointId：");
            builder.AppendLine(point.PointId);
        }

        builder.AppendLine();
        builder.AppendLine("关系：");

        if (result.PointSet.Relations.Count == 0)
        {
            builder.AppendLine("- 无关系记录。");
        }
        else
        {
            for (int index = 0;
                index < result.PointSet.Relations.Count;
                index++)
            {
                CalculationPointRelation relation =
                    result.PointSet.Relations[index];
                builder.Append("- RelationId：");
                builder.AppendLine(relation.Id);
                builder.Append("  ");
                builder.Append(relation.FromPointId);
                builder.Append(" -> ");
                builder.Append(relation.ToPointId);
                builder.Append("；");
                builder.Append(relation.Kind);
                builder.Append("；");
                builder.AppendLine(relation.Description);
            }
        }

        builder.AppendLine();
        builder.AppendLine("物理量：");

        if (result.Observables.Count == 0)
        {
            builder.AppendLine("- 无物理量记录。");
        }
        else
        {
            for (int index = 0;
                index < result.Observables.Count;
                index++)
            {
                ScientificObservable observable =
                    result.Observables[index];
                builder.Append("- ObservableId：");
                builder.AppendLine(observable.Id);
                builder.Append("  PointIds：");
                builder.AppendLine(
                    string.Join(
                        ", ",
                        observable.PointIds));
                builder.Append("  RelationIds：");
                builder.AppendLine(
                    string.Join(
                        ", ",
                        observable.RelationIds));
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildFinalSummary(
        ScientificResult result,
        List<ScientificPointNarrative> pointNarratives,
        List<ScientificObservable> observables)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("ResultId：" + result.Id);
        builder.AppendLine("状态：" + result.Status);
        builder.AppendLine("标题：" + result.Title);

        if (!string.IsNullOrWhiteSpace(result.Summary))
        {
            builder.AppendLine("摘要：" + result.Summary);
        }

        string acceptedPointId = GetMetadataValue(
            result.Metadata,
            "acceptedPointId");

        if (!string.IsNullOrWhiteSpace(acceptedPointId))
        {
            builder.AppendLine(
                "AcceptedPointId：" +
                acceptedPointId);
        }

        builder.AppendLine();
        builder.AppendLine("叙述点：");

        for (int index = 0;
            index < pointNarratives.Count;
            index++)
        {
            builder.Append("- PointId：");
            builder.AppendLine(
                pointNarratives[index].PointId);
        }

        builder.AppendLine();
        builder.AppendLine("最终物理量：");

        if (observables.Count == 0)
        {
            builder.AppendLine("- 没有来自科学数据的物理量。");
        }
        else
        {
            for (int index = 0;
                index < observables.Count;
                index++)
            {
                ScientificObservable observable =
                    observables[index];
                builder.Append("- ");
                builder.Append(observable.Name);
                builder.Append("：");
                builder.Append(
                    FormatObservable(observable));
                builder.Append("；PointIds：");
                builder.AppendLine(
                    string.Join(
                        ", ",
                        observable.PointIds));
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatProperty(
        ScientificProperty property)
    {
        if (property.NumericValue.HasValue)
        {
            return property.NumericValue.Value.ToString(
                       "G17",
                       CultureInfo.InvariantCulture) +
                   " " +
                   property.Unit;
        }

        return property.TextValue;
    }

    private static string FormatObservable(
        ScientificObservable observable)
    {
        if (observable.NumericValue.HasValue)
        {
            return observable.NumericValue.Value.ToString(
                       "G17",
                       CultureInfo.InvariantCulture) +
                   " " +
                   observable.Unit;
        }

        return observable.TextValue;
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
