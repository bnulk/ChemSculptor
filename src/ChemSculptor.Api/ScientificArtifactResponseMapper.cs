using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Models;

namespace ChemSculptor.Api;

/// <summary>把科学点文件解析结果转换为 API 响应。</summary>
public static class ScientificArtifactResponseMapper
{
    /// <summary>转换成果包清单。</summary>
    public static ScientificArtifactManifestResponse Convert(
        ScientificArtifactManifest manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        ScientificArtifactManifestResponse response =
            new ScientificArtifactManifestResponse();
        response.ResultId = manifest.ResultId;
        response.RootJobId = manifest.RootWorkflowId;

        for (int pointIndex = 0;
            pointIndex < manifest.Points.Count;
            pointIndex++)
        {
            ScientificArtifactPointManifest point =
                manifest.Points[pointIndex];
            ScientificArtifactPointResponse pointResponse =
                new ScientificArtifactPointResponse();
            pointResponse.PointId = point.PointId;
            pointResponse.Sequence = point.Sequence;
            pointResponse.DirectoryName =
                point.DirectoryName;
            pointResponse.Status = point.Status.ToString();
            pointResponse.Multiplicity = point.Multiplicity;
            pointResponse.HasArtifactManifest =
                point.HasArtifactManifest;
            pointResponse.ArtifactManifestMessage =
                point.ArtifactManifestMessage;

            for (int fileIndex = 0;
                fileIndex < point.Artifacts.Count;
                fileIndex++)
            {
                ScientificArtifactFileManifest file =
                    point.Artifacts[fileIndex];
                ScientificArtifactFileResponse fileResponse =
                    new ScientificArtifactFileResponse();
                fileResponse.ArtifactId = file.ArtifactId;
                fileResponse.Kind = file.Kind.ToString();
                fileResponse.DownloadFileName =
                    file.DownloadFileName;
                fileResponse.Length = file.Length;
                fileResponse.Sha256 =
                    file.ExpectedSha256;
                fileResponse.IsAvailable = file.IsAvailable;
                fileResponse.Error = file.Error;
                fileResponse.DownloadPath =
                    BuildDownloadPath(
                        manifest.ResultId,
                        point.PointId,
                        file.ArtifactId);
                pointResponse.Files.Add(fileResponse);
            }

            response.Points.Add(pointResponse);
        }

        return response;
    }

    private static string BuildDownloadPath(
        string resultId,
        string pointId,
        string artifactId)
    {
        return
            "/scientific-results/" +
            Uri.EscapeDataString(resultId) +
            "/artifacts/" +
            Uri.EscapeDataString(pointId) +
            "/" +
            Uri.EscapeDataString(artifactId);
    }
}
