namespace ChemSculptor.Api;

public sealed class InterveneRequest
{
    public string Operation { get; set; } = string.Empty;

    public string? NodeId { get; set; }

    public string? Parameter { get; set; }

    public string? Value { get; set; }
}

public sealed class ApprovalRequest
{
    public bool Approved { get; set; }

    public string? Note { get; set; }
}

public sealed class RegisterContainerRequest
{
    public string Id { get; set; } = string.Empty;

    public string Version { get; set; } = "1.0.0";

    public List<string> Capabilities { get; set; } = new List<string>();
}

public sealed class ApiError
{
    public string Error { get; set; } = string.Empty;
}

public sealed class ServiceInfoResponse
{
    public string Service { get; set; } = string.Empty;

    public List<string> Endpoints { get; set; } = new List<string>();
}

public sealed class InterventionResponse
{
    public string Id { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public string? NodeId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}

public sealed class ApprovalResponse
{
    public string Id { get; set; } = string.Empty;

    public bool Approved { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}

public sealed class ClientJobAcceptedResponse
{
    public string Id { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }
}

public sealed class ClientJobStatusResponse
{
    public string Id { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public bool HasResult { get; set; }
}

public sealed class GeometryAtomResponse
{
    public string Element { get; set; } = string.Empty;

    public double X { get; set; }

    public double Y { get; set; }

    public double Z { get; set; }
}

public sealed class GeometrySubmitResponse
{
    public string SourceName { get; set; } = string.Empty;

    public string Formula { get; set; } = string.Empty;

    public int AtomCount { get; set; }

    public List<GeometryAtomResponse> Atoms { get; set; } = new List<GeometryAtomResponse>();

    public List<string> Diagnostics { get; set; } = new List<string>();
}

public sealed class GeometryErrorResponse
{
    public string Error { get; set; } = string.Empty;

    public List<string> Diagnostics { get; set; } = new List<string>();
}
