using ChemSculptor.ScientificData.Extraction.Abstractions;
using ChemSculptor.ScientificData.Extraction.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.ScientificData.Extraction;

/// <summary>提取科学成果并写入科学数据仓储。</summary>
public sealed class ScientificDataRecorder
    : IScientificDataRecorder
{
    private readonly IScientificResultExtractor _extractor;
    private readonly IScientificDataRepository _repository;

    /// <summary>创建科学数据记录器。</summary>
    public ScientificDataRecorder(
        IScientificResultExtractor extractor,
        IScientificDataRepository repository)
    {
        if (extractor == null)
        {
            throw new ArgumentNullException(nameof(extractor));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(
                nameof(repository));
        }

        _extractor = extractor;
        _repository = repository;
    }

    /// <summary>提取并保存科学成果。</summary>
    public async Task<ScientificResultExtractionResult> RecordAsync(
        ScientificResultExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        ScientificResultExtractionResult result =
            await _extractor.ExtractAsync(
                request,
                cancellationToken);

        if (!result.Succeeded || result.Result == null)
        {
            return result;
        }

        await _repository.SaveAsync(
            result.Result,
            cancellationToken);
        return result;
    }
}
