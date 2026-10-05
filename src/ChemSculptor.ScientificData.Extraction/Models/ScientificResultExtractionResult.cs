using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.ScientificData.Extraction.Models;

/// <summary>科学成果提取结果。</summary>
public sealed class ScientificResultExtractionResult
{
    /// <summary>是否成功提取。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>提取出的科学成果。</summary>
    public ScientificResult? Result { get; set; }
}
