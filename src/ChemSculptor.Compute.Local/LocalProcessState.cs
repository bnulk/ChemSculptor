using System.Diagnostics;
using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Local;

/// <summary>
/// 单个本机进程的运行状态。
/// </summary>
internal sealed class LocalProcessState
{
    public string JobId { get; set; } = string.Empty;

    public Process? Process { get; set; }

    public Task? MonitorTask { get; set; }

    public CalculationJobState Status { get; set; } = CalculationJobState.Created;

    public string StandardOutputPath { get; set; } = string.Empty;

    public string StandardErrorPath { get; set; } = string.Empty;

    public string OutputFilePath { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public int? ExitCode { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }
}
