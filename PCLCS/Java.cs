namespace PCLCS;

[Serializable] public class Java(string folder, Version? version = null) : IEquatable<Java> {

    public readonly string Folder = PathUtils.AddSlashSuffix(PathUtils.ForCompare(folder));

    [JsonIgnore] public string JavaExePath => Path.Combine(Folder, "java.exe");

    public Version? Version = version;

    [JsonIgnore] public string? FileEncodingName;

    [JsonIgnore] public string? NativeEncodingName;

    public async Task<bool> CheckAsync(CancellationToken cancellationToken = default) {
        if (Interlocked.Exchange(ref isChecked, 1) == 1 && available != null) return available!.Value;
        string? output = null;
        try {
            if (!FileUtils.Exists(JavaExePath)) throw new FileNotFoundException("未找到 java.exe 文件", JavaExePath);

            output = (await TaskUtils.RunProgramAsync(JavaExePath, "-XshowSettings:properties -version", 15000, cancellationToken: cancellationToken).NoCapture()).Output.Lower();
            if (output == "") throw new InvalidOperationException("尝试运行该 Java 失败");
            Logger.Trace($"Java 检查输出：{JavaExePath}{Environment.NewLine}{output}");
            if (output.Contains("/lib/ext exists")) throw new InvalidOperationException("无法运行该 Java，请在删除 Java 文件夹中的 /lib/ext 文件夹后再试");
            if (output.Contains("a fatal error") || output.Contains("error: ")) throw new InvalidOperationException("无法运行该 Java，该 Java 或系统存在问题");

            var versionString = (output.RegexSeek(@"(?<=version "")[^""]+") ?? output.RegexSeek("(?<=openjdk )[0-9]+"))?.Replace("_", ".").Replace("+", ".").BeforeFirst("-");
            if (string.IsNullOrEmpty(versionString)) throw new InvalidOperationException($"未找到该 Java 的版本号{(output.Length < 500 ? $"\n原始输出为：\n{output}" : "")}");
            if (versionString.StartsWithF("1.")) versionString = versionString!.AfterFirst(".");
            var segments = versionString!.Split(".", true).ToList();
            while (segments.Count < 4) segments.Add("0");
            Version = new Version(segments.Take(4).Join("."));
            if (Version.Major is <= 4 or >= 100) throw new InvalidOperationException($"分析详细信息失败，获取的版本为 {Version}{(output.Length < 500 ? $"\n原始输出为：\n{output}" : "")}");

            if (!output.Contains("64-bit")) throw new InvalidOperationException("该 Java 为 32 位版本，请安装 64 位的 Java");
            string? GetProperty(string name) {
                string prefix = name + " = ";
                string? line = output.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.StartsWithF(prefix));
                return line?[prefix.Length..].Trim();
            }
            FileEncodingName = GetProperty("file.encoding");
            NativeEncodingName = GetProperty("native.encoding") ?? FileEncodingName;

            Logger.Info($"检查 Java 成功：{this}");
            available = true;
        } catch (Exception ex) {
            if (ex.IsCanceled()) {
                isChecked = 0;
                throw;
            }
            Logger.Warn($"检查 Java 失败（{JavaExePath}），其输出为：\n{output ?? "无程序输出"}");
            available = false;
        }
        return available!.Value;
    }

    public void InvalidateChecked() {
        isChecked = 0;
        available = null;
    }

    [JsonIgnore] private int isChecked = 0;

    [JsonIgnore] internal bool? available = null;

    public bool Equals(Java? other)
        => other is not null && PathUtils.IsSame(Folder, other.Folder);
    public override bool Equals(object? obj) => obj is Java other && Equals(other);
    public override int GetHashCode() => StringComparer.CurrentCultureIgnoreCase.GetHashCode(PathUtils.ForCompare(Folder));

    public override string ToString()
        => $"Java {Version?.Major.ToString() ?? "尚未检查"} ({Version})：{Folder}";
}

