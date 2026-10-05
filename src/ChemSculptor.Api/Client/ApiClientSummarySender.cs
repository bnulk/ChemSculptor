using System.Collections.Concurrent;
using ChemSculptor.ScientificSummary.Abstractions;
using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.Api.Client;

/// <summary>
/// API 端的客户端摘要发送器。
/// 当前使用内存邮箱，客户端通过接口领取摘要。
/// </summary>
public sealed class ApiClientSummarySender
    : IClientSummarySender
{
    private readonly ConcurrentDictionary<
        string,
        ConcurrentQueue<ClientScientificSummary>> _mailboxes =
        new ConcurrentDictionary<
            string,
            ConcurrentQueue<ClientScientificSummary>>(
            StringComparer.OrdinalIgnoreCase);

    /// <summary>把摘要放入指定客户端的邮箱。</summary>
    public Task SendAsync(
        string clientId,
        ClientScientificSummary summary,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException(
                "客户端标识不能为空。",
                nameof(clientId));
        }

        if (summary == null)
        {
            throw new ArgumentNullException(nameof(summary));
        }

        ConcurrentQueue<ClientScientificSummary> mailbox =
            _mailboxes.GetOrAdd(
                clientId,
                static _ =>
                    new ConcurrentQueue<
                        ClientScientificSummary>());
        mailbox.Enqueue(summary);
        return Task.CompletedTask;
    }

    /// <summary>领取并移除指定客户端当前的全部摘要。</summary>
    public IReadOnlyList<ClientScientificSummary> Take(
        string clientId)
    {
        List<ClientScientificSummary> summaries =
            new List<ClientScientificSummary>();

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return summaries;
        }

        ConcurrentQueue<ClientScientificSummary>? mailbox;

        if (!_mailboxes.TryGetValue(clientId, out mailbox))
        {
            return summaries;
        }

        ClientScientificSummary? summary;

        while (mailbox.TryDequeue(out summary))
        {
            if (summary != null)
            {
                summaries.Add(summary);
            }
        }

        return summaries;
    }
}
