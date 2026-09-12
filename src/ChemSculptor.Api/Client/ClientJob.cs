namespace ChemSculptor.Api.Client;

/// <summary>
/// 一个客户端任务的内存记录。
/// </summary>
public sealed class ClientJob
{
    /// <summary>任务唯一标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>任务状态，例如 Queued、Running、Passed、Failed。</summary>
    public string Status { get; set; } = "Queued";

    /// <summary>状态说明或错误信息。</summary>
    public string? Message { get; set; }

    /// <summary>任务结果文本；尚未就绪时为 null。</summary>
    public string? ResultText { get; set; }

    /// <summary>任务创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>任务开始时间。</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>任务结束时间。</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
