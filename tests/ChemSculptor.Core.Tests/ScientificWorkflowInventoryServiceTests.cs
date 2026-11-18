using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.Core.Tests;

/// <summary>科学成果与根工作流作业关联审计测试。</summary>
public class ScientificWorkflowInventoryServiceTests
{
    /// <summary>验证原始作业和恢复作业可以映射到同一个科学点。</summary>
    [Fact]
    public async Task MapsOriginalAndRecoveryJobsToOneScientificPoint()
    {
        string root = "workflow-scientific-inventory";
        CalculationWorkflowJobInventory inventory = CreateInventory(
            root,
            "job-original",
            "job-recovery");
        ScientificResult result = new ScientificResult();
        result.Id = "result-original-recovery";
        result.PointSet.Id = "point-set-original-recovery";
        result.PointSet.Points.Add(CreatePoint(
            "point-original-recovery",
            root,
            "job-recovery",
            "job-original",
            "job-recovery"));
        StubScientificRepository scientific =
            new StubScientificRepository(new[] { result });
        ScientificWorkflowInventoryService service =
            CreateService(inventory, scientific);

        ScientificWorkflowInventory actual = await service.QueryAsync(
            new ScientificWorkflowInventoryQuery
            {
                RootWorkflowId = root
            });

        ScientificWorkflowResultAssociation association =
            Assert.Single(actual.Results);
        ScientificWorkflowPointAssociation point =
            Assert.Single(association.Points);
        Assert.True(association.MatchesRootWorkflow);
        Assert.True(point.MatchesRootWorkflow);
        Assert.Contains("job-original", point.ReferencedJobIds);
        Assert.Contains("job-recovery", point.ReferencedJobIds);
        Assert.Contains(
            point.Jobs,
            job => job.JobId == "job-original"
                && job.Kind == ScientificWorkflowJobReferenceKind.Initial);
        Assert.Contains(
            point.Jobs,
            job => job.JobId == "job-recovery"
                && job.Kind == ScientificWorkflowJobReferenceKind.Calculation);
        Assert.DoesNotContain(
            actual.Issues,
            issue => issue.Code
                == ScientificWorkflowAssociationIssueCodes
                    .PointCalculationJobNotFound);
    }

    /// <summary>验证一个科学成果可以包含多个科学点和多个作业。</summary>
    [Fact]
    public async Task MapsMultiplePointsAndJobsInOneResult()
    {
        string root = "workflow-multiple-points";
        CalculationWorkflowJobInventory inventory = CreateInventory(
            root,
            "job-opt",
            "job-frequency");
        ScientificResult result = new ScientificResult();
        result.Id = "result-multiple-points";
        result.PointSet.Points.Add(CreatePoint(
            "point-opt",
            root,
            "job-opt",
            string.Empty,
            "job-opt"));
        result.PointSet.Points.Add(CreatePoint(
            "point-frequency",
            root,
            "job-frequency",
            string.Empty,
            "job-frequency"));
        ScientificWorkflowInventoryService service = CreateService(
            inventory,
            new StubScientificRepository(new[] { result }));

        ScientificWorkflowInventory actual = await service.QueryAsync(
            new ScientificWorkflowInventoryQuery
            {
                RootWorkflowId = root
            });

        ScientificWorkflowResultAssociation association =
            Assert.Single(actual.Results);
        Assert.Equal(2, association.Points.Count);
        Assert.Contains(
            association.Points,
            point => point.PointId == "point-opt"
                && point.MatchesRootWorkflow);
        Assert.Contains(
            association.Points,
            point => point.PointId == "point-frequency"
                && point.MatchesRootWorkflow);
    }

    /// <summary>验证不存在的作业、文件和验证记录都会留下稳定问题。</summary>
    [Fact]
    public async Task ReportsMissingPointArtifactAndValidationJobs()
    {
        string root = "workflow-missing-jobs";
        CalculationWorkflowJobInventory inventory = CreateInventory(root);
        ScientificResult result = new ScientificResult();
        result.Id = "result-missing-jobs";
        CalculationPoint point = CreatePoint(
            "point-missing-jobs",
            root,
            "job-missing",
            string.Empty,
            string.Empty);
        point.Artifacts.Add(new PointArtifactReference
        {
            ArtifactId = "artifact-missing",
            CalculationJobId = "job-artifact-missing"
        });
        point.Validations.Add(new PointValidationRecord
        {
            Id = "validation-missing",
            JobId = "job-validation-missing"
        });
        result.PointSet.Points.Add(point);
        ScientificWorkflowInventoryService service = CreateService(
            inventory,
            new StubScientificRepository(new[] { result }));

        ScientificWorkflowInventory actual = await service.QueryAsync(
            new ScientificWorkflowInventoryQuery
            {
                RootWorkflowId = root
            });

        Assert.True(actual.HasErrors);
        Assert.Contains(
            actual.Issues,
            issue => issue.Code
                == ScientificWorkflowAssociationIssueCodes
                    .PointCalculationJobNotFound
                && issue.JobId == "job-missing");
        Assert.Contains(
            actual.Issues,
            issue => issue.Code
                == ScientificWorkflowAssociationIssueCodes
                    .ArtifactJobNotFound
                && issue.JobId == "job-artifact-missing");
        Assert.Contains(
            actual.Issues,
            issue => issue.Code
                == ScientificWorkflowAssociationIssueCodes
                    .ValidationJobNotFound
                && issue.JobId == "job-validation-missing");
    }

