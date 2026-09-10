namespace ChemSculptor.Api.Client;

public sealed class ClientJob
{
    public string Id { get; set; } = string.Empty;

    public string Status { get; set; } = "Queued";

    public string? Message { get; set; }

    public string? ResultText { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
