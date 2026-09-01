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
