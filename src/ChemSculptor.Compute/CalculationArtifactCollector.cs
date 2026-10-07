using System.Security.Cryptography;
using System.Text;

namespace ChemSculptor.Compute;

/// <summary>
/// 通用计算产物收集器。
/// 根据程序适配器提供的规则枚举文件，并生成可持久化的描述。
/// </summary>
public static class CalculationArtifactCollector
{
    /// <summary>
    /// 收集运行目录中的计算产物。
    /// 同一文件匹配多条规则时，采用第一条匹配规则。
    /// </summary>
    /// <param name="rootDirectory">计算运行目录。</param>
    /// <param name="patterns">程序适配器声明的产物规则。</param>
    /// <returns>按文件名排序的产物描述列表。</returns>
    public static List<CalculationArtifactDescriptor> Collect(
        string rootDirectory,
        IReadOnlyList<CalculationArtifactPattern> patterns)
    {
        List<CalculationArtifactDescriptor> artifacts =
            new List<CalculationArtifactDescriptor>();

        if (string.IsNullOrWhiteSpace(rootDirectory)
            || !Directory.Exists(rootDirectory)
            || patterns == null)
        {
            return artifacts;
        }

        Dictionary<string, CalculationArtifactPattern> matchedPatterns =
            new Dictionary<string, CalculationArtifactPattern>(
                StringComparer.OrdinalIgnoreCase);

        for (int patternIndex = 0; patternIndex < patterns.Count; patternIndex++)
        {
            CalculationArtifactPattern pattern = patterns[patternIndex];

            if (pattern == null || string.IsNullOrWhiteSpace(pattern.FilePattern))
            {
                continue;
            }

            string[] matches = Directory.GetFiles(
                rootDirectory,
                pattern.FilePattern,
                SearchOption.TopDirectoryOnly);

            for (int matchIndex = 0; matchIndex < matches.Length; matchIndex++)
            {
                string fileName = Path.GetFileName(matches[matchIndex]);

                if (!matchedPatterns.ContainsKey(fileName))
                {
                    matchedPatterns[fileName] = pattern;
                }
            }
        }

        List<string> orderedFileNames = new List<string>(matchedPatterns.Keys);
        orderedFileNames.Sort(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < orderedFileNames.Count; index++)
        {
            string fileName = orderedFileNames[index];
            string fullPath = Path.Combine(rootDirectory, fileName);

            if (!File.Exists(fullPath))
            {
                continue;
            }

            CalculationArtifactPattern pattern = matchedPatterns[fileName];
            FileInfo fileInfo = new FileInfo(fullPath);

            // 空的标准输出/标准错误文件没有科学或诊断价值。
            if (fileInfo.Length == 0)
            {
                continue;
            }

            CalculationArtifactDescriptor descriptor =
                new CalculationArtifactDescriptor();
            descriptor.FileName = fileName;
            descriptor.RelativePath = fileName;
            descriptor.Kind = pattern.Kind;
            descriptor.MediaType = pattern.MediaType;
            descriptor.Length = fileInfo.Length;
            descriptor.Sha256 = ComputeSha256(fullPath);
            descriptor.CanUseForRestart = pattern.CanUseForRestart;
            artifacts.Add(descriptor);
        }

        return artifacts;
    }

    /// <summary>
    /// 把结果文件中的相对路径解析为运行目录内的绝对路径。
    /// 解析结果必须位于运行目录内，防止结果文件被手工修改后形成路径穿越。
    /// </summary>
    /// <param name="rootDirectory">计算运行目录。</param>
    /// <param name="relativePath">结果文件中的相对路径。</param>
    /// <param name="fullPath">解析成功时返回绝对路径。</param>
    /// <returns>路径合法且文件存在时返回 true。</returns>
    public static bool TryResolveFullPath(
        string rootDirectory,
        string relativePath,
        out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(rootDirectory)
            || string.IsNullOrWhiteSpace(relativePath)
            || Path.IsPathRooted(relativePath))
        {
            return false;
        }

        try
        {
            string rootFullPath = Path.GetFullPath(rootDirectory);
            string candidatePath = Path.GetFullPath(
                Path.Combine(rootFullPath, relativePath));

            string normalizedRoot = rootFullPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            string rootPrefix = normalizedRoot + Path.DirectorySeparatorChar;
            StringComparison pathComparison =
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

            if (!candidatePath.StartsWith(
                rootPrefix,
                pathComparison))
            {
                return false;
            }

            if (!File.Exists(candidatePath))
            {
                return false;
            }

            fullPath = candidatePath;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (PathTooLongException)
        {
            return false;
        }
    }

    private static string ComputeSha256(string path)
    {
        using (FileStream stream = File.OpenRead(path))
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(hash.Length * 2);

                for (int index = 0; index < hash.Length; index++)
                {
                    builder.Append(hash[index].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
