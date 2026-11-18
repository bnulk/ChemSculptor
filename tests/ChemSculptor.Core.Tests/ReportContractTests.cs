using ChemSculptor.Compute;
using ChemSculptor.Reporting;
using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.Core.Tests;

/// <summary>报告成果包第 0 步契约测试。</summary>
public class ReportContractTests
{
    /// <summary>四个主契约必须能够以统一 JSON 选项往返。</summary>
    [Fact]
    public void ReportPackageContractsRoundTripThroughJson()
    {
        ReportPackageRequest request = CreateRequest();
        ReportEvidenceSnapshot snapshot = CreateSnapshot(request);
        ReportDocument document = CreateDocument(snapshot);
        ReportPackageManifest manifest = CreateManifest(document);

        ReportPackageRequest requestRoundTrip =
            RoundTrip(request);
        ReportEvidenceSnapshot snapshotRoundTrip =
            RoundTrip(snapshot);
        ReportDocument documentRoundTrip =
            RoundTrip(document);
        ReportPackageManifest manifestRoundTrip =
            RoundTrip(manifest);

        Assert.Equal(request.ResultId, requestRoundTrip.ResultId);
        Assert.Equal(
            ReportPackageProfile.Archive,
            requestRoundTrip.Profile);

        Assert.Equal(snapshot.SnapshotId, snapshotRoundTrip.SnapshotId);
        Assert.NotNull(snapshotRoundTrip.ScientificResult);

        ScientificResult? roundTripResult =
            snapshotRoundTrip.ScientificResult;
        Assert.NotNull(roundTripResult);
        Assert.Equal(
            "point-1",
            roundTripResult.PointSet.Points[0].Id);
        Assert.Equal(
            ReportEvidenceCompleteness.Partial,
            snapshotRoundTrip.Completeness);

        CalculationJob? roundTripJob =
            snapshotRoundTrip.Jobs[0].Job;
        Assert.NotNull(roundTripJob);
        Assert.Equal(
            "job-1",
            roundTripJob.JobId);

        Assert.Equal(document.DocumentId, documentRoundTrip.DocumentId);
        Assert.Single(documentRoundTrip.Sections);
        Assert.Equal(
            ReportContentBlockKind.Table,
            documentRoundTrip.Sections[0].Blocks[0].Kind);
        Assert.Equal(
            "1.0000",
            documentRoundTrip.Sections[0].Blocks[0].Rows[0].Cells[0]);

        Assert.Equal(manifest.PackageId, manifestRoundTrip.PackageId);
        Assert.Equal(
            ReportPackageStatus.Complete,
            manifestRoundTrip.Status);
        Assert.Single(manifestRoundTrip.Files);
        Assert.Equal(
            ReportPackageFileRole.ReportMarkdown,
            manifestRoundTrip.Files[0].Role);
    }

    /// <summary>通用报告文档不能要求特定的吸收光谱或 NTO 类型。</summary>
    [Fact]
    public void ReportDocumentRepresentsGenericWorkflowWithoutDomainSpecificTypes()
    {
        ReportEvidenceSnapshot snapshot =
            CreateSnapshot(CreateRequest());
        ReportDocument document = CreateDocument(snapshot);

        string json = ReportContractSerializer.Serialize(document);

        Assert.DoesNotContain(
            "absorption",
            json,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "nto",
            json,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "\"kind\": \"Table\"",
            json,
            StringComparison.Ordinal);
    }

