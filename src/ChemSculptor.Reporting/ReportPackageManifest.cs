namespace ChemSculptor.Reporting;

/// <summary>
/// 已生成成果包的文件清单。
/// 清单描述最终包内真实文件，不描述尚待生成的候选文件。
/// </summary>
public sealed class ReportPackageManifest
{
    /// <summary>报告契约模式版本。</summary>
    public string SchemaVersion { get; set; } = ReportContractVersions.Current;

    /// <summary>成果包标识。</summary>
    public string PackageId { get; set; } = string.Empty;

    /// <summary>触发成果包的请求标识。</summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>清单所描述的成果包范围。</summary>
    public ReportPackageScope? Scope { get; set; }

    /// <summary>证据快照标识。</summary>
    public string SnapshotId { get; set; } = string.Empty;

    /// <summary>报告文档标识。</summary>
    public string ReportDocumentId { get; set; } = string.Empty;

    /// <summary>成果包生成状态。</summary>
    public ReportPackageStatus Status { get; set; } =
        ReportPackageStatus.Draft;

    /// <summary>证据完整性。</summary>
    public ReportEvidenceCompleteness Completeness { get; set; } =
        ReportEvidenceCompleteness.Insufficient;

    /// <summary>包内全部文件。</summary>
    public List<ReportPackageFile> Files { get; set; } =
        new List<ReportPackageFile>();

    /// <summary>相对包根目录的校验清单路径。</summary>
    public string ChecksumManifestPath { get; set; } =
        "checksums.sha256";

    /// <summary>全部文件字节数之和。</summary>
    public long TotalBytes { get; set; }

    /// <summary>成果包生成时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>不影响文件完整性的成果包警告。</summary>
    public List<string> Warnings { get; set; } = new List<string>();
}

/// <summary>成果包中的一个真实文件。</summary>
public sealed class ReportPackageFile
{
    /// <summary>相对包根目录的规范路径。</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>文件在成果包中的角色。</summary>
    public ReportPackageFileRole Role { get; set; } =
        ReportPackageFileRole.Other;

    /// <summary>文件媒体类型。</summary>
    public string MediaType { get; set; } = "application/octet-stream";

    /// <summary>文件字节数。</summary>
    public long Length { get; set; }

    /// <summary>文件 SHA-256。</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>原始证据或科学数据中的来源标识；生成文件可为空。</summary>
    public string SourceReferenceId { get; set; } = string.Empty;

    /// <summary>该文件是否为成果包完整性的必要条件。</summary>
    public bool IsRequired { get; set; } = true;
}
