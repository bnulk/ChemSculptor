using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;
using ChemSculptor.ScientificSummary;
using ChemSculptor.ScientificSummary.Abstractions;
using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.Core.Tests;

/// <summary>客户端摘要服务框架测试。</summary>
public class ClientSummaryServiceTests
{
    /// <summary>验证摘要服务只从科学数据仓储读取并发送。</summary>
    [Fact]
    public async Task BuildsAndSendsSummaryFromRepository()
    {
        ScientificResult result = new ScientificResult();
        result.Id = "result-1";
        result.Title = "测试科学成果";
        RecordingRepository repository =
            new RecordingRepository(result);
        RecordingBuilder builder = new RecordingBuilder();
        RecordingSender sender = new RecordingSender();
        ClientSummaryService service = new ClientSummaryService(
            repository,
            builder,
            sender);

        ClientScientificSummary? summary =
            await service.BuildAndSendAsync(
                result.Id,
                "client-1");

        Assert.NotNull(summary);
        Assert.Equal("client-1", sender.ClientId);
        Assert.Equal(result.Id, builder.LastResultId);
        Assert.Equal(result.Id, sender.LastSummary!.ResultId);
    }

    /// <summary>验证默认摘要生成器只使用科学数据模型。</summary>
    [Fact]
    public async Task DefaultBuilderCreatesDisplaySections()
    {
        ScientificResult result = new ScientificResult();
        result.Id = "result-2";
        result.Title = "氧气计算";
        result.Summary = "计算完成。";
        result.Status = ScientificResultStatus.Complete;
        result.Metadata["correctionCount"] = "1";
        result.Metadata["acceptedPointId"] = "point-1";

        ScientificObservable observable =
            new ScientificObservable();
        observable.Id = "energy";
        observable.Name = "单点能量";
        observable.NumericValue = -150.274273534;
        observable.Unit = "Hartree";
        result.Observables.Add(observable);

        DefaultClientSummaryBuilder builder =
            new DefaultClientSummaryBuilder();
        ClientScientificSummary summary =
            await builder.BuildAsync(result);

        Assert.Equal(result.Id, summary.ResultId);
        Assert.Equal("Complete", summary.Status);
        Assert.Contains(
            summary.Sections,
            section => section.Id == "points");
        Assert.Contains(
            summary.Sections,
            section => section.Id == "observable-energy");
    }

    private sealed class RecordingRepository
        : IScientificDataRepository
    {
        private readonly ScientificResult _result;

        public RecordingRepository(ScientificResult result)
        {
            _result = result;
        }

        public Task SaveAsync(
            ScientificResult result,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<ScientificResult?> GetAsync(
            string resultId,
            CancellationToken cancellationToken = default)
        {
            if (string.Equals(
                resultId,
                _result.Id,
                StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<ScientificResult?>(_result);
            }

            return Task.FromResult<ScientificResult?>(null);
        }

        public IReadOnlyList<ScientificResult> List()
        {
            List<ScientificResult> results =
                new List<ScientificResult>();
            results.Add(_result);
            return results;
        }
    }

    private sealed class RecordingBuilder
        : IClientSummaryBuilder
    {
        public string LastResultId { get; private set; } =
            string.Empty;

        public Task<ClientScientificSummary> BuildAsync(
            ScientificResult result,
            CancellationToken cancellationToken = default)
        {
            LastResultId = result.Id;
            ClientScientificSummary summary =
                new ClientScientificSummary();
            summary.ResultId = result.Id;
            return Task.FromResult(summary);
        }
    }

    private sealed class RecordingSender
        : IClientSummarySender
    {
        public string ClientId { get; private set; } =
            string.Empty;

        public ClientScientificSummary? LastSummary
        {
            get;
            private set;
        }

        public Task SendAsync(
            string clientId,
            ClientScientificSummary summary,
            CancellationToken cancellationToken = default)
        {
            ClientId = clientId;
            LastSummary = summary;
            return Task.CompletedTask;
        }
    }
}
