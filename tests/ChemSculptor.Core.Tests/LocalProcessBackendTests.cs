using ChemSculptor.Compute;
using ChemSculptor.Compute.Local;

namespace ChemSculptor.Core.Tests;

/// <summary>
/// 本机进程执行后端测试。
/// </summary>
public class LocalProcessBackendTests
{
    /// <summary>验证后端可以启动进程、捕获输出并标记完成。</summary>
    [Fact]
    public async Task RunsLocalProcessAndCapturesOutput()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));

        string runDirectory = Path.Combine(root, "run");
        string artifactsDirectory = Path.Combine(root, "artifacts");
        Directory.CreateDirectory(runDirectory);

        try
        {
            LocalProcessBackendOptions options = LocalProcessBackendOptions.CreateDefault();
            LocalProcessBackend backend = new LocalProcessBackend(options);

            CalculationJob job = new CalculationJob();
            job.JobId = "job-local-test";

            CalculationExecutionContext context = new CalculationExecutionContext();
            context.RunDirectory = runDirectory;
            context.ExecutablePath = "powershell.exe";
            context.OutputFilePath = Path.Combine(runDirectory, "output.log");
            context.EnvironmentVariables["CHEMSCULPTOR_TEST_ENV"] = "environment-ok";
            context.Arguments = new List<string>();
            context.Arguments.Add("-NoProfile");
            context.Arguments.Add("-Command");
            context.Arguments.Add(
                "Write-Output $env:CHEMSCULPTOR_TEST_ENV; " +
                "Write-Output 'Normal termination'; " +
                "Write-Output 'SCF Done'");

            await backend.SubmitAsync(job, context);
            CalculationJobState status = await WaitForCompletionAsync(backend, job);

            Assert.Equal(CalculationJobState.Completed, status);

            string standardOutputPath = Path.Combine(runDirectory, "stdout.log");
            Assert.True(File.Exists(standardOutputPath));

            string standardOutput = await File.ReadAllTextAsync(standardOutputPath);
            Assert.Contains("environment-ok", standardOutput);
            Assert.Contains("Normal termination", standardOutput);

            await backend.FetchArtifactsAsync(job, artifactsDirectory);
            Assert.True(File.Exists(Path.Combine(artifactsDirectory, "stdout.log")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private static async Task<CalculationJobState> WaitForCompletionAsync(
        LocalProcessBackend backend,
        CalculationJob job)
    {
        CalculationJobState status = await backend.GetStatusAsync(job);

        for (int attempt = 0; attempt < 100; attempt++)
        {
            if (status != CalculationJobState.Queued
                && status != CalculationJobState.Running)
            {
                return status;
            }

            await Task.Delay(100);
            status = await backend.GetStatusAsync(job);
        }

        return status;
    }
}