public static class JavaUtils {

    public static async Task<List<Java>> CheckAllAsync(IEnumerable<Java> javas, CancellationToken cancellationToken = default, ProgressProvider? progress = null) {
        await TaskUtils.WhenAll(javas.Where(j => j.available == null).Select(
            async java => await java.CheckAsync(cancellationToken).NoCapture()
        ), progress).NoCapture();
        return javas.Where(j => j.available == true).ToList();
    }

    public static async Task<List<Java>> SortAsync(List<Java> javas, CancellationToken cancellationToken = default, ProgressProvider? progress = null) {
        javas = await JavaUtils.CheckAllAsync(javas, cancellationToken, progress).NoCapture();
        javas.Sort((left, right) => {

            var isLeftInCandidateFolder = JavaUtils.CandidateFolders.Value.Any(folder => PathUtils.IsParentOf(folder, left.Folder));
            var isRightInCandidateFolder = JavaUtils.CandidateFolders.Value.Any(folder => PathUtils.IsParentOf(folder, right.Folder));
            if (isLeftInCandidateFolder != isRightInCandidateFolder) return isLeftInCandidateFolder ? -1 : 1;

            var leftDistance = left.Version is null ? int.MaxValue : Math.Abs(left.Version.Major - 21);
            var rightDistance = right.Version is null ? int.MaxValue : Math.Abs(right.Version.Major - 21);
            return leftDistance.CompareTo(rightDistance);
        });
        return javas;
    }

    #region 搜索

    internal static readonly Lazy<List<string>> CandidateFolders = new(() => {
        List<string> folders = [
            Paths.AppData + @".minecraft\runtime\",
            Paths.AppData + @".hmcl\java\",
            Paths.AppData + @"ATLauncher\runtimes\minecraft\",
            Paths.AppData + @"ModrinthApp\meta\java_versions\",
            Paths.AppData + @"PrismLauncher\java\",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\curseforge\minecraft\Install\runtime\",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\.jdks\",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\.sdkman\candidates\java\",
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\.ftba\bin\runtime\",
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.4297127D64EC6_8wekyb3d8bbwe\LocalCache\Local\runtime\",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Minecraft Launcher\runtime\",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Minecraft\runtime\",
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Curse\Minecraft\Install\runtime\",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Java\",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Eclipse Adoptium\",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Amazon Corretto\",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Zulu\",
            ..DirectoryUtils.EnumerateDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Microsoft\", searchPattern: "jdk-*"),

            ..(Environment.GetEnvironmentVariable("JDK_HOME") + ";" + Environment.GetEnvironmentVariable("JAVA_HOME")).
                Split(";", true).Select(path => path.Trim(' ', '"'))
        ];
        return folders.Select(f => {
            try {
                return PathUtils.ForCompare(f);
            } catch (Exception ex) {
                Logger.Warn(ex, $"Java 候选文件夹路径无效（{f}）");
                return null;
            }
        }).Where(f => f is not null).Cast<string>().ToList();
    });

    public static async Task RefreshListAsync(CancellationToken c = default, ProgressProvider? p = null) {

        List<Java> javaList = await SearchFoldersAsync(true, CandidateFolders.Value, c, p?.SplitTo(0.5)).NoCapture();

        var oldJavaList = Configs.JavaList.Get()!;
        oldJavaList = await JavaUtils.CheckAllAsync(oldJavaList, c, p?.SplitTo(0.9)).NoCapture();
        javaList.RemoveAll(oldJavaList);
        javaList = await JavaUtils.SortAsync(javaList, c).NoCapture();
        javaList.AddRange(oldJavaList);
        javaList = javaList.Distinct().ToList();

        javaList.RemoveIf(j => Configs.JavaRemovedList.Get()!.Contains(j.Folder, StringComparer.OrdinalIgnoreCase));

        Configs.JavaList.Set([..javaList]);
        Logger.Info($"Java 搜索完成，发现 {javaList.Count} 个 Java");
    }