    /// <summary>证据缺失必须能被显式表达，而不是由报告层静默补全。</summary>
    [Fact]
    public void EvidenceSnapshotCanDescribeMissingEvidence()
    {
        ReportPackageRequest request = CreateRequest();
        ReportEvidenceSnapshot snapshot = CreateSnapshot(request);
        snapshot.ScientificResult = null;
        snapshot.Completeness =
            ReportEvidenceCompleteness.Insufficient;
        snapshot.Issues.Add(
            CreateIssue(
                "evidence.scientific-result.missing",
                true,
                "没有找到科学成果。"));

        string json = ReportContractSerializer.Serialize(snapshot);
        ReportEvidenceSnapshot? roundTrip =
            ReportContractSerializer.Deserialize<ReportEvidenceSnapshot>(json);

        Assert.NotNull(roundTrip);
        Assert.Null(roundTrip.ScientificResult);
        Assert.Equal(
            ReportEvidenceCompleteness.Insufficient,
            roundTrip.Completeness);

        ReportEvidenceIssue? blockingIssue =
            FindIssue(
                roundTrip,
                "evidence.scientific-result.missing");
        Assert.NotNull(blockingIssue);
        Assert.True(blockingIssue.IsBlocking);
    }

    /// <summary>成果包清单只记录最终文件，并携带文件角色和完整性信息。</summary>
    [Fact]
    public void ManifestDescribesFinalPackageFilesAndChecksums()
    {
        ReportDocument document = CreateDocument(
            CreateSnapshot(CreateRequest()));
        ReportPackageManifest manifest = CreateManifest(document);

        Assert.Equal("checksums.sha256", manifest.ChecksumManifestPath);
        Assert.Equal(2048, manifest.TotalBytes);
        Assert.True(manifest.Files[0].IsRequired);
        Assert.Equal(2048, manifest.Files[0].Length);
        Assert.Equal(64, manifest.Files[0].Sha256.Length);
        Assert.Equal(
            ReportEvidenceCompleteness.Partial,
            manifest.Completeness);
    }

    /// <summary>统一序列化入口必须固定模式版本和枚举文本。</summary>
    [Fact]
    public void SerializerUsesFixedSchemaVersionAndStringEnums()
    {
        ReportPackageRequest request = CreateRequest();
        string json = ReportContractSerializer.Serialize(request);

        Assert.Contains(
            "\"schemaVersion\": \"1.0\"",
            json,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"profile\": \"Archive\"",
            json,
            StringComparison.Ordinal);
    }

    /// <summary>科学点包和全过程档案包必须能够共用范围契约往返。</summary>
    [Fact]
    public void PackageScopesRoundTripWithDistinctBoundaries()
    {
        ReportPackageScope pointScope =
            ReportPackageScope.CreateScientificPointArtifact(
                "scientific-result-1");
        pointScope.CapturedAt =
            new DateTimeOffset(
                2026,
                11,
                18,
                9,
                30,
                0,
                TimeSpan.FromHours(8));
        pointScope.CalculationConclusion =
            ReportCalculationConclusionStatus.Succeeded;

        ReportPackageScope archiveScope =
            ReportPackageScope.CreateCalculationRunArchive(
                "workflow-1",
                "scientific-result-1");
        archiveScope.CalculationConclusion =
            ReportCalculationConclusionStatus.Failed;

        ReportPackageScope pointRoundTrip = RoundTrip(pointScope);
        ReportPackageScope archiveRoundTrip = RoundTrip(archiveScope);

        Assert.Equal(
            ReportPackageScopeKind.ScientificPointArtifact,
            pointRoundTrip.Kind);
        Assert.True(pointRoundTrip.IncludeSciencePointSet);
        Assert.False(pointRoundTrip.IncludeExecutionEvidence);
        Assert.Equal(
            ReportCalculationConclusionStatus.Succeeded,
            pointRoundTrip.CalculationConclusion);

        Assert.Equal(
            ReportPackageScopeKind.CalculationRunArchive,
            archiveRoundTrip.Kind);
        Assert.Equal(
            "workflow-1",
            archiveRoundTrip.RootWorkflowId);
        Assert.True(archiveRoundTrip.IncludeExecutionEvidence);
        Assert.True(archiveRoundTrip.IncludeExcludedArtifacts);
        Assert.Equal("science", archiveRoundTrip.ScienceDirectory);
        Assert.Equal("process", archiveRoundTrip.ProcessDirectory);
    }

