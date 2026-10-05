using ChemSculptor.ScientificData.Extraction.Models;

namespace ChemSculptor.ScientificData.Extraction.Abstractions;

/// <summary>把计算和异常处理结果提取为科学成果。</summary>
public interface IScientificResultExtractor
{
    /// <summary>提取科学成果。</summary>
    Task<ScientificResultExtractionResult> ExtractAsync(
        ScientificResultExtractionRequest request,
        CancellationToken cancellationToken = default);
}
