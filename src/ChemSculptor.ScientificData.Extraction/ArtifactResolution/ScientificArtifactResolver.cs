using System.Security.Cryptography;
using ChemSculptor.Compute;
using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Models;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution;

/// <summary>
/// 根据科学数据仓储中的文件引用解析服务器真实文件。
/// 只访问计算工作区中对应作业的 run 目录。
/// </summary>
public sealed class ScientificArtifactResolver
    : IScientificArtifactResolver
{
    private readonly IScientificDataRepository _repository;
    private readonly ICalculationWorkspace _workspace;

    /// <summary>创建科学点文件解析服务。</summary>
    public ScientificArtifactResolver(
        IScientificDataRepository repository,
        ICalculationWorkspace workspace)
    {
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        _repository = repository;
        _workspace = workspace;
    }

    /// <summary>解析一项科学成果中全部科学点的文件引用。</summary>
    public async Task<ScientificArtifactManifest> ResolveManifestAsync(
        string resultId,
        CancellationToken cancellationToken = default)
    {
        ScientificArtifactManifest manifest =
            new ScientificArtifactManifest();
        manifest.ResultId = resultId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(resultId))
        {
            manifest.Error = "科学成果标识不能为空。";
            return manifest;
        }

        ScientificResult? result = await _repository.GetAsync(
            resultId,
            cancellationToken);

        if (result == null)
        {
            manifest.Error =
                "没有找到科学成果：" + resultId;
            return manifest;
        }

        manifest.ResultId = result.Id;
        manifest.RootWorkflowId = GetMetadataValue(
            result.Metadata,
            "rootWorkflowId");

        for (int index = 0;
            index < result.PointSet.Points.Count;
            index++)
        {
            CalculationPoint point =
                result.PointSet.Points[index];
            ScientificArtifactPointManifest pointManifest =
                await ResolvePointManifestAsync(
                    point,
                    index + 1,
                    cancellationToken);
            manifest.Points.Add(pointManifest);
        }

        manifest.Succeeded = true;
        return manifest;
    }

    /// <summary>打开一个已经通过路径和 SHA-256 验证的科学点文件。</summary>
    public async Task<ScientificArtifactOpenResult> OpenArtifactAsync(
        string resultId,
        string pointId,
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        ScientificArtifactOpenResult openResult =
            new ScientificArtifactOpenResult();

        if (string.IsNullOrWhiteSpace(resultId))
        {
            openResult.Error = "科学成果标识不能为空。";
            return openResult;
        }

        if (string.IsNullOrWhiteSpace(pointId))
        {
            openResult.Error = "科学点标识不能为空。";
            return openResult;
        }

        if (string.IsNullOrWhiteSpace(artifactId))
        {
            openResult.Error = "文件引用标识不能为空。";
            return openResult;
        }

        ScientificResult? result = await _repository.GetAsync(
            resultId,
            cancellationToken);

        if (result == null)
        {
            openResult.Error =
                "没有找到科学成果：" + resultId;
            return openResult;
        }

        CalculationPoint? point = FindPoint(
            result,
            pointId);

        if (point == null)
        {
            openResult.Error =
                "科学成果中不存在科学点：" + pointId;
            return openResult;
        }

        PointArtifactReference? artifact = FindArtifact(
            point,
            artifactId);

        if (artifact == null)
        {
            openResult.Error =
                "科学点中不存在文件引用：" + artifactId;
            return openResult;
        }

        string validationError = ValidateArtifactReference(
            point,
            artifact);

        if (!string.IsNullOrWhiteSpace(validationError))
        {
            openResult.Error = validationError;
            return openResult;
        }

        string runDirectory;

        try
        {
            runDirectory = _workspace.GetRunDirectory(
                point.CalculationJobId);
        }
        catch (ArgumentException exception)
        {
            openResult.Error =
                "计算作业标识无效：" + exception.Message;
            return openResult;
        }

        string fullPath;
        string pathError;

        if (!TryResolveArtifactPath(
            runDirectory,
            artifact.RelativePath,
            out fullPath,
            out pathError))
        {
            openResult.Error = pathError;
            return openResult;
        }

        FileStream? stream = null;

        try
        {
            stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous
                    | FileOptions.SequentialScan);
            string actualSha256 = await ComputeSha256Async(
                stream,
                cancellationToken);

            if (!string.Equals(
                artifact.Sha256,
                actualSha256,
                StringComparison.OrdinalIgnoreCase))
            {
                await stream.DisposeAsync();
                openResult.Error =
                    "SHA-256 校验失败：" +
                    artifact.RelativePath;
                return openResult;
            }

            FileInfo fileInfo = new FileInfo(fullPath);
            ScientificArtifactContent content =
                new ScientificArtifactContent();
            content.ResultId = result.Id;
            content.PointId = point.Id;
            content.ArtifactId = artifact.ArtifactId;
            content.CalculationJobId =
                artifact.CalculationJobId;
            content.DownloadFileName =
                artifact.DownloadFileName;
            content.MediaType = artifact.MediaType;
            content.Length = fileInfo.Length;
            content.Sha256 = actualSha256;
            content.Content = stream;

            openResult.Succeeded = true;
            openResult.Artifact = content;
            return openResult;
        }
        catch (FileNotFoundException)
        {
            openResult.Error =
                "文件不存在：" + artifact.RelativePath;
        }
        catch (DirectoryNotFoundException)
        {
            openResult.Error =
                "文件所属计算目录不存在：" + artifact.RelativePath;
        }
        catch (UnauthorizedAccessException)
        {
            openResult.Error =
                "没有权限读取文件：" + artifact.RelativePath;
        }
        catch (IOException exception)
        {
            openResult.Error =
                "读取文件失败：" +
                artifact.RelativePath +
                "；" +
                exception.Message;
        }
        catch (ArgumentException exception)
        {
            openResult.Error =
                "文件路径无效：" + exception.Message;
        }
        catch (NotSupportedException exception)
        {
            openResult.Error =
                "文件路径不受支持：" + exception.Message;
        }

        if (stream != null)
        {
            await stream.DisposeAsync();
        }

        return openResult;
    }

    private async Task<ScientificArtifactPointManifest>
        ResolvePointManifestAsync(
            CalculationPoint point,
            int sequence,
            CancellationToken cancellationToken)
    {
        ScientificArtifactPointManifest manifest =
            new ScientificArtifactPointManifest();
        manifest.Sequence = sequence;
        manifest.PointId = point.Id;
        manifest.CalculationJobId =
            point.CalculationJobId;

        if (string.IsNullOrWhiteSpace(point.Id))
        {
            manifest.Error = "科学点标识不能为空。";
        }
        else if (string.IsNullOrWhiteSpace(
            point.CalculationJobId))
        {
            manifest.Error = "科学点的计算作业标识不能为空。";
        }
        else
        {
            string runDirectory;

            try
            {
                runDirectory = _workspace.GetRunDirectory(
                    point.CalculationJobId);
            }
            catch (ArgumentException exception)
            {
                manifest.Error =
                    "计算作业标识无效：" +
                    exception.Message;
                runDirectory = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(runDirectory)
                && Directory.Exists(runDirectory))
            {
                manifest.IsAvailable = true;
            }
            else
            {
                manifest.Error =
                    "计算运行目录不存在：" + runDirectory;
            }

            for (int index = 0;
                index < point.Artifacts.Count;
                index++)
            {
                ScientificArtifactFileManifest artifactManifest =
                    await ResolveArtifactManifestAsync(
                        point,
                        point.Artifacts[index],
                        runDirectory,
                        cancellationToken);
                manifest.Artifacts.Add(artifactManifest);
            }

            return manifest;
        }

        for (int index = 0;
            index < point.Artifacts.Count;
            index++)
        {
            ScientificArtifactFileManifest artifactManifest =
                CreateUnavailableArtifactManifest(
                    point,
                    point.Artifacts[index],
                    manifest.Error);
            manifest.Artifacts.Add(artifactManifest);
        }

        return manifest;
    }

    private async Task<ScientificArtifactFileManifest>
        ResolveArtifactManifestAsync(
            CalculationPoint point,
            PointArtifactReference artifact,
            string runDirectory,
            CancellationToken cancellationToken)
    {
        ScientificArtifactFileManifest manifest =
            CreateArtifactManifest(point, artifact);

        string validationError = ValidateArtifactReference(
            point,
            artifact);

        if (!string.IsNullOrWhiteSpace(validationError))
        {
            manifest.Error = validationError;
            return manifest;
        }

        string fullPath;
        string pathError;

        if (!TryResolveArtifactPath(
            runDirectory,
            artifact.RelativePath,
            out fullPath,
            out pathError))
        {
            manifest.Error = pathError;
            return manifest;
        }

        if (!File.Exists(fullPath))
        {
            manifest.Error =
                "文件不存在：" + artifact.RelativePath;
            return manifest;
        }

        if (string.IsNullOrWhiteSpace(artifact.Sha256))
        {
            manifest.Error =
                "文件引用缺少 SHA-256 摘要：" +
                artifact.RelativePath;
            return manifest;
        }

        try
        {
            string actualSha256 = await ComputeFileSha256Async(
                fullPath,
                cancellationToken);
            manifest.ActualSha256 = actualSha256;

            if (!string.Equals(
                artifact.Sha256,
                actualSha256,
                StringComparison.OrdinalIgnoreCase))
            {
                manifest.Error =
                    "SHA-256 校验失败：" +
                    artifact.RelativePath;
                return manifest;
            }

            FileInfo fileInfo = new FileInfo(fullPath);
            manifest.Length = fileInfo.Length;
            manifest.IsSha256Valid = true;
            manifest.IsAvailable = true;
            return manifest;
        }
        catch (UnauthorizedAccessException)
        {
            manifest.Error =
                "没有权限读取文件：" + artifact.RelativePath;
            return manifest;
        }
        catch (IOException exception)
        {
            manifest.Error =
                "读取文件失败：" +
                artifact.RelativePath +
                "；" +
                exception.Message;
            return manifest;
        }
    }

    private static ScientificArtifactFileManifest
        CreateArtifactManifest(
            CalculationPoint point,
            PointArtifactReference artifact)
    {
        ScientificArtifactFileManifest manifest =
            new ScientificArtifactFileManifest();
        manifest.ArtifactId = artifact.ArtifactId;
        manifest.PointId = point.Id;
        manifest.CalculationJobId =
            artifact.CalculationJobId;
        manifest.Kind = artifact.Kind;
        manifest.RelativePath = artifact.RelativePath;
        manifest.DownloadFileName =
            artifact.DownloadFileName;
        manifest.MediaType = artifact.MediaType;
        manifest.Length = artifact.Length;
        manifest.ExpectedSha256 = artifact.Sha256;
        manifest.CanDownload = artifact.CanDownload;
        manifest.CanUseForRestart =
            artifact.CanUseForRestart;
        return manifest;
    }

    private static ScientificArtifactFileManifest
        CreateUnavailableArtifactManifest(
            CalculationPoint point,
            PointArtifactReference artifact,
            string error)
    {
        ScientificArtifactFileManifest manifest =
            CreateArtifactManifest(
                point,
                artifact);
        manifest.Error = error;
        return manifest;
    }

    private static string ValidateArtifactReference(
        CalculationPoint point,
        PointArtifactReference artifact)
    {
        if (string.IsNullOrWhiteSpace(artifact.ArtifactId))
        {
            return "文件引用标识不能为空。";
        }

        if (string.IsNullOrWhiteSpace(
            artifact.RelativePath))
        {
            return "文件相对路径不能为空。";
        }

        if (string.IsNullOrWhiteSpace(
            artifact.CalculationJobId))
        {
            return "文件引用的计算作业标识不能为空。";
        }

        if (!string.Equals(
            point.CalculationJobId,
            artifact.CalculationJobId,
            StringComparison.OrdinalIgnoreCase))
        {
            return
                "文件引用与科学点的计算作业标识不一致。";
        }

        if (!artifact.CanDownload)
        {
            return "该文件未标记为允许下载。";
        }

        return string.Empty;
    }

    private static CalculationPoint? FindPoint(
        ScientificResult result,
        string pointId)
    {
        for (int index = 0;
            index < result.PointSet.Points.Count;
            index++)
        {
            CalculationPoint point =
                result.PointSet.Points[index];

            if (string.Equals(
                point.Id,
                pointId,
                StringComparison.OrdinalIgnoreCase))
            {
                return point;
            }
        }

        return null;
    }

    private static PointArtifactReference? FindArtifact(
        CalculationPoint point,
        string artifactId)
    {
        for (int index = 0;
            index < point.Artifacts.Count;
            index++)
        {
            PointArtifactReference artifact =
                point.Artifacts[index];

            if (string.Equals(
                artifact.ArtifactId,
                artifactId,
                StringComparison.OrdinalIgnoreCase))
            {
                return artifact;
            }
        }

        return null;
    }

    private static bool TryResolveArtifactPath(
        string runDirectory,
        string relativePath,
        out string fullPath,
        out string error)
    {
        fullPath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(runDirectory))
        {
            error = "计算运行目录不能为空。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            error = "文件相对路径不能为空。";
            return false;
        }

        if (Path.IsPathRooted(relativePath))
        {
            error =
                "文件引用不能使用绝对路径：" +
                relativePath;
            return false;
        }

        try
        {
            string rootFullPath =
                Path.GetFullPath(runDirectory);
            string candidatePath = Path.GetFullPath(
                Path.Combine(
                    rootFullPath,
                    relativePath));
            string normalizedRoot = rootFullPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            string rootPrefix =
                normalizedRoot +
                Path.DirectorySeparatorChar;
            StringComparison comparison =
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

            if (!candidatePath.StartsWith(
                rootPrefix,
                comparison))
            {
                error =
                    "文件引用路径越界，已拒绝访问：" +
                    relativePath;
                return false;
            }

            if (ContainsReparsePoint(
                rootFullPath,
                candidatePath))
            {
                error =
                    "文件引用路径包含链接，已拒绝访问：" +
                    relativePath;
                return false;
            }

            fullPath = candidatePath;
            return true;
        }
        catch (ArgumentException exception)
        {
            error = "文件路径无效：" + exception.Message;
            return false;
        }
        catch (NotSupportedException exception)
        {
            error =
                "文件路径不受支持：" +
                exception.Message;
            return false;
        }
        catch (PathTooLongException exception)
        {
            error = "文件路径过长：" + exception.Message;
            return false;
        }
    }

    private static bool ContainsReparsePoint(
        string rootFullPath,
        string candidatePath)
    {
        string relativePath = Path.GetRelativePath(
            rootFullPath,
            candidatePath);
        string[] segments = relativePath.Split(
            new char[]
            {
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            },
            StringSplitOptions.RemoveEmptyEntries);
        string currentPath = rootFullPath;

        for (int index = 0;
            index < segments.Length;
            index++)
        {
            currentPath = Path.Combine(
                currentPath,
                segments[index]);

            if (!File.Exists(currentPath)
                && !Directory.Exists(currentPath))
            {
                continue;
            }

            FileAttributes attributes =
                File.GetAttributes(currentPath);

            if ((attributes & FileAttributes.ReparsePoint)
                != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<string> ComputeFileSha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        using (FileStream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous
                | FileOptions.SequentialScan))
        {
            return await ComputeSha256Async(
                stream,
                cancellationToken);
        }
    }

    private static async Task<string> ComputeSha256Async(
        Stream stream,
        CancellationToken cancellationToken)
    {
        byte[] hash = await SHA256.HashDataAsync(
            stream,
            cancellationToken);
        stream.Position = 0;
        return Convert.ToHexString(hash);
    }

    private static string GetMetadataValue(
        Dictionary<string, string> metadata,
        string key)
    {
        string? value;

        if (metadata.TryGetValue(key, out value))
        {
            return value ?? string.Empty;
        }

        return string.Empty;
    }
}
