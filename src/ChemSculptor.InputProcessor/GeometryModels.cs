namespace ChemSculptor.InputProcessor;

public sealed class GeometryAtom
{
    public string Element { get; set; } = string.Empty;

    public double X { get; set; }

    public double Y { get; set; }

    public double Z { get; set; }
}

public sealed class MolecularGeometry
{
    public string SourceName { get; set; } = string.Empty;

    public string Formula { get; set; } = string.Empty;

    public string RawText { get; set; } = string.Empty;

    public List<GeometryAtom> Atoms { get; set; } = new List<GeometryAtom>();

    public List<string> Diagnostics { get; set; } = new List<string>();
}
