using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;

namespace ChemSculptor.Core.Tests;

/// <summary>计算作业监控与结果保存测试。</summary>
public class CalculationJobMonitorTests
{
    /// <summary>验证进程结束后会解析输出、保存结果并更新作业状态。</summary>
    [Fact]
    public async Task ParsesOutputAndSavesResultAfterCompletion()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(root);

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = root;

            WorkspaceManager workspace = new WorkspaceManager(workspaceOptions);
            FileCalculationRepository repository =
                new FileCalculationRepository(workspace);

            CalculationJob job = new CalculationJob();
            job.JobId = "job-monitor-test";
            job.Spec = CalculationDefaults.CreateDefaultSinglePoint();
            job.InputFilePath = Path.Combine(
                workspace.GetRunDirectory(job.JobId),
                job.JobId + ".gjf");
            job.OutputFilePath = workspace.GetJobOutputPath(job.JobId);

            await workspace.EnsureJobWorkspaceAsync(job.JobId);
            await File.WriteAllTextAsync(
                job.OutputFilePath,
                " SCF Done:  E(RCAM-B3LYP) =  -76.3801014     A.U. after    8 cycles\n" +
                " Normal termination of Gaussian 16\n");

            GaussianInputWriter inputWriter = new GaussianInputWriter();
            GaussianOutputParser outputParser = new GaussianOutputParser();
            Gaussian16ProgramAdapterOptions adapterOptions =
                Gaussian16ProgramAdapterOptions.CreateDefault();
            Gaussian16ProgramAdapter programAdapter =
                new Gaussian16ProgramAdapter(
                    inputWriter,
                    outputParser,
                    adapterOptions);

            CompletedComputeBackend backend = new CompletedComputeBackend();
            CalculationJobMonitorOptions monitorOptions =
                new CalculationJobMonitorOptions();
            monitorOptions.PollingIntervalMilliseconds = 10;

            CalculationJobMonitor monitor = new CalculationJobMonitor(
                backend,
                programAdapter,
                repository,
                monitorOptions);

            monitor.Start(job);

            CalculationResult? savedResult = null;
            CalculationJob? savedJob = null;

            for (int attempt = 0; attempt < 100; attempt++)
            {
                savedResult = await repository.GetResultAsync(job.JobId);
                savedJob = await repository.GetJobAsync(job.JobId);

                if (savedResult != null
                    && savedJob != null
                    && savedJob.State == CalculationJobState.Parsed)
                {
                    break;
                }

                await Task.Delay(20);
            }

            Assert.NotNull(savedResult);
            Assert.NotNull(savedResult.Energy);
            Assert.Equal(-76.3801014, savedResult.Energy.Value, 7);
            Assert.True(savedResult.NormalTermination);

            Assert.NotNull(savedJob);
            Assert.Equal(CalculationJobState.Parsed, savedJob.State);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private sealed class CompletedComputeBackend : IComputeBackend
    {
        public string Name
        {
            get { return "completed-test"; }
        }

        public Task<string> SubmitAsync(
            CalculationJob job,
            CalculationExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(job.JobId);
        }

        public Task<CalculationJobState> GetStatusAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CalculationJobState.Completed);
        }

        public Task FetchArtifactsAsync(
            CalculationJob job,
            string localDirectory,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task CancelAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