    /// <summary>科学点包不能偷偷携带全过程证据。</summary>
    [Fact]
    public void ScientificPointScopeRejectsExecutionEvidence()
    {
        ReportPackageScope scope =
            ReportPackageScope.CreateScientificPointArtifact(
                "scientific-result-1");
        scope.IncludeExecutionEvidence = true;

        ReportPackageScopeValidationResult validation =
            ReportPackageScopeValidator.Validate(scope);

        Assert.False(validation.IsValid);
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.process_evidence.included");
    }

    /// <summary>全过程档案必须同时保留科学点、执行证据和排除项。</summary>
    [Fact]
    public void ArchiveScopeRequiresScienceAndProcessEvidence()
    {
        ReportPackageScope scope =
            ReportPackageScope.CreateCalculationRunArchive(
                "workflow-1",
                "scientific-result-1");
        scope.IncludeSciencePointSet = false;
        scope.IncludeExecutionEvidence = false;
        scope.IncludeExcludedArtifacts = false;

        ReportPackageScopeValidationResult validation =
            ReportPackageScopeValidator.Validate(
                scope,
                ReportPackageStatus.Draft);

        Assert.False(validation.IsValid);
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.science_point_set.not_included");
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.execution_evidence.not_included");
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.excluded_artifacts.not_included");
    }

    /// <summary>运行中的根工作流不能生成标记为 Complete 的完整档案。</summary>
    [Fact]
    public void InProgressArchiveCannotBeMarkedComplete()
    {
        ReportPackageScope scope =
            ReportPackageScope.CreateCalculationRunArchive(
                "workflow-1",
                "scientific-result-1");
        scope.CalculationConclusion =
            ReportCalculationConclusionStatus.InProgress;

        ReportPackageScopeValidationResult validation =
            ReportPackageScopeValidator.Validate(
                scope,
                ReportPackageStatus.Complete);

        Assert.False(validation.IsValid);
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.in_progress.cannot_be_complete");
    }

    /// <summary>结论未知时不能声称全过程档案完整。</summary>
    [Fact]
    public void UnknownConclusionArchiveCannotBeMarkedComplete()
    {
        ReportPackageScope scope =
            ReportPackageScope.CreateCalculationRunArchive(
                "workflow-1",
                "scientific-result-1");
        scope.CalculationConclusion =
            ReportCalculationConclusionStatus.Unknown;

        ReportPackageScopeValidationResult validation =
            ReportPackageScopeValidator.Validate(
                scope,
                ReportPackageStatus.Complete);

        Assert.False(validation.IsValid);
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.unknown_conclusion.cannot_be_complete");
    }

    /// <summary>计算失败不妨碍全过程档案在证据完整时标记 Complete。</summary>
    [Fact]
    public void FailedCalculationCanHaveCompleteArchiveEvidence()
    {
        ReportPackageScope scope =
            ReportPackageScope.CreateCalculationRunArchive(
                "workflow-1",
                "scientific-result-1");
        scope.CalculationConclusion =
            ReportCalculationConclusionStatus.Failed;

        ReportPackageScopeValidationResult validation =
            ReportPackageScopeValidator.Validate(
                scope,
                ReportPackageStatus.Complete);

        Assert.True(validation.IsValid);
    }

    /// <summary>范围标识必须与快照和清单的外部标识一致。</summary>
    [Fact]
    public void ScopeIdentityMustMatchPackageIdentity()
    {
        ReportPackageScope scope =
            ReportPackageScope.CreateCalculationRunArchive(
                "workflow-1",
                "scientific-result-1");

        ReportPackageScopeValidationResult validation =
            ReportPackageScopeValidator.Validate(
                scope,
                ReportPackageStatus.Complete,
                "scientific-result-2",
                "workflow-2");

        Assert.False(validation.IsValid);
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.result_id.mismatch");
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.root_workflow_id.mismatch");
    }

