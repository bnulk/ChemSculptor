namespace ChemSculptor.WinForms.Models.Responses;

/// <summary>服务器处理智能体消息后的响应。</summary>
public sealed class AgentMessageResultDto
{
    /// <summary>服务器解释出的任务类型。</summary>
    public string TaskType { get; set; } = string.Empty;

    /// <summary>计算作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>当前状态。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>生成的输入文件路径。</summary>
    public string InputFilePath { get; set; } = string.Empty;

    /// <summary>计算输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}

/// <summary>计算状态响应。</summary>
public sealed class CalculationStatusDto
{
    public string JobId { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string InputFilePath { get; set; } = string.Empty;

    public string OutputFilePath { get; set; } = string.Empty;

    public List<CalculationDiagnosticDto> Diagnostics { get; set; } =
        new List<CalculationDiagnosticDto>();
}

/// <summary>通用计算结果响应。</summary>
public sealed class CalculationResultDto
{
    public string JobId { get; set; } = string.Empty;

    public double? Energy { get; set; }

    public string EnergyUnit { get; set; } = string.Empty;

    public bool NormalTermination { get; set; }

    public string FailureKind { get; set; } = string.Empty;

    public string Program { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string Basis { get; set; } = string.Empty;

    public int Charge { get; set; }

    public int Multiplicity { get; set; }

    public string OutputFilePath { get; set; } = string.Empty;

    public List<CalculationDiagnosticDto> Diagnostics { get; set; } =
        new List<CalculationDiagnosticDto>();
}

/// <summary>计算结果验证响应。</summary>
public sealed class CalculationValidationDto
{
    public string JobId { get; set; } = string.Empty;

    public bool Passed { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string ValidatorName { get; set; } = string.Empty;

    public List<CalculationValidationCheckDto> Checks { get; set; } =
        new List<CalculationValidationCheckDto>();

    public List<CalculationDiagnosticDto> Issues { get; set; } =
        new List<CalculationDiagnosticDto>();
}

/// <summary>单条计算验证检查响应。</summary>
public sealed class CalculationValidationCheckDto
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Passed { get; set; }

    public string Severity { get; set; } = string.Empty;

    public string Requirement { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string ExpectedValue { get; set; } = string.Empty;

    public string ActualValue { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

/// <summary>计算诊断响应。</summary>
public sealed class CalculationDiagnosticDto
{
    public string Severity { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

/// <summary>计算产物清单响应。</summary>
public sealed class CalculationArtifactManifestDto
{
    public string JobId { get; set; } = string.Empty;

    public List<CalculationArtifactFileDto> Files { get; set; } =
        new List<CalculationArtifactFileDto>();
}

/// <summary>单个计算产物。</summary>
public sealed class CalculationArtifactFileDto
{
    public string FileName { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public long Length { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string MediaType { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public bool CanUseForRestart { get; set; }

    public string DownloadPath { get; set; } = string.Empty;
}