    /// <summary>验证科学点根标识冲突会阻止关联成为根工作流结果。</summary>
    [Fact]
    public async Task ReportsRootWorkflowMismatch()
    {
        string root = "workflow-root-match";
        CalculationWorkflowJobInventory inventory = CreateInventory(
            root,
            "job-root-match");
        ScientificResult result = new ScientificResult();
        result.Id = "result-root-mismatch";
        result.PointSet.Points.Add(CreatePoint(
            "point-root-mismatch",
            "workflow-other",
            "job-root-match",
            string.Empty,
            "job-root-match"));
        ScientificWorkflowInventoryService service = CreateService(
            inventory,
            new StubScientificRepository(new[] { result }));

        ScientificWorkflowInventory actual = await service.QueryAsync(
            new ScientificWorkflowInventoryQuery
            {
                RootWorkflowId = root,
                ResultId = result.Id
            });

        ScientificWorkflowResultAssociation association =
            Assert.Single(actual.Results);
        ScientificWorkflowPointAssociation point =
            Assert.Single(association.Points);
        Assert.False(point.MatchesRootWorkflow);
        Assert.False(association.MatchesRootWorkflow);
        Assert.Contains(
            actual.Issues,
            issue => issue.Code
                == ScientificWorkflowAssociationIssueCodes
                    .PointRootMismatch
                && issue.PointId == point.PointId);
    }

    /// <summary>验证不属于当前根工作流的科学成果不会进入结果集合。</summary>
    [Fact]
    public async Task ExcludesUnrelatedScientificResults()
    {
        string root = "workflow-unrelated";
        CalculationWorkflowJobInventory inventory = CreateInventory(
            root,
            "job-target");
        ScientificResult target = new ScientificResult();
        target.Id = "result-target";
        target.PointSet.Points.Add(CreatePoint(
            "point-target",
            root,
            "job-target",
            string.Empty,
            "job-target"));
        ScientificResult unrelated = new ScientificResult();
        unrelated.Id = "result-unrelated";
        unrelated.PointSet.Points.Add(CreatePoint(
            "point-unrelated",
            "workflow-other",
            "job-other",
            string.Empty,
            "job-other"));
        ScientificWorkflowInventoryService service = CreateService(
            inventory,
            new StubScientificRepository(
                new[] { target, unrelated }));

        ScientificWorkflowInventory actual = await service.QueryAsync(
            new ScientificWorkflowInventoryQuery
            {
                RootWorkflowId = root
            });

        ScientificWorkflowResultAssociation association =
            Assert.Single(actual.Results);
        Assert.Equal(target.Id, association.ResultId);
    }

    /// <summary>验证没有科学成果时明确报告缺失，而不是返回伪造成功。</summary>
    [Fact]
    public async Task ReportsMissingScientificResult()
    {
        string root = "workflow-no-scientific-result";
        ScientificWorkflowInventoryService service = CreateService(
            CreateInventory(root),
            new StubScientificRepository(Array.Empty<ScientificResult>()));

        ScientificWorkflowInventory actual = await service.QueryAsync(
            new ScientificWorkflowInventoryQuery
            {
                RootWorkflowId = root
            });

        Assert.Empty(actual.Results);
        Assert.True(actual.HasErrors);
        Assert.Contains(
            actual.Issues,
            issue => issue.Code
                == ScientificWorkflowAssociationIssueCodes.ResultNotFound);
    }

    /// <summary>验证展示筛选不会把被筛掉的来源作业误报为缺失。</summary>
    [Fact]
    public async Task UsesCompleteInventoryForAssociationAudit()
    {
        string root = "workflow-filtered-audit";
        CalculationWorkflowJobInventory inventory = CreateInventory(
            root,
            "job-optimization",
            "job-frequency");
        inventory.Jobs[0].TaskType =
            CalculationTaskType.Optimization;
        inventory.Jobs[1].TaskType = CalculationTaskType.Frequency;
        ScientificResult result = new ScientificResult();
        result.Id = "result-filtered-audit";
        result.PointSet.Points.Add(CreatePoint(
            "point-optimization",
            root,
            "job-optimization",
            string.Empty,
            "job-optimization"));
        ScientificWorkflowInventoryService service = CreateService(
            inventory,
            new StubScientificRepository(new[] { result }));
        ScientificWorkflowInventoryQuery query =
            new ScientificWorkflowInventoryQuery();
        query.RootWorkflowId = root;
        query.JobQuery.TaskTypes.Add(CalculationTaskType.Frequency);

        ScientificWorkflowInventory actual =
            await service.QueryAsync(query);

        Assert.Single(actual.JobInventory.Jobs);
        Assert.Equal(
            CalculationTaskType.Frequency,
            actual.JobInventory.Jobs[0].TaskType);
        Assert.Equal(2, actual.AuditJobInventory.Jobs.Count);
        Assert.DoesNotContain(
            actual.Issues,
            issue => issue.Code
                == ScientificWorkflowAssociationIssueCodes
                    .PointCalculationJobNotFound);
    }

