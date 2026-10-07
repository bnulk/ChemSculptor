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

/// <summary>服务器生成的客户端科学摘要。</summary>
public sealed class ClientScientificSummaryDto
{
    /// <summary>对应的科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>摘要标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>摘要状态。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>摘要正文。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>顺序化摘要段落。</summary>
    public List<ClientScientificSummarySectionDto> Sections
    {
        get;
        set;
    } = new List<ClientScientificSummarySectionDto>();

    /// <summary>附加显示信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

    /// <summary>生成时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;
}

/// <summary>客户端科学摘要中的一个显示段落。</summary>
public sealed class ClientScientificSummarySectionDto
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public int Order { get; set; }
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

/// <summary>科学点文件成果包清单响应。</summary>
public sealed class ScientificArtifactManifestDto
{
    public string ResultId { get; set; } = string.Empty;

    public string RootJobId { get; set; } = string.Empty;

    public List<ScientificArtifactPointDto> Points { get; set; } =
        new List<ScientificArtifactPointDto>();
}

/// <summary>成果包中的单个科学点。</summary>
public sealed class ScientificArtifactPointDto
{
    public string PointId { get; set; } = string.Empty;

    public int Sequence { get; set; }

    public string DirectoryName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int Multiplicity { get; set; }

    public bool HasArtifactManifest { get; set; }

    public string ArtifactManifestMessage { get; set; } =
        string.Empty;

    public List<ScientificArtifactFileDto> Files { get; set; } =
        new List<ScientificArtifactFileDto>();
}

/// <summary>成果包中的单个文件。</summary>
public sealed class ScientificArtifactFileDto
{
    public string ArtifactId { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string DownloadFileName { get; set; } = string.Empty;

    public long Length { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }

    public string Error { get; set; } = string.Empty;

    public string DownloadPath { get; set; } = string.Empty;
}

/// <summary>由科学数据生成的成果叙述包。</summary>
public sealed class ScientificNarrativePackageDto
{
    public string ResultId { get; set; } = string.Empty;

    public string RootJobId { get; set; } = string.Empty;

    public string FinalSummary { get; set; } = string.Empty;

    public string Organization { get; set; } = string.Empty;

    public List<ScientificPointNarrativeDto> Points { get; set; } =
        new List<ScientificPointNarrativeDto>();

    public List<ScientificPointRelationDto> Relations { get; set; } =
        new List<ScientificPointRelationDto>();

    public List<ScientificObservableDto> Observables { get; set; } =
        new List<ScientificObservableDto>();
}

/// <summary>单个科学点的叙述文本。</summary>
public sealed class ScientificPointNarrativeDto
{
    public string PointId { get; set; } = string.Empty;

    public int Sequence { get; set; }

    public string PointSummary { get; set; } = string.Empty;

    public string Provenance { get; set; } = string.Empty;
}

/// <summary>可以追溯到科学点的关系。</summary>
public sealed class ScientificPointRelationDto
{
    public string Id { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string FromPointId { get; set; } = string.Empty;

    public string ToPointId { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int? Sequence { get; set; }

    public string Description { get; set; } = string.Empty;
}

/// <summary>可以追溯到科学点的物理量。</summary>
public sealed class ScientificObservableDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public double? NumericValue { get; set; }

    public string TextValue { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public string Formula { get; set; } = string.Empty;

    public List<string> PointIds { get; set; } = new List<string>();

    public List<string> RelationIds { get; set; } =
        new List<string>();

    public string Summary { get; set; } = string.Empty;
}
