using ChemSculptor.ScientificData.Extraction.Models;

namespace ChemSculptor.ScientificData.Extraction.Abstractions;

/// <summary>提取并保存科学成果。</summary>
public interface IScientificDataRecorder
{
    /// <summary>提取并保存科学成果。</summary>
    Task<ScientificResultExtractionResult> RecordAsync(
        ScientificResultExtractionRequest request,
        CancellationToken cancellationToken = default);
}
