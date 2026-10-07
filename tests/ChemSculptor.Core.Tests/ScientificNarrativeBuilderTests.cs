using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificSummary;
using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.Core.Tests;

/// <summary>科学数据叙述生成测试。</summary>
public class ScientificNarrativeBuilderTests
{
    /// <summary>
    /// 验证全部叙述文本都引用科学点，并可追溯到关系和物理量。
    /// </summary>
    [Fact]
    public async Task BuildsPointTraceableNarrativesFromScientificData()
    {
        ScientificResult result = CreateResult();
        DefaultScientificNarrativeBuilder builder =
            new DefaultScientificNarrativeBuilder();

        ScientificNarrativePackage package =
            await builder.BuildAsync(result);

        Assert.Equal(result.Id, package.ResultId);
        Assert.Equal("job-original", package.RootJobId);
        Assert.Equal(2, package.Points.Count);
        Assert.Single(package.Relations);
        Assert.Single(package.Observables);

        ScientificPointNarrative original =
            FindPoint(package, "point-original");
        ScientificPointNarrative recovery =
            FindPoint(package, "point-recovery");

        Assert.Contains(
            "PointId：point-original",
            original.PointSummary);
        Assert.Contains(
            "PointId：point-original",
            original.Provenance);
        Assert.Contains(
            "ParentPointId：point-original",
            recovery.Provenance);
        Assert.Contains(
            "CorrectionCount：1",
            recovery.Provenance);

        Assert.Contains(
            "point-original",
            package.Organization);
        Assert.Contains(
            "point-recovery",
            package.Organization);
        Assert.Contains(
            "relation-1",
            package.Organization);
        Assert.Contains(
            "observable-energy",
            package.Organization);
        Assert.Contains(
            "point-recovery",
            package.Organization);

        Assert.Contains(
            "AcceptedPointId：point-recovery",
            package.FinalSummary);
        Assert.Contains(
            "单点能量",
            package.FinalSummary);
        Assert.Contains(
            "point-recovery",
            package.FinalSummary);
    }

    private static ScientificResult CreateResult()
    {
        ScientificResult result = new ScientificResult();
        result.Id = "result-1";
        result.Title = "氧气波函数稳定性矫正";
        result.Summary = "三重态修正点进入科学数据。";
        result.Status = ScientificResultStatus.Complete;
        result.Metadata["rootWorkflowId"] = "job-original";
        result.Metadata["acceptedPointId"] = "point-recovery";

        CalculationPoint original = CreatePoint(
            "point-original",
            CalculationPointStatus.Superseded,
            1,
            "job-original",
            string.Empty,
            0);
        CalculationPoint recovery = CreatePoint(
            "point-recovery",
            CalculationPointStatus.Accepted,
            3,
            "job-recovery",
            "point-original",
            1);
        result.PointSet.Points.Add(original);
        result.PointSet.Points.Add(recovery);

        CalculationPointRelation relation =
            new CalculationPointRelation();
        relation.Id = "relation-1";
        relation.Kind =
            CalculationPointRelationKind.DerivedFrom;
        relation.FromPointId = "point-original";
        relation.ToPointId = "point-recovery";
        relation.Description = "修正计算由原始计算派生。";
        result.PointSet.Relations.Add(relation);

        ScientificObservable observable =
            new ScientificObservable();
        observable.Id = "observable-energy";
        observable.Name = "单点能量";
        observable.Kind =
            ScientificObservableKind.SinglePointEnergy;
        observable.NumericValue = -150.0;
        observable.Unit = "Hartree";
        observable.Formula = "采用已接受计算点。";
        observable.PointIds.Add("point-recovery");
        observable.RelationIds.Add("relation-1");
        result.Observables.Add(observable);
        return result;
    }

    private static CalculationPoint CreatePoint(
        string pointId,
        CalculationPointStatus status,
        int multiplicity,
        string acceptedJobId,
        string parentPointId,
        int correctionCount)
    {
        CalculationPoint point = new CalculationPoint();
        point.Id = pointId;
        point.Name = pointId;
        point.Status = status;
        point.CalculationJobId = acceptedJobId;
        point.Metadata["formula"] = "O2";
        point.ElectronicState.Charge = 0;
        point.ElectronicState.Multiplicity = multiplicity;
        point.CalculationModel.Program = "gaussian-16";
        point.CalculationModel.Method = "CAM-B3LYP";
        point.CalculationModel.Basis = "6-31G*";
        point.Provenance.InitialJobId = "job-original";
        point.Provenance.AcceptedJobId = acceptedJobId;
        point.Provenance.RootWorkflowId = "job-original";
        point.Provenance.ParentPointId = parentPointId;
        point.Provenance.CorrectionCount = correctionCount;
        point.Provenance.AcceptedBy = "ScientificResultExtractor";
        point.Provenance.AcceptanceSummary =
            "由科学数据提取器接受。";

        ScientificProperty energy = new ScientificProperty();
        energy.Name = "energy";
        energy.Kind = ScientificPropertyKind.Energy;
        energy.NumericValue = -150.0;
        energy.Unit = "Hartree";
        point.Properties.Add(energy);
        return point;
    }

    private static ScientificPointNarrative FindPoint(
        ScientificNarrativePackage package,
        string pointId)
    {
        for (int index = 0;
            index < package.Points.Count;
            index++)
        {
            ScientificPointNarrative point =
                package.Points[index];

            if (string.Equals(
                point.PointId,
                pointId,
                StringComparison.OrdinalIgnoreCase))
            {
                return point;
            }
        }

        throw new InvalidOperationException(
            "没有找到叙述科学点：" + pointId);
    }
}
