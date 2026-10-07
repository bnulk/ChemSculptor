namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution.Backfill.Models;

/// <summary>单个科学点的文件引用回填结果。</summary>
public sealed class ScientificArtifactBackfillPointResult
{
    /// <summary>科学点标识。</summary>
    public string PointId { get; set; } = string.Empty;

    /// <summary>计算作业标识。</summary>
    public string CalculationJobId { get; set; } = string.Empty;

    /// <summary>是否实际写入了新的文件引用。</summary>
    public bool Backfilled { get; set; }

    /// <summary>回填后的文件引用数量。</summary>
    public int ArtifactCount { get; set; }

    /// <summary>处理说明。</summary>
    public string Message { get; set; } = string.Empty;
}
