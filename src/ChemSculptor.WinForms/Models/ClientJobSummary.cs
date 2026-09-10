namespace ChemSculptor.WinForms;

public sealed class ClientJobSummary
{
    public string Id { get; set; } = string.Empty;

    public string JobId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public bool HasResult { get; set; }
}

public sealed class ClientJobItem
{
    public string Id { get; set; } = string.Empty;

    public string Status { get; set; } = "Queued";

    public string? ResultText { get; set; }

    public override string ToString()
    {
        return Id + "    [" + Status + "]";
    }
}

public sealed class GeometryAtomDto
{
    public string Element { get; set; } = string.Empty;

    public double X { get; set; }

    public double Y { get; set; }

    public double Z { get; set; }
}

public sealed class GeometrySubmitResult
{
    public string SourceName { get; set; } = string.Empty;

    public string Formula { get; set; } = string.Empty;

    public int AtomCount { get; set; }

    public List<GeometryAtomDto> Atoms { get; set; } = new List<GeometryAtomDto>();

    public List<string> Diagnostics { get; set; } = new List<string>();
}

public sealed class ChatMessage
{
    public string Role { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}

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
