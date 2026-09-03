namespace ChemSculptor.InputProcessor;

public sealed record GeometryAtom(string Element, double X, double Y, double Z);

public sealed record MolecularGeometry
{
    public required string SourceName { get; init; }

    public required string Formula { get; init; }

    public required string RawText { get; init; }

    public IReadOnlyList<GeometryAtom> Atoms { get; init; } = [];

    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
