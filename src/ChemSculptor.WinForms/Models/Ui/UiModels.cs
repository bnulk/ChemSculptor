namespace ChemSculptor.WinForms.Models.Ui;

/// <summary>会话中的一条消息。</summary>
public sealed class ChatMessage
{
    public string Role { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}

/// <summary>一个本地会话。</summary>
public sealed class ChatSession
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    public override string ToString()
    {
        return Title;
    }
}

/// <summary>客户端跟踪的单个计算作业。</summary>
public sealed class CalculationJobItem
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>提交任务的客户端会话标识。</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>当前状态。</summary>
    public string State { get; set; } = "Running";

    /// <summary>是否已经结束。</summary>
    public bool IsFinished { get; set; }

    /// <summary>已格式化的结果文本。</summary>
    public string? ResultText { get; set; }

    /// <summary>是否正在等待服务器科学摘要。</summary>
    public bool NeedsClientSummary { get; set; }

    /// <summary>服务器返回的客户端科学摘要。</summary>
    public string? ClientSummaryText { get; set; }

    public override string ToString()
    {
        return JobId + "    [" + State + "]";
    }
}
