namespace ChemSculptor.Api.Client;

public sealed class ClientJob
{
    public required string Id { get; init; }

    public string Status { get; set; } = "Queued";

    public string? Message { get; set; }

    public string? ResultText { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