    /// <summary>范围目录不得使用绝对路径或目录穿越。</summary>
    [Fact]
    public void ArchiveScopeRejectsUnsafeDirectories()
    {
        ReportPackageScope scope =
            ReportPackageScope.CreateCalculationRunArchive(
                "workflow-1",
                "scientific-result-1");
        scope.ScienceDirectory = "../outside";
        scope.ProcessDirectory = "C:\\server\\jobs";

        ReportPackageScopeValidationResult validation =
            ReportPackageScopeValidator.Validate(scope);

        Assert.False(validation.IsValid);
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.science_directory.invalid");
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.process_directory.invalid");
    }

    /// <summary>科学点和过程证据不能写入同一个逻辑目录。</summary>
    [Fact]
    public void ArchiveScopeRequiresDistinctDirectoryNames()
    {
        ReportPackageScope scope =
            ReportPackageScope.CreateCalculationRunArchive(
                "workflow-1",
                "scientific-result-1");
        scope.ProcessDirectory = scope.ScienceDirectory;

        ReportPackageScopeValidationResult validation =
            ReportPackageScopeValidator.Validate(scope);

        Assert.False(validation.IsValid);
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.directories.not_distinct");
    }

    /// <summary>包内逻辑目录必须是规范相对路径，不能混入文件名片段。</summary>
    [Theory]
    [InlineData("science/../process")]
    [InlineData("./science")]
    [InlineData("science/")]
    [InlineData("science//points")]
    public void ArchiveScopeRejectsNonCanonicalDirectories(string directory)
    {
        ReportPackageScope scope =
            ReportPackageScope.CreateCalculationRunArchive(
                "workflow-1",
                "scientific-result-1");
        scope.ScienceDirectory = directory;

        ReportPackageScopeValidationResult validation =
            ReportPackageScopeValidator.Validate(scope);

        Assert.False(validation.IsValid);
        Assert.Contains(
            validation.Issues,
            issue => issue.Code
                == "report.scope.science_directory.invalid");
    }

    private static T RoundTrip<T>(T value)
    {
        string json = ReportContractSerializer.Serialize(value);
        T? roundTrip = ReportContractSerializer.Deserialize<T>(json);

        if (roundTrip == null)
        {
            throw new InvalidOperationException("JSON 往返结果为空。");
        }

        return roundTrip;
    }

