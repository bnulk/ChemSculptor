using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;
using ChemSculptor.ScientificSummary.Abstractions;
using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.ScientificSummary;

/// <summary>
/// 客户端摘要编排服务。
/// 只从科学数据仓储读取科学成果，生成摘要后交给发送接口。
/// </summary>
public sealed class ClientSummaryService
    : IClientSummaryService
{
    private readonly IScientificDataRepository _repository;
    private readonly IClientSummaryBuilder _builder;
    private readonly IClientSummarySender _sender;

    /// <summary>创建客户端摘要服务。</summary>
    public ClientSummaryService(
        IScientificDataRepository repository,
        IClientSummaryBuilder builder,
        IClientSummarySender sender)
    {
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (sender == null)
        {
            throw new ArgumentNullException(nameof(sender));
        }

        _repository = repository;
        _builder = builder;
        _sender = sender;
    }

    /// <summary>生成并发送客户端摘要。</summary>
    public async Task<ClientScientificSummary?> BuildAndSendAsync(
        string resultId,
        string clientId,
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

        ClientScientificSummary summary =
            await _builder.BuildAsync(
                result,
                cancellationToken);
        await _sender.SendAsync(
            clientId,
            summary,
            cancellationToken);
        return summary;
    }
}
