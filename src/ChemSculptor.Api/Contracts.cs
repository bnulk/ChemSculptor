namespace ChemSculptor.Api;

public sealed record InterveneRequest
{
    public string Operation { get; init; } = "";

    public string? NodeId { get; init; }

    public string? Parameter { get; init; }

    public string? Value { get; init; }
}

public sealed record ApprovalRequest
{
    public bool Approved { get; init; }

    public string? Note { get; init; }
}

public sealed record RegisterContainerRequest
{
    public string Id { get; init; } = "";

    public string Version { get; init; } = "1.0.0";

    public IReadOnlyList<string> Capabilities { get; init; } = [];
}
