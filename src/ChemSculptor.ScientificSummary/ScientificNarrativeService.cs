using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;
using ChemSculptor.ScientificSummary.Abstractions;
using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.ScientificSummary;

/// <summary>从科学数据仓储生成叙述包。</summary>
public sealed class ScientificNarrativeService
    : IScientificNarrativeService
{
    private readonly IScientificDataRepository _repository;
    private readonly IScientificNarrativeBuilder _builder;

    /// <summary>创建叙述服务。</summary>
    public ScientificNarrativeService(
        IScientificDataRepository repository,
        IScientificNarrativeBuilder builder)
    {
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        _repository = repository;
        _builder = builder;
    }

    /// <summary>按科学成果标识生成叙述包。</summary>
    public async Task<ScientificNarrativePackage?> BuildAsync(
        string resultId,
        CancellationToken cancellationToken = default)
    {
        ScientificResult? result =
            await _repository.GetAsync(
                resultId,
                cancellationToken);

        if (result == null)
        {
            return null;
        }

        return await _builder.BuildAsync(
            result,
            cancellationToken);
    }
}
