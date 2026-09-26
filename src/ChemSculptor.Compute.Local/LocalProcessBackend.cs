using System.Collections.Concurrent;
using System.Diagnostics;
using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Local;

/// <summary>
/// 本机进程执行后端。
/// 负责启动本机程序、捕获标准输出和标准错误，并跟踪运行状态。
/// </summary>
public sealed class LocalProcessBackend : IComputeBackend
{
    private readonly LocalProcessBackendOptions _options;
    private readonly ConcurrentDictionary<string, LocalProcessState> _states =
        new ConcurrentDictionary<string, LocalProcessState>(StringComparer.OrdinalIgnoreCase);

    /// <summary>创建本机进程后端。</summary>
    public LocalProcessBackend(LocalProcessBackendOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _options = options;
    }

    /// <summary>后端名称。</summary>
    public string Name
    {
        get { return "local"; }
    }

    /// <summary>启动本机进程并异步监控。</summary>
    public Task<string> SubmitAsync(
        CalculationJob job,
        CalculationExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        string executablePath = context.ExecutablePath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            executablePath = _options.ExecutablePath;
        }

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("未配置本机执行程序路径。");
        }

        if (string.IsNullOrWhiteSpace(context.RunDirectory))
        {
            throw new InvalidOperationException("运行目录不能为空。");
        }

        Directory.CreateDirectory(context.RunDirectory);

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = executablePath;
        startInfo.WorkingDirectory = context.RunDirectory;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;

        for (int index = 0; index < context.Arguments.Count; index++)
        {
            startInfo.ArgumentList.Add(context.Arguments[index]);
        }

        foreach (KeyValuePair<string, string> environmentVariable in context.EnvironmentVariables)
        {
            startInfo.Environment[environmentVariable.Key] = environmentVariable.Value;
        }

        if (context.Arguments.Count == 0 && !string.IsNullOrWhiteSpace(context.InputFilePath))
        {
            startInfo.ArgumentList.Add(context.InputFilePath);
        }

        Process process = new Process();
        process.StartInfo = startInfo;

        LocalProcessState state = new LocalProcessState();
        state.JobId = job.JobId;
        state.Process = process;
        state.Status = CalculationJobState.Queued;
        state.StandardOutputPath = Path.Combine(
            context.RunDirectory,
            _options.StandardOutputFileName);
        state.StandardErrorPath = Path.Combine(
            context.RunDirectory,
            _options.StandardErrorFileName);
        state.OutputFilePath = context.OutputFilePath;
        state.StartedAt = DateTimeOffset.UtcNow;

        _states[job.JobId] = state;

        process.Start();
        state.Status = CalculationJobState.Running;

        Task<string> standardOutputTask =
            process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardErrorTask =
            process.StandardError.ReadToEndAsync(cancellationToken);

        state.MonitorTask = MonitorProcessAsync(
            state,
            standardOutputTask,
            standardErrorTask,
            cancellationToken);

        return Task.FromResult(job.JobId);
    }

    /// <summary>查询本机进程状态。</summary>
    public Task<CalculationJobState> GetStatusAsync(
        CalculationJob job,
        CancellationToken cancellationToken = default)
    {
        LocalProcessState? state;
        if (!_states.TryGetValue(job.JobId, out state))
        {
            return Task.FromResult(CalculationJobState.Failed);
        }

        return Task.FromResult(state.Status);
    }

    /// <summary>把运行目录中的产物复制到指定目录。</summary>
    public Task FetchArtifactsAsync(
        CalculationJob job,
        string localDirectory,
        CancellationToken cancellationToken = default)
    {
        LocalProcessState? state;
        if (!_states.TryGetValue(job.JobId, out state))
        {
            return Task.CompletedTask;
        }

        Directory.CreateDirectory(localDirectory);

        CopyFileIfExists(state.StandardOutputPath, localDirectory);
        CopyFileIfExists(state.StandardErrorPath, localDirectory);
        CopyFileIfExists(state.OutputFilePath, localDirectory);

        return Task.CompletedTask;
    }

    /// <summary>取消运行中的本机进程。</summary>
    public Task CancelAsync(
        CalculationJob job,
        CancellationToken cancellationToken = default)
    {
        LocalProcessState? state;
        if (!_states.TryGetValue(job.JobId, out state))
        {
            return Task.CompletedTask;
        }

        if (state.Process != null && !state.Process.HasExited)
        {
            state.Process.Kill(_options.KillProcessTreeOnCancel);
        }

        state.Status = CalculationJobState.Canceled;
        state.CompletedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    private async Task MonitorProcessAsync(
        LocalProcessState state,
        Task<string> standardOutputTask,
        Task<string> standardErrorTask,
        CancellationToken cancellationToken)
    {
        try
        {
            if (state.Process == null)
            {
                state.Status = CalculationJobState.Failed;
                state.ErrorMessage = "进程对象为空。";
                return;
            }

            await state.Process.WaitForExitAsync(cancellationToken);

            string standardOutput = await standardOutputTask;
            string standardError = await standardErrorTask;

            await File.WriteAllTextAsync(
                state.StandardOutputPath,
                standardOutput,
                CancellationToken.None);
            await File.WriteAllTextAsync(
                state.StandardErrorPath,
                standardError,
                CancellationToken.None);

            state.ExitCode = state.Process.ExitCode;
            state.CompletedAt = DateTimeOffset.UtcNow;

            if (state.ExitCode == 0)
            {
                state.Status = CalculationJobState.Completed;
            }
            else
            {
                state.Status = CalculationJobState.Failed;
                state.ErrorMessage = "进程退出码：" + state.ExitCode.Value.ToString();
            }
        }
        catch (OperationCanceledException)
        {
            state.Status = CalculationJobState.Canceled;
            state.CompletedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            state.Status = CalculationJobState.Failed;
            state.ErrorMessage = ex.Message;
            state.CompletedAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            if (state.Process != null)
            {
                state.Process.Dispose();
            }
        }
    }

    private static void CopyFileIfExists(string sourcePath, string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return;
        }

        string targetPath = Path.Combine(targetDirectory, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, targetPath, true);
    }
}