    private static ScientificWorkflowInventoryService CreateService(
        CalculationWorkflowJobInventory inventory,
        IScientificDataRepository scientific)
    {
        return new ScientificWorkflowInventoryService(
            new StubCalculationInventoryRepository(inventory),
            scientific);
    }

    private static CalculationWorkflowJobInventory CreateInventory(
        string root,
        params string[] jobIds)
    {
        CalculationWorkflowJobInventory inventory =
            new CalculationWorkflowJobInventory();
        inventory.RootWorkflowId = root;

        for (int index = 0; index < jobIds.Length; index++)
        {
            CalculationWorkflowJobInventoryEntry entry =
                new CalculationWorkflowJobInventoryEntry();
            entry.JobId = jobIds[index];
            entry.IsIndexed = true;
            entry.StageId = CalculationWorkflowStageIds.Optimization;
            entry.State = CalculationJobState.Validated;
            entry.TaskType = CalculationTaskType.Optimization;
            entry.Program = "Gaussian";
            entry.ManifestStatus =
                CalculationWorkflowJobManifestStatus.Present;
            CalculationJob job = new CalculationJob();
            job.JobId = jobIds[index];
            job.RootWorkflowId = root;
            job.Spec.TaskType = CalculationTaskType.Optimization;
            job.Spec.Program = "Gaussian";
            entry.Job = job;
            inventory.Jobs.Add(entry);
        }

        return inventory;
    }

    private static CalculationPoint CreatePoint(
        string pointId,
        string root,
        string calculationJobId,
        string initialJobId,
        string acceptedJobId)
    {
        CalculationPoint point = new CalculationPoint();
        point.Id = pointId;
        point.CalculationJobId = calculationJobId;
        point.Provenance.RootWorkflowId = root;
        point.Provenance.InitialJobId = initialJobId;
        point.Provenance.AcceptedJobId = acceptedJobId;
        return point;
    }

    private sealed class StubCalculationInventoryRepository
        : ICalculationWorkflowInventoryRepository
    {
        private readonly CalculationWorkflowJobInventory _inventory;

        public StubCalculationInventoryRepository(
            CalculationWorkflowJobInventory inventory)
        {
            _inventory = inventory;
        }

        public Task<CalculationWorkflowJobInventory> QueryAsync(
            CalculationWorkflowJobInventoryQuery query,
            CancellationToken cancellationToken = default)
        {
            CalculationWorkflowJobInventory filtered =
                new CalculationWorkflowJobInventory();
            filtered.RootWorkflowId = _inventory.RootWorkflowId;
            filtered.Ledger = _inventory.Ledger;
            filtered.Issues.AddRange(_inventory.Issues);

            for (int index = 0; index < _inventory.Jobs.Count; index++)
            {
                CalculationWorkflowJobInventoryEntry entry =
                    _inventory.Jobs[index];

                if (query.TaskTypes.Count > 0
                    && !query.TaskTypes.Contains(entry.TaskType))
                {
                    continue;
                }

                if (query.States.Count > 0
                    && !query.States.Contains(entry.State))
                {
                    continue;
                }

                if (query.StageIds.Count > 0
                    && !query.StageIds.Exists(
                        stageId => string.Equals(
                            stageId,
                            entry.StageId,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                filtered.Jobs.Add(entry);
            }

            return Task.FromResult(filtered);
        }
    }

    private sealed class StubScientificRepository
        : IScientificDataRepository
    {
        private readonly List<ScientificResult> _results;

        public StubScientificRepository(
            IEnumerable<ScientificResult> results)
        {
            _results = new List<ScientificResult>(results);
        }

        public Task SaveAsync(
            ScientificResult result,
            CancellationToken cancellationToken = default)
        {
            _results.Add(result);
            return Task.CompletedTask;
        }

        public Task<ScientificResult?> GetAsync(
            string resultId,
            CancellationToken cancellationToken = default)
        {
            ScientificResult? result = _results.Find(
                candidate => candidate.Id == resultId);
            return Task.FromResult(result);
        }

        public IReadOnlyList<ScientificResult> List()
        {
            return _results;
        }
    }
}
