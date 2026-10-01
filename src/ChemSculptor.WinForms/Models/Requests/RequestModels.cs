namespace ChemSculptor.WinForms.Models.Requests;

/// <summary>发送给服务器的智能体原始消息。</summary>
public sealed class AgentMessageRequestDto
{
    /// <summary>客户端会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>用户原始自然语言文本。</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>当前选择的坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;
}
