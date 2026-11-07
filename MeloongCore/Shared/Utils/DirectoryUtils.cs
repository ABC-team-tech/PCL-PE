namespace MeloongCore;
public static class DirectoryUtils {

    public static void Create(string folder) {
        if (DirectoryUtils.Exists(folder)) return;
        Logger.Trace($"新建文件夹：{folder}");
        Directory.CreateDirectory(PathUtils.ForApi(folder));
    }

    public static string CreateRandom() {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        DirectoryUtils.Create(tempDir);
        return tempDir;
    }

    public static bool Exists(string folder)
        => Directory.Exists(PathUtils.ForApi(folder));

    public static DirectoryInfo GetInfo(string folder)
        => new(PathUtils.ForApi(folder));

    public static IEnumerable<string> EnumerateFiles(string folder, bool includeSubDirectories = false, string searchPattern = "*") {
        if (!DirectoryUtils.Exists(folder)) return [];
        return Directory.EnumerateFiles(PathUtils.ForApi(folder), searchPattern, includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).
            Select(PathUtils.RemoveExtendedPrefix);
    }

    public static IEnumerable<string> EnumerateDirectories(string folder, bool includeSubDirectories = false, string searchPattern = "*") {
        if (!DirectoryUtils.Exists(folder)) return [];
        return Directory.EnumerateDirectories(PathUtils.ForApi(folder), searchPattern, includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).
            Select(PathUtils.RemoveExtendedPrefix);
    }

    public static bool IsEmpty(string folder) {
        if (!DirectoryUtils.Exists(folder)) return true;
        return !Directory.EnumerateFileSystemEntries(PathUtils.ForApi(folder)).Any();
    }

    public static void Copy(string sourceFolder, string destFolder) {
        sourceFolder = PathUtils.ForCompare(sourceFolder);
        destFolder = PathUtils.ForCompare(destFolder);
        if (string.CompareOrdinal(sourceFolder, destFolder) == 0) {

            Logger.Trace($"复制文件夹到自身，不执行操作：{sourceFolder} → {destFolder}");
        } else if (PathUtils.IsSame(sourceFolder, destFolder)) {

            Logger.Trace($"复制文件夹到自身，但大小写不同，等效于重命名文件夹：{sourceFolder} → {destFolder}");
            DirectoryUtils.Move(sourceFolder, destFolder);
        } else {

            Logger.Trace($"复制文件夹：{sourceFolder} → {destFolder}");
            foreach (var file in DirectoryUtils.EnumerateFiles(sourceFolder, includeSubDirectories: true).ToList()) FileUtils.Copy(file, file.Replace(sourceFolder, destFolder));
        }
    }

    public static void Move(string sourceFolder, string destFolder) {
        SafetyCheck(sourceFolder);
        sourceFolder = PathUtils.ForCompare(sourceFolder);
        destFolder = PathUtils.ForCompare(destFolder);
        if (string.CompareOrdinal(sourceFolder, destFolder) == 0) {

            Logger.Trace($"剪切文件夹到自身，不执行操作：{sourceFolder} → {destFolder}");
        } else if (PathUtils.IsSame(sourceFolder, destFolder)) {

            Logger.Trace($"剪切文件夹到自身，但大小写不同：{sourceFolder} → {destFolder}");
            var temp = Path.Combine(PathUtils.RemoveLastPart(sourceFolder), Path.GetRandomFileName());
            DirectoryUtils.Move(sourceFolder, temp);
            DirectoryUtils.Move(temp, destFolder);
        } else if (PathUtils.GetDiskName(sourceFolder) == PathUtils.GetDiskName(destFolder)) {

            Logger.Trace($"剪切文件夹到同一磁盘：{sourceFolder} → {destFolder}");
            DirectoryUtils.Delete(destFolder);
            Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
                FileUtils.SetReadOnly(sourceFolder, false);
                Directory.Move(PathUtils.ForApi(sourceFolder), PathUtils.ForApi(destFolder));
            });
        } else {

            Logger.Trace($"剪切文件夹到不同磁盘：{sourceFolder} → {destFolder}");
            DirectoryUtils.Copy(sourceFolder, destFolder);
            DirectoryUtils.Delete(sourceFolder);
        }
    }

    public static bool? Delete(string folder, bool toRecycleBin = false) {
        if (!DirectoryUtils.Exists(folder)) return null;
        Logger.Trace($"{(toRecycleBin ? "将文件夹删除到回收站" : "删除文件夹")}：{folder}");
        SafetyCheck(folder);

        if (toRecycleBin) {
            try {
                FileUtils.DeleteToRecycleBin(folder);
                return true;
            } catch (Exception ex) {
                Logger.Warn(ex, $"无法将文件夹删除到回收站，回退到永久删除：{folder}");
            }
        }

        DeleteInternal(folder);
        static void DeleteInternal(string folder) {
            try {
                folder = PathUtils.ForApi(folder);
                foreach (string filePath in DirectoryUtils.EnumerateFiles(folder)) FileUtils.Delete(filePath);
                foreach (string str in DirectoryUtils.EnumerateDirectories(folder)) DeleteInternal(str);
                Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
                    FileUtils.SetReadOnly(folder, false);
                    Directory.Delete(folder, true);
                });
            } catch (DirectoryNotFoundException ex) {
                if (DirectoryUtils.Exists(folder)) {
                    Logger.Warn(ex, $"该文件夹可能为孤立的符号链接，尝试直接删除（{folder}）");
                    FileUtils.SetReadOnly(folder, false);
                    Directory.Delete(folder);
                } else {
                    throw;
                }
            }
        }
        return false;
    }

    private static void SafetyCheck(string folder) {
        folder = PathUtils.ForCompare(folder);
        if (PathUtils.IsSame(folder, Path.GetPathRoot(folder)))
            throw new UnauthorizedAccessException($"不应操作磁盘根目录：{folder}");
        if (criticalFolders.Value.Any(f => PathUtils.IsParentOf(folder, f)))
            throw new UnauthorizedAccessException($"不应操作文件夹：{folder}");
    }
    private static readonly Lazy<HashSet<string>> criticalFolders = new(() => new(new[] {
        Paths.Base,
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
    }.Where(f => f.Contains(Path.VolumeSeparatorChar))));

}