    public static async Task<List<Java>> SearchFoldersAsync(bool includeSubDirectories, IEnumerable<string> folders, CancellationToken cancellationToken = default, ProgressProvider? progress = null) {

        ConcurrentBag<(string Folder, bool HasReparsePoint, bool IsSpecialPath)> results = [];
        await TaskUtils.ForEachAsync(folders.Distinct(StringComparer.OrdinalIgnoreCase), 4, async (folder, cp) => {
            await Task.Run(() => {
                try {
                    foreach (var file in DirectoryUtils.EnumerateFiles(folder, includeSubDirectories, "java.exe")) {
                        cancellationToken.ThrowIfCancellationRequested();
                        string targetFolder = PathUtils.AddSlashSuffix(PathUtils.ForCompare(PathUtils.RemoveLastPart(file)));

                        static bool HasReparsePoint(FileSystemInfo info) {
                            if (PathUtils.IsParentOf(Paths.AppData + @".minecraft\runtime\", info.FullName)) return false;
                            do {
                                if (info.Attributes.HasFlagF(FileAttributes.ReparsePoint)) return true;
                                info = info is FileInfo fileInfo ? fileInfo.Directory : ((DirectoryInfo) info).Parent;
                            } while (info is not null);
                            return false;
                        }

                        var hasReparsePoint = HasReparsePoint(FileUtils.GetInfo(Path.Combine(targetFolder, "java.exe")));
                        var isSpecialPath = targetFolder.ContainsIgnoreCase("java8path_target_") || targetFolder.ContainsIgnoreCase("javapath_target_") || targetFolder.ContainsIgnoreCase("javatmp") || targetFolder.ContainsIgnoreCase("system32");
                        results.Add((targetFolder, hasReparsePoint, isSpecialPath));
                    }
                } catch (UnauthorizedAccessException) {
                    Logger.Info($"快速查找 Java 时没有权限（{folder}）");
                } catch (Exception ex) {
                    ex.ThrowIfCanceled();
                    Logger.Warn(ex, $"快速查找 Java 时出错（{folder}）");
                }
            }, cancellationToken).NoCapture();
        }, cancellationToken, progress?.SplitTo(0.2)).NoCapture();
        if (!results.Any()) return [];

        if (results.All(r => r.HasReparsePoint)) {
            Logger.Warn("找到的所有 Java 都包含重解析点，保留全部结果");
        } else if (results.Any(r => r.HasReparsePoint)) {
            Logger.Warn($"找到的以下 Java 包含重解析点，将它们从列表中排除：{results.Where(r => r.HasReparsePoint).Select(r => r.Folder).Join('、')}");
            results = [..results.Where(r => !r.HasReparsePoint)];
        }

        if (results.All(r => r.IsSpecialPath)) {
            Logger.Warn("找到的所有 Java 都包含特殊路径，保留全部结果");
        } else if (results.Any(r => r.IsSpecialPath)) {
            Logger.Warn($"找到的以下 Java 包含特殊路径，将它们从列表中排除：{results.Where(r => r.IsSpecialPath).Select(r => r.Folder).Join('、')}");
            results = [..results.Where(r => !r.IsSpecialPath)];
        }

        List<Java> javaList = results.Select(r => r.Folder).Distinct(StringComparer.OrdinalIgnoreCase).Select(folder => new Java(folder)).ToList();
        javaList = (await JavaUtils.CheckAllAsync(javaList, cancellationToken, progress?.SplitTo(1)).NoCapture()).ToList();
        javaList = await JavaUtils.SortAsync(javaList, cancellationToken).NoCapture();
        Logger.Info($"快速查找 Java 完成，共有 {javaList.Count} 个候选：{javaList.Join('、')}");
        return javaList;
    }

    #endregion

}
