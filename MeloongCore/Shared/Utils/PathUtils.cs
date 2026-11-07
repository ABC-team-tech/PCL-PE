namespace MeloongCore;
public static class PathUtils {

    #region 分隔符

    public static string AddSlashSuffix(string folder)
        => folder + (folder.EndsWithF(Path.DirectorySeparatorChar) || folder.EndsWithF(Path.AltDirectorySeparatorChar) ? "" : Path.DirectorySeparatorChar);

    public static string RemoveSlashSuffix(string folder)
        => folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    #endregion

    #region 长路径

    public static string ToShortPath(string pathName) {

        if (string.IsNullOrEmpty(pathName)) return pathName;
        if (!pathName.Contains(":") || pathName.Contains("://")) return pathName;
        if (pathName.Length <= 220) return pathName;

        pathName = PathUtils.ForCompare(pathName);

        string pathToKeep = "";
        string pathToShorten = pathName;
        if (FileUtils.Exists(pathName)) {
            var fileName = PathUtils.GetLastPart(pathName);
            if (fileName.Length >= 180) {
                Logger.Warn($"文件名过长，将为文件名生成短路径：{pathName}", LogBehavior.None);
            } else {
                pathToKeep = fileName;
                pathToShorten = PathUtils.RemoveLastPart(pathName);
            }
        }

        while (!DirectoryUtils.Exists(pathToShorten) && !FileUtils.Exists(pathToShorten)) {
            string? parentPath = Path.GetDirectoryName(pathToShorten);
            if (string.IsNullOrEmpty(parentPath) || PathUtils.IsSame(parentPath, pathToShorten)) return pathName;
            pathToKeep = Path.Combine(PathUtils.GetLastPart(pathToShorten), pathToKeep);
            pathToShorten = parentPath;
        }
        if (pathToKeep.Length >= 200) {
            Logger.Warn($"路径中的大部分内容都尚未建立，无法在内容不存在时缩短路径：{pathName} → {pathToShorten}", LogBehavior.None);
            return pathName;
        }

        Func<string, string> TryGetShortPath = static pathName => {
            char[] buffer = new char[220];
            int result = GetShortPathNameW(PathUtils.ForApi(pathName), buffer, buffer.Length);
            return result is 0 or > 220 ? pathName : new string(buffer, 0, result);
        };
        var shortPath = TryGetShortPath(pathToShorten);

        if (Path.Combine(shortPath, pathToKeep).Length > 220) {
            Logger.Warn($"{nameof(GetShortPathNameW)} 未能有效缩短路径，将尝试生成新的短名称：{pathToShorten} → {shortPath}", LogBehavior.None);
            var candidates = new List<(string Path, string Name)>();
            string? candidatePath = pathToShorten;
            while (!string.IsNullOrEmpty(candidatePath)) {
                string candidateName = PathUtils.GetLastPart(candidatePath);
                if (candidateName.Length > 12 &&
                    string.Equals(PathUtils.GetLastPart(TryGetShortPath(candidatePath)), candidateName, StringComparison.OrdinalIgnoreCase)) {
                    candidates.Add((candidatePath, candidateName));
                }
                candidatePath = Path.GetDirectoryName(candidatePath);
            }
            foreach (var candidate in candidates.OrderByDescending(candidate => candidate.Name.Length)) {
                using var handle = CreateFileW(PathUtils.ForApi(candidate.Path), 0x40010000, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero);
                if (handle.IsInvalid) continue;
                bool created = false;
                try {
                    unsafe {
                        fixed (char* originalNameBuffer = candidate.Name) {
                            char* shortNameBuffer = stackalloc char[12];
                            var name = new UnicodeString {Length = (ushort)(candidate.Name.Length * 2), MaximumLength = (ushort)((candidate.Name.Length + 1) * 2), Buffer = originalNameBuffer};
                            var shortName = new UnicodeString {MaximumLength = 24, Buffer = shortNameBuffer};
                            var context = new GenerateNameContext();
                            for (int i = 0; i < 20; i++) {
                                shortName.Length = 0;
                                if (RtlGenerate8dot3Name(ref name, 0, ref context, ref shortName) != 0) break;
                                string generatedName = new(shortName.Buffer, 0, shortName.Length / 2);
                                if (SetFileShortNameW(handle, generatedName)) { created = true; break; }
                                if (Marshal.GetLastWin32Error() != 183) break;
                            }
                        }
                    }
                } catch (EntryPointNotFoundException) {}
                if (!created) continue;
                shortPath = TryGetShortPath(pathToShorten);
                if (Path.Combine(shortPath, pathToKeep).Length <= 220) break;
            }
        }
        return PathUtils.RemoveSlashSuffix(PathUtils.RemoveExtendedPrefix(Path.Combine(shortPath, pathToKeep)));
    }
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct UnicodeString { public ushort Length, MaximumLength; public char* Buffer; }
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct GenerateNameContext { public ushort Checksum; public byte ChecksumInserted, NameLength; public fixed char NameBuffer[8]; public uint ExtensionLength; public fixed char ExtensionBuffer[4]; public uint LastIndexValue; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetShortPathNameW(string lpszLongPath, [Out] char[]? buffer, int cchBuffer);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFileW(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetFileShortNameW(Microsoft.Win32.SafeHandles.SafeFileHandle file, string shortName);
    [DllImport("ntdll.dll")]
    private static extern int RtlGenerate8dot3Name(ref UnicodeString name, byte allowExtendedCharacters, ref GenerateNameContext context, ref UnicodeString name8dot3);

    public static string ToExtendedFormat(string pathName) {
        if (string.IsNullOrWhiteSpace(pathName)) return pathName;
        if (!pathName.Contains(@":")) return pathName;
        if (pathName.StartsWithF(@"\\?\")) return pathName;
        pathName = RemoveSlashSuffix(pathName).Replace('/', '\\');
        if (pathName.StartsWithF(@"\\")) {
            return $@"\\?\UNC\{pathName[2..]}";
        } else {
            return $@"\\?\{pathName}";
        }
    }

    public static string RemoveExtendedPrefix(string pathName) {
        if (pathName.StartsWithF(@"\\?\UNC\")) return @"\\" + pathName.AfterLast(@"\\?\UNC\");
        if (pathName.StartsWithF(@"\\?\")) return pathName.AfterLast(@"\\?\");
        return pathName;
    }

    #endregion

    #region 路径处理

    public static string RemoveLastPart(string pathName) {
        if (pathName.Contains("://")) {
            pathName = PathUtils.RemoveSlashSuffix(pathName.BeforeFirst("#").BeforeFirst("?"));
        }
        return PathUtils.RemoveSlashSuffix(pathName).BeforeLastOfAny([@"\", "/"]);
    }

    public static string GetLastPart(string pathName) {
        if (pathName.Contains("://")) {
            pathName = PathUtils.RemoveSlashSuffix(pathName.BeforeFirst("#").BeforeFirst("?"));
        } else {
            pathName = PathUtils.RemoveSlashSuffix(pathName);
        }
        return pathName.AfterLastOfAny([@"\", "/"]);
    }

    public static string GetFileNameWithoutExtension(string pathName)
        => GetLastPart(pathName).BeforeLast(".");

    public static string GetExtension(string pathName) {
        var name = GetLastPart(pathName);
        return name.Contains(".") ? name.AfterLast(".").Lower() : "";
    }

    public static char? GetDiskName(string? pathName) {
        if (string.IsNullOrEmpty(pathName)) return null;
        pathName = PathUtils.RemoveExtendedPrefix(pathName!);
        if (pathName.StartsWithF(@"\\") || pathName.StartsWithF("/")) return null;
        return char.ToUpper(pathName[0]);
    }

    public static string ForCompare(string pathName) {
        try {
            return PathUtils.RemoveSlashSuffix(PathUtils.RemoveExtendedPrefix(Path.GetFullPath(pathName)).Replace("/", @"\"));
        } catch (ArgumentException ex) {
            throw new ArgumentException($"路径不正确：{pathName}", ex);
        }
    }

    public static bool IsSame(string? path1, string? path2) {
        return PathUtils.Compare(path1, path2) == 0;
    }

    public static int Compare(string? path1, string? path2) {
        if (string.IsNullOrEmpty(path1)) return string.IsNullOrEmpty(path2) ? 0 : -1;
        if (string.IsNullOrEmpty(path2)) return 1;
        return string.Compare(PathUtils.ForCompare(path1!), PathUtils.ForCompare(path2!), StringComparison.OrdinalIgnoreCase);
    }

    public static string ForApi(string pathName) {
        pathName = PathUtils.ToExtendedFormat(pathName);
        if (pathName.EndsWithF(":")) pathName += @"\";
        if (pathName.EndsWithF(@":\")) return pathName;
        return PathUtils.RemoveSlashSuffix(pathName);
    }

    #endregion

    public static bool IsParentOf(string parentPath, string childPath) {
        parentPath = PathUtils.ForCompare(parentPath);
        childPath = PathUtils.ForCompare(childPath);
        return childPath.StartsWithF(parentPath + @"\", true) || PathUtils.IsSame(childPath, parentPath);
    }

}

public static class Paths {

    public static string Base => PathUtils.AddSlashSuffix(AppContext.BaseDirectory);

    public static string BaseThenName => PathUtils.AddSlashSuffix(Base + Main.AppName);

    public static string AppData => PathUtils.AddSlashSuffix(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    public static string AppDataThenName => PathUtils.AddSlashSuffix(AppData + Main.AppName);
}