    private static ReportPackageRequest CreateRequest()
    {
        ReportPackageRequest request = new ReportPackageRequest();
        request.RequestId = "request-1";
        request.ResultId = "scientific-result-1";
        request.Scope =
            ReportPackageScope.CreateScientificPointArtifact(
                request.ResultId);
        request.Profile = ReportPackageProfile.Archive;
        request.RequestedAt =
            new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.FromHours(8));
        request.RequestedSectionIds.Add("overview");
        request.ClientMetadata["source"] = "winforms";
        return request;
    }

    private static ReportEvidenceSnapshot CreateSnapshot(
        ReportPackageRequest request)
    {
        ScientificResult result = new ScientificResult();
        result.Id = request.ResultId;
        result.Title = "通用计算成果";
        result.Status = ScientificResultStatus.Partial;

        CalculationPoint point = new CalculationPoint();
        point.Id = "point-1";
        point.Name = "接受点";
        point.Status = CalculationPointStatus.Accepted;
        point.CalculationJobId = "job-1";
        result.PointSet.Points.Add(point);

        CalculationJob job = new CalculationJob();
        job.JobId = "job-1";
        job.RootWorkflowId = "job-1";
        job.Goal = "通用计算";

        ReportJobEvidence jobEvidence = new ReportJobEvidence();
        jobEvidence.Relationship = "accepted";
        jobEvidence.Job = job;

        ReportEvidenceSnapshot snapshot = new ReportEvidenceSnapshot();
        snapshot.SnapshotId = "snapshot-1";
        snapshot.RequestId = request.RequestId;
        snapshot.ResultId = request.ResultId;
        snapshot.RootWorkflowId = "job-1";
        snapshot.Scope = request.Scope;
        snapshot.Request = request;
        snapshot.ScientificResult = result;
        snapshot.Jobs.Add(jobEvidence);
        snapshot.Completeness = ReportEvidenceCompleteness.Partial;
        snapshot.SnapshotSha256 = new string('a', 64);
        snapshot.Issues.Add(
            CreateIssue(
                "evidence.validation.missing",
                false,
                "没有找到验证记录。"));
        return snapshot;
    }

    private static ReportDocument CreateDocument(
        ReportEvidenceSnapshot snapshot)
    {
        ReportContentBlock table = new ReportContentBlock();
        table.Id = "properties";
        table.Kind = ReportContentBlockKind.Table;
        table.Title = "性质";
        table.Columns.Add("数值");

        ReportDataRow row = new ReportDataRow();
        row.Cells.Add("1.0000");
        table.Rows.Add(row);

        ReportSection section = new ReportSection();
        section.Id = "overview";
        section.Title = "成果概况";
        section.Order = 1;
        section.ProviderId = "core.overview";
        section.Blocks.Add(table);

        ReportDocument document = new ReportDocument();
        document.DocumentId = "document-1";
        document.RequestId = snapshot.RequestId;
        document.ResultId = snapshot.ResultId;
        document.Scope = snapshot.Scope;
        document.SnapshotId = snapshot.SnapshotId;
        document.SnapshotSha256 = snapshot.SnapshotSha256;
        document.Title = "通用计算成果报告";
        document.Profile = snapshot.Request.Profile;
        document.Summary = "只从冻结证据生成的通用报告。";
        document.Completeness = snapshot.Completeness;
        document.Sections.Add(section);
        return document;
    }

    private static ReportPackageManifest CreateManifest(
        ReportDocument document)
    {
        ReportPackageFile file = new ReportPackageFile();
        file.Path = "report.md";
        file.Role = ReportPackageFileRole.ReportMarkdown;
        file.MediaType = "text/markdown";
        file.Length = 2048;
        file.Sha256 = new string('b', 64);
        file.IsRequired = true;

        ReportPackageManifest manifest = new ReportPackageManifest();
        manifest.PackageId = "package-1";
        manifest.RequestId = document.RequestId;
        manifest.ResultId = document.ResultId;
        manifest.Scope = document.Scope;
        manifest.SnapshotId = document.SnapshotId;
        manifest.ReportDocumentId = document.DocumentId;
        manifest.Status = ReportPackageStatus.Complete;
        manifest.Completeness = document.Completeness;
        manifest.TotalBytes = 2048;
        manifest.Files.Add(file);
        return manifest;
    }

    private static ReportEvidenceIssue CreateIssue(
        string code,
        bool isBlocking,
        string message)
    {
        ReportEvidenceIssue issue = new ReportEvidenceIssue();
        issue.Code = code;

        if (isBlocking)
        {
            issue.Severity = ReportEvidenceIssueSeverity.Error;
        }
        else
        {
            issue.Severity = ReportEvidenceIssueSeverity.Warning;
        }

        issue.ReferenceId = "job-1";
        issue.Message = message;
        issue.IsBlocking = isBlocking;
        return issue;
    }

    private static ReportEvidenceIssue? FindIssue(
        ReportEvidenceSnapshot snapshot,
        string code)
    {
        for (int index = 0; index < snapshot.Issues.Count; index++)
        {
            ReportEvidenceIssue issue = snapshot.Issues[index];

            if (string.Equals(
                issue.Code,
                code,
                StringComparison.Ordinal))
            {
                return issue;
            }
        }

        return null;
    }
}
