namespace ChemSculptor.WinForms;

public sealed record ClientJobSummary
{
    public string Id { get; init; } = "";

    public string JobId { get; init; } = "";

    public string Status { get; init; } = "";

    public string? Message { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public bool HasResult { get; init; }
}

public sealed class ClientJobItem
{
    public required string Id { get; init; }

    public string Status { get; set; } = "Queued";

    public string? ResultText { get; set; }

    public override string ToString() => $"{Id}    [{Status}]";
}

public sealed record GeometryAtomDto
{
    public string Element { get; init; } = "";

    public double X { get; init; }

    public double Y { get; init; }

    public double Z { get; init; }
}

public sealed record GeometrySubmitResult
{
    public string SourceName { get; init; } = "";

    public string Formula { get; init; } = "";

    public int AtomCount { get; init; }

    public IReadOnlyList<GeometryAtomDto> Atoms { get; init; } = [];

    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}

public sealed record ChatMessage
{
    public required string Role { get; init; }

    public required string Text { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
}

public sealed class ChatSession
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public List<ChatMessage> Messages { get; } = [];

    public override string ToString() => Title;
}
