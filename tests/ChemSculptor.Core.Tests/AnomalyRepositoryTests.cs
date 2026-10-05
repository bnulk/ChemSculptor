using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>异常处理记录存储测试。</summary>
public class AnomalyRepositoryTests
{
    /// <summary>验证异常记录按作业保存并可以读取。</summary>
    [Fact]
    public async Task SavesAndReadsAnomalyRecord()
    {
        string root = CreateTemporaryRoot();
        string jobId = "job-anomaly-001";
        string recordId = "anomaly-001";

        try
        {
            CalculationWorkspaceOptions options =
                new CalculationWorkspaceOptions();
            options.RootDirectory = root;

            WorkspaceManager workspace = new WorkspaceManager(options);
            await workspace.EnsureJobWorkspaceAsync(jobId);
            FileAnomalyRepository repository =
                new FileAnomalyRepository(workspace);

            AnomalyRecord record = new AnomalyRecord();
            record.Id = recordId;
            record.JobId = jobId;
            record.WorkflowId = "workflow-001";

            AnomalyCheckResult check = new AnomalyCheckResult();
            check.Code = CommonAnomalyCheckCodes.WavefunctionStability;
            check.DisplayName = "波函数稳定性检查";
            check.Category = AnomalyCategory.Scientific;
            check.Status = AnomalyCheckStatus.Skipped;
            check.SkippedReason = "当前计算模型不支持波函数稳定性检查。";
            record.Checks.Add(check);

            await repository.SaveAsync(record);

            string path = AnomalyStoragePaths.GetRecordPath(
                workspace,
                jobId,
                recordId);

            Assert.True(File.Exists(path));

            AnomalyRecord? loaded =
                await repository.GetAsync(jobId, recordId);

            Assert.NotNull(loaded);
            Assert.Equal(jobId, loaded.JobId);
            Assert.Single(loaded.Checks);
            Assert.Equal(
                AnomalyCheckStatus.Skipped,
                loaded.Checks[0].Status);
            Assert.Contains(
                "不支持",
                loaded.Checks[0].SkippedReason);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证可以列出指定作业的全部异常记录。</summary>
    [Fact]
    public async Task ListsAllRecordsForJob()
    {
        string root = CreateTemporaryRoot();
        string jobId = "job-anomaly-002";

        try
        {
            CalculationWorkspaceOptions options =
                new CalculationWorkspaceOptions();
            options.RootDirectory = root;

            WorkspaceManager workspace = new WorkspaceManager(options);
            await workspace.EnsureJobWorkspaceAsync(jobId);
            FileAnomalyRepository repository =
                new FileAnomalyRepository(workspace);

            AnomalyRecord first = new AnomalyRecord();
            first.Id = "anomaly-002-a";
            first.JobId = jobId;

            AnomalyRecord second = new AnomalyRecord();
            second.Id = "anomaly-002-b";
            second.JobId = jobId;

            await repository.SaveAsync(first);
            await repository.SaveAsync(second);

            IReadOnlyList<AnomalyRecord> records =
                await repository.ListByJobAsync(jobId);

            Assert.Equal(2, records.Count);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证非法记录标识不会形成路径穿越。</summary>
    [Fact]
    public void RejectsInvalidRecordIdentifier()
    {
        string root = CreateTemporaryRoot();
        string jobId = "job-anomaly-003";

        try
        {
            CalculationWorkspaceOptions options =
                new CalculationWorkspaceOptions();
            options.RootDirectory = root;

            WorkspaceManager workspace = new WorkspaceManager(options);
            bool exceptionThrown = false;

            try
            {
                AnomalyStoragePaths.GetRecordPath(
                    workspace,
                    jobId,
                    "../outside");
            }
            catch (ArgumentException)
            {
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTemporaryRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
