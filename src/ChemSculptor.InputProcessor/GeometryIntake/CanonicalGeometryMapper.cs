namespace ChemSculptor.InputProcessor.GeometryIntake;

/// <summary>
/// 把旧的分子几何解析结果转换为规范几何模型。
/// 该转换只复制原子坐标，不解释分层、片段或约束。
/// </summary>
public static class CanonicalGeometryMapper
{
    /// <summary>把分子几何转换为规范几何。</summary>
    public static CanonicalGeometry FromMolecularGeometry(
        MolecularGeometry geometry,
        string sourceSubmissionId)
    {
        if (geometry == null)
        {
            throw new ArgumentNullException(nameof(geometry));
        }

        CanonicalGeometry canonicalGeometry = new CanonicalGeometry();
        canonicalGeometry.Units = GeometryUnits.Angstrom;
        canonicalGeometry.SourceSubmissionId = sourceSubmissionId;
        canonicalGeometry.Atoms = new List<CanonicalAtom>();

        for (int index = 0; index < geometry.Atoms.Count; index++)
        {
            GeometryAtom sourceAtom = geometry.Atoms[index];
            CanonicalAtom targetAtom = new CanonicalAtom();
            targetAtom.Index = index + 1;
            targetAtom.Element = sourceAtom.Element;
            targetAtom.X = sourceAtom.X;
            targetAtom.Y = sourceAtom.Y;
            targetAtom.Z = sourceAtom.Z;
            canonicalGeometry.Atoms.Add(targetAtom);
        }

        return canonicalGeometry;
    }
}
