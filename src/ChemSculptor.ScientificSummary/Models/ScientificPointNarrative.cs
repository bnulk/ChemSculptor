namespace ChemSculptor.ScientificSummary.Models;

/// <summary>单个科学点的叙述性文本。</summary>
public sealed class ScientificPointNarrative
{
    /// <summary>科学点标识。</summary>
    public string PointId { get; set; } = string.Empty;

    /// <summary>科学点在成果中的顺序，从 1 开始。</summary>
    public int Sequence { get; set; }

    /// <summary>科学点说明。</summary>
    public string PointSummary { get; set; } = string.Empty;

    /// <summary>科学点来源和矫正说明。</summary>
    public string Provenance { get; set; } = string.Empty;
}
