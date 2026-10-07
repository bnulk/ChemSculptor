using System.Globalization;
using System.Text;
using ChemSculptor.Compute;
using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution;

/// <summary>从计算产物描述生成科学点文件引用。</summary>
public static class ScientificArtifactReferenceFactory
{
    /// <summary>根据分子式和科学点角色生成规范文件名主体。</summary>
    public static string BuildCanonicalStem(
        string formula,
        string role,
        int multiplicity)
    {
        string basis = string.IsNullOrWhiteSpace(formula)
            ? "point"
            : formula;
        StringBuilder builder = new StringBuilder();

        for (int index = 0;
            index < basis.Length;
            index++)
        {
            char value = basis[index];

            if (char.IsLetterOrDigit(value)
                || value == '-'
                || value == '_'
                || value == '.')
            {
                builder.Append(value);
            }
            else
            {
                builder.Append('-');
            }
        }

        return builder.ToString() +
            "-" +
            role +
            "-m" +
            multiplicity.ToString(
                CultureInfo.InvariantCulture);
    }

    /// <summary>根据来源点判断科学点角色。</summary>
    public static string ResolveRole(CalculationPoint point)
    {
        return string.IsNullOrWhiteSpace(
            point.Provenance.ParentPointId)
                ? "original"
                : "recovery";
    }

    /// <summary>把计算产物描述复制为科学点文件引用。</summary>
    public static List<PointArtifactReference> CreateReferences(
        CalculationJob job,
        CalculationResult result,
        string canonicalStem)
    {
        List<PointArtifactReference> references =
            new List<PointArtifactReference>();

        if (result.Artifacts == null
            || result.Artifacts.Count == 0)
        {
            return references;
        }

        HashSet<string> usedFileNames =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        for (int index = 0;
            index < result.Artifacts.Count;
            index++)
        {
            CalculationArtifactDescriptor artifact =
                result.Artifacts[index];

            if (string.IsNullOrWhiteSpace(
                artifact.RelativePath))
            {
                continue;
            }

            string extension = Path.GetExtension(
                artifact.RelativePath);
            string fileStem = canonicalStem;
            string fileName = fileStem + extension;
            int duplicateIndex = 2;

            while (!usedFileNames.Add(fileName))
            {
                fileStem =
                    canonicalStem +
                    "-" +
                    duplicateIndex.ToString(
                        CultureInfo.InvariantCulture);
                fileName = fileStem + extension;
                duplicateIndex++;
            }

            PointArtifactReference reference =
                new PointArtifactReference();
            reference.ArtifactId =
                "artifact-" +
                job.JobId +
                "-" +
                index.ToString(
                    CultureInfo.InvariantCulture);
            reference.CalculationJobId = job.JobId;
            reference.Kind = MapArtifactKind(
                artifact.Kind);
            reference.RelativePath = artifact.RelativePath;
            reference.DownloadFileName = fileName;
            reference.CanonicalStem = fileStem;
            reference.CanonicalExtension = extension;
            reference.MediaType = artifact.MediaType;
            reference.Length = artifact.Length;
            reference.Sha256 = artifact.Sha256;
            reference.CanDownload = true;
            reference.CanUseForRestart =
                artifact.CanUseForRestart;
            references.Add(reference);
        }

        return references;
    }

    private static ScientificArtifactKind MapArtifactKind(
        CalculationArtifactKind kind)
    {
        if (kind == CalculationArtifactKind.Input)
        {
            return ScientificArtifactKind.Input;
        }

        if (kind == CalculationArtifactKind.PrimaryOutput)
        {
            return ScientificArtifactKind.PrimaryOutput;
        }

        if (kind == CalculationArtifactKind.SupportingOutput)
        {
            return ScientificArtifactKind.SupportingOutput;
        }

        if (kind == CalculationArtifactKind.RestartState)
        {
            return ScientificArtifactKind.RestartState;
        }

        return ScientificArtifactKind.Other;
    }
}
