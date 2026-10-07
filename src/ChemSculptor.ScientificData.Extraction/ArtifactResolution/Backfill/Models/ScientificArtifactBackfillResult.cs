namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution.Backfill.Models;

/// <summary>一项科学成果的文件引用回填结果。</summary>
public sealed class ScientificArtifactBackfillResult
{
    /// <summary>科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>回填流程是否成功完成。</summary>
    public bool Succeeded { get; set; }

    /// <summary>流程级错误。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>扫描的科学点数量。</summary>
    public int PointsScanned { get; set; }

    /// <summary>实际回填的科学点数量。</summary>
    public int PointsBackfilled { get; set; }

    /// <summary>跳过的科学点数量。</summary>
    public int PointsSkipped { get; set; }

    /// <summary>逐科学点结果。</summary>
    public List<ScientificArtifactBackfillPointResult> Points { get; set; } =
        new List<ScientificArtifactBackfillPointResult>();
}
