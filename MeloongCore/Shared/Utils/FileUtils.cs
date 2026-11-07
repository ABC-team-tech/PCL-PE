using System.IO.Compression;

namespace MeloongCore;
public static class FileUtils {

    #region 读取

    public static Stream ReadAsStream(string filePath, Type? type = null) {
        if (type is null) {
            return Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, func: _
                => new FileStream(PathUtils.ForApi(filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        } else {
            return type.Assembly.GetManifestResourceStream(filePath) ?? throw new FileNotFoundException($"Embedded resource \"{filePath}\" was not found.");
        }
    }

    public static byte[] ReadAsBytes(string filePath, Type? type = null) {
        using Stream fs = FileUtils.ReadAsStream(filePath, type);
        using MemoryStream ms = new();
        fs.CopyTo(ms);
        return ms.ToArray();
    }

    public static string ReadAsString(string filePath, Encoding? encoding = null, Type? type = null)
        => FileUtils.ReadAsBytes(filePath, type).GetString(encoding);

    public static string? TryReadAsString(string filePath, Encoding? encoding = null, Type? type = null) {
        try {
            if (type == null && !FileUtils.Exists(filePath)) return null;
            return FileUtils.ReadAsBytes(filePath, type).GetString(encoding);
        } catch {
            return null;
        }
    }

    public static string[] ReadAsLines(string filePath, bool skipEmptyLines = false, Encoding? encoding = null, Type? type = null)
        => FileUtils.ReadAsString(filePath, encoding, type).SplitLines(skipEmptyLines);

    public static string[] TryReadAsLines(string filePath, bool skipEmptyLines = false, Encoding? encoding = null, Type? type = null)
        => FileUtils.TryReadAsString(filePath, encoding, type)?.SplitLines(skipEmptyLines) ?? [];

    public static JToken? ReadAsJson(string filePath, Encoding? encoding = null, Type? type = null)
        => FileUtils.ReadAsString(filePath, encoding, type).DeserializeJson();

    public static T? ReadAsJson<T>(string filePath, Encoding? encoding = null, Type? type = null) where T : JToken
        => FileUtils.ReadAsString(filePath, encoding, type).DeserializeJson<T>();

    #endregion

    #region 创建 / 写入

    public static void Write(string filePath, string text, Encoding? encoding = null)
        => FileUtils.Write(filePath, (encoding ?? new UTF8Encoding()).GetBytes(text));

    public static void Write(string filePath, IEnumerable<byte> content)
        => FileUtils.Write(filePath, [.. content]);

    public static void Write(string filePath, byte[] content) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(filePath));
        Logger.Trace($"写入文件：{filePath}（{content.Length} 字节）");
        Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
            File.WriteAllBytes(PathUtils.ForApi(filePath), content);
        });
    }

    public static void WriteAtomic(string filePath, string text, Encoding? encoding = null) =>
        FileUtils.WriteAtomic(filePath, (encoding ?? new UTF8Encoding()).GetBytes(text));

    public static void WriteAtomic(string filePath, byte[] content) {
        string tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        try {
            FileUtils.Write(tempPath, content);
            if (FileUtils.Exists(filePath)) {
                File.Replace(PathUtils.ForApi(tempPath), PathUtils.ForApi(filePath), null, true);
            } else {
                File.Move(PathUtils.ForApi(tempPath), PathUtils.ForApi(filePath));
            }
        } finally {
            FileUtils.Delete(tempPath);
        }
    }

    public static void Write(string filePath, Stream stream) {
        Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
            FileUtils.Delete(filePath);
            using FileStream fileStream = FileUtils.CreateAsStream(filePath);
            if (stream.CanSeek && stream.Position != 0) stream.Seek(0, SeekOrigin.Begin);
            Logger.Trace($"写入文件：{filePath}（{stream.GetType().Name}{(stream.CanSeek ? $" {stream.Length} 字节" : "")}）");
            stream.CopyTo(fileStream);
        });
    }

    public static FileStream CreateAsStream(string filePath) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(filePath));
        Logger.Trace($"创建文件流：{filePath}");
        return Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, func: _ => {
            FileUtils.Delete(filePath);
            return new FileStream(PathUtils.ForApi(filePath), FileMode.Create);
        });
    }

    public static string CreateRandom()
        => Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, func: _
            => Path.GetTempFileName());

    public static void ExtractResources(string logicalName, string localFilePath, Type type) {
        var content = ReadAsBytes(logicalName, type);
        if (Exists(localFilePath) && CryptographyUtils.ComputeFileHash(localFilePath) == CryptographyUtils.ComputeHash(content)) return;
        Logger.Info($"将资源写入到文件：{logicalName} → {localFilePath}");
        Write(localFilePath, content);
    }

    #endregion

    #region 复制 / 剪切

    public static void Copy(string sourceFilePath, string destFilePath) {
        sourceFilePath = PathUtils.ForCompare(sourceFilePath);
        destFilePath = PathUtils.ForCompare(destFilePath);
        if (string.CompareOrdinal(sourceFilePath, destFilePath) == 0) {

            Logger.Trace($"复制文件到自身，不执行操作：{sourceFilePath} → {destFilePath}");
        } else if (PathUtils.IsSame(sourceFilePath, destFilePath)) {

            Logger.Trace($"复制文件到自身，但大小写不同，等效于重命名文件：{sourceFilePath} → {destFilePath}");
            FileUtils.Move(sourceFilePath, destFilePath);
        } else {

            DirectoryUtils.Create(PathUtils.RemoveLastPart(destFilePath));
            Logger.Trace($"复制文件：{sourceFilePath} → {destFilePath}");
            if (FileUtils.Exists(destFilePath)) FileUtils.SetReadOnly(destFilePath, false);
            Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _
                => File.Copy(PathUtils.ForApi(sourceFilePath), PathUtils.ForApi(destFilePath), true));
        }
    }

    public static void Move(string sourceFilePath, string destFilePath, bool toRecycleBin = false) {
        sourceFilePath = PathUtils.ForCompare(sourceFilePath);
        destFilePath = PathUtils.ForCompare(destFilePath);
        if (string.CompareOrdinal(sourceFilePath, destFilePath) == 0) {

            Logger.Trace($"剪切文件到自身，不执行操作：{sourceFilePath} → {destFilePath}");
        } else if (PathUtils.IsSame(sourceFilePath, destFilePath)) {

            Logger.Trace($"剪切文件到自身，但大小写不同：{sourceFilePath} → {destFilePath}");
            Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
                var temp = Path.Combine(PathUtils.RemoveLastPart(sourceFilePath), Path.GetRandomFileName());
                FileUtils.SetReadOnly(sourceFilePath, false);
                if (FileUtils.Exists(destFilePath)) FileUtils.SetReadOnly(destFilePath, false);
                File.Move(PathUtils.ForApi(sourceFilePath), PathUtils.ForApi(temp));
                File.Move(PathUtils.ForApi(temp), PathUtils.ForApi(destFilePath));
            });
        } else {

            DirectoryUtils.Create(PathUtils.RemoveLastPart(destFilePath));
            FileUtils.Delete(destFilePath, toRecycleBin);
            Logger.Trace($"剪切文件：{sourceFilePath} → {destFilePath}");
            Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
                FileUtils.SetReadOnly(sourceFilePath, false);
                if (FileUtils.Exists(destFilePath)) FileUtils.SetReadOnly(destFilePath, false);
                File.Move(PathUtils.ForApi(sourceFilePath), PathUtils.ForApi(destFilePath));
            });
        }
    }

    #endregion

    #region 删除

    public static bool? Delete(string filePath, bool toRecycleBin = false) {
        if (!FileUtils.Exists(filePath)) return null;
        Logger.Trace($"{(toRecycleBin ? "将文件删除到回收站" : "删除文件")}：{filePath}");

        if (toRecycleBin) {
            try {
                DeleteToRecycleBin(filePath);
                return true;
            } catch (Exception ex) {
                Logger.Warn(ex, $"无法将文件删除到回收站，回退到永久删除：{filePath}");
            }
        }

        Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
            FileUtils.SetReadOnly(filePath, false);
            File.Delete(PathUtils.ForApi(filePath));
        });
        return false;
    }

    internal static void DeleteToRecycleBin(string target) {

        void Run() {
            IShellItem? item = null;
            IFileOperation? op = null;
            try {
                var iid = typeof(IShellItem).GUID;
                Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(PathUtils.RemoveExtendedPrefix(target), IntPtr.Zero, ref iid, out item));

                op = (IFileOperation) new FileOperation();
                op.SetOperationFlags(0x0040 | 0x0010 | 0x0004);
                op.DeleteItem(item, IntPtr.Zero);
                op.PerformOperations();
                op.GetAnyOperationsAborted(out bool aborted);
                if (aborted) throw new OperationCanceledException("Delete operation was aborted.");
            } finally {
                if (op != null && Marshal.IsComObject(op)) Marshal.FinalReleaseComObject(op);
                if (item != null && Marshal.IsComObject(item)) Marshal.FinalReleaseComObject(item);
            }
        }

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) {
            Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => Run());
        } else {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo? internalEx = null;
            var thread = new Thread(() => {
                try {
                    Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException or COMException, action: _ => Run());
                } catch (Exception ex) {
                    internalEx = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex);
                }
            }) { IsBackground = true, Name = nameof(DeleteToRecycleBin) };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            internalEx?.Throw();
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItem { }
    [ComImport, Guid("3AD05575-8857-4850-9277-11B85BDB8E09")]
    class FileOperation { }
    [ComImport, Guid("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IFileOperation {
        void Advise(IntPtr pfops, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOperationFlags(uint dwOperationFlags);
        void SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string pszMessage);
        void SetProgressDialog(IntPtr popd);
        void SetProperties(IntPtr pproparray);
        void SetOwnerWindow(IntPtr hwndOwner);
        void ApplyPropertiesToItem(IShellItem psiItem);
        void ApplyPropertiesToItems(IntPtr punkItems);
        void RenameItem(IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IntPtr pfopsItem);
        void RenameItems(IntPtr pUnkItems, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        void MoveItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IntPtr pfopsItem);
        void MoveItems(IntPtr punkItems, IShellItem psiDestinationFolder);
        void CopyItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszCopyName, IntPtr pfopsItem);
        void CopyItems(IntPtr punkItems, IShellItem psiDestinationFolder);
        void DeleteItem(IShellItem psiItem, IntPtr pfopsItem);
        void DeleteItems(IntPtr punkItems);
        void NewItem(IShellItem psiDestinationFolder, uint dwFileAttributes, [MarshalAs(UnmanagedType.LPWStr)] string pszName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, IntPtr pfopsItem);
        void PerformOperations();
        void GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)] out bool pfAnyOperationsAborted);
    }

    #endregion

    #region 压缩 / 解压

    public static ZipArchive OpenZip(string zipFilePath, CancellationToken c = default) {
        ZipArchive TryOpen(Encoding encoding) {
            Logger.Trace($"尝试以 {encoding.EncodingName} 编码打开压缩包：{zipFilePath}");
            var result = ZipFile.Open(PathUtils.ForApi(zipFilePath), ZipArchiveMode.Read, encoding);
            try {
                _ = result.Entries;
                return result;
            } catch {
                result.Dispose();
                throw;
            }
        }
        return Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), maxAttempts:10, isRetryAllowed: ex => ex is IOException, cancellationToken: c, func: _ => {
            try {
                try {
                    return TryOpen(new UTF8Encoding(false, true));
                } catch (DecoderFallbackException) {
                    return TryOpen(Encoding.GetEncoding("GB18030"));
                }
            } catch (InvalidDataException ex) {
                throw new InvalidDataException($"文件不是压缩包，或者文件已损坏（{zipFilePath}）", ex);
            }
        });

    }

    public static void ExtractToDirectory(string compressionFile, string outputDirectory, CancellationToken c = default, ProgressProvider? p = null) {
        compressionFile = PathUtils.ForApi(compressionFile);
        DirectoryUtils.Create(outputDirectory);

        if (PathUtils.GetExtension(compressionFile) == "gz") {
            string outFilePath = Path.Combine(outputDirectory, PathUtils.GetFileNameWithoutExtension(compressionFile));
            Logger.Trace($"解压 gz 文件：{compressionFile} → {outFilePath}");
            using var fileStream = FileUtils.ReadAsStream(compressionFile);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            FileUtils.Write(outFilePath, gzipStream);
            p?.Finish();
            return;
        }

        using var archive = FileUtils.OpenZip(compressionFile, c);
        var entries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
        int totalCount = entries.Count;
        int doneCount = 0;
        Logger.Trace($"解压 zip 文件：{compressionFile} → {outputDirectory}（共 {totalCount} 项）");
        foreach (var entry in entries) {
            c.ThrowIfCancellationRequested();
            doneCount++;
            if (totalCount > 0) p?.Set((double) doneCount / totalCount);

            string outputFilePath = Path.Combine(outputDirectory, entry.FullName);
            if (!PathUtils.IsParentOf(outputDirectory, outputFilePath))
                throw new InvalidDataException($"Zip 文件项 {entry.FullName} 的路径在压缩包之外，这可能导致安全问题");

            using var entryStream = entry.Open();
            FileUtils.Write(outputFilePath, entryStream);
        }
    }

    public static void CreateZipFromDirectory(string outputFullPath, string sourceDirectory) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(outputFullPath));
        FileUtils.Delete(outputFullPath);
        Logger.Trace($"将文件夹中的内容压缩为 zip 文件：{sourceDirectory} → {outputFullPath}");
        Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _
            => ZipFile.CreateFromDirectory(PathUtils.ForApi(sourceDirectory), PathUtils.ForApi(outputFullPath)));
    }

    public static void CreateZipFromFiles(string outputFullPath, params string[] sourceFiles) {
        var sources = new Dictionary<string, string>();
        foreach (var source in sourceFiles) {
            string fileName = PathUtils.GetLastPart(source);
            if (sources.ContainsKey(fileName)) throw new ArgumentException($"尝试将多个同文件名的文件放进压缩包中（{fileName}）", nameof(sourceFiles));
            sources.Add(fileName, source);
        }
        FileUtils.CreateZipFromFiles(outputFullPath, sources);
    }

    public static void CreateZipFromFiles(string outputFullPath, IDictionary<string, string> sources) {
        DirectoryUtils.Create(PathUtils.RemoveLastPart(outputFullPath));
        FileUtils.Delete(outputFullPath);
        using var archive = Retrier.Attempt(delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, func: _
            => ZipFile.Open(PathUtils.ForApi(outputFullPath), ZipArchiveMode.Create));
        Logger.Trace($"创建 zip 文件：{sources.Count} 个文件 → {outputFullPath}\n{sources.Select(p => $"- {p.Value} → {p.Key}").Join('\n')}");
        foreach (var pair in sources) archive.CreateEntryFromFile(PathUtils.ForApi(pair.Value), pair.Key.Replace('\\', '/'), CompressionLevel.Optimal);
    }

    #endregion

    public static bool Exists(string filePath)
        => File.Exists(PathUtils.ForApi(filePath));

    public static bool IsAvailable(string filePath) {
        if (!Exists(filePath)) return true;
        try {
            Retrier.Attempt(maxAttempts: 3, delay: _ => TimeSpan.FromMilliseconds(200), isRetryAllowed: ex => ex is IOException, action: _ => {
                using var stream = new FileStream(PathUtils.ForApi(filePath), FileMode.Open, FileAccess.Read, FileShare.None);
            });
            return true;
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            return false;
        }
    }

    public static FileInfo GetInfo(string path)
        => new(PathUtils.ForApi(path));

    public static void SetReadOnly(string path, bool readOnly) {
        path = PathUtils.ForApi(path);
        var attributes = File.GetAttributes(path);
        var newAttributes = readOnly
            ? attributes | FileAttributes.ReadOnly
            : attributes & ~FileAttributes.ReadOnly;
        if (attributes == newAttributes) return;
        Logger.Trace($"{(readOnly ? "添加" : "移除")}只读属性：{path}（原属性：{attributes}，新属性：{newAttributes}）");
        File.SetAttributes(path, newAttributes);
    }

}

public class FileChecker {

    public long ActualSize = -1;

    public long MinSize = -1;

    public string? Hash = null;

    public bool IsJson = false;

    public string? Check(string localPath) {
        try {
            var info = FileUtils.GetInfo(localPath);
            if (!info.Exists) return "文件不存在：" + localPath;

            long fileSize = info.Length;
            if (ActualSize >= 0 && ActualSize != fileSize) {
                return $"文件大小应为 {ActualSize} B，实际为 {fileSize} B" +
                    (fileSize < 2000 ? "，内容为：" + FileUtils.ReadAsString(localPath) : "");
            }
            if (MinSize >= 0 && MinSize > fileSize) {
                return $"文件大小应至少为 {MinSize} B，实际为 {fileSize} B" +
                    (fileSize < 2000 ? "，内容为：" + FileUtils.ReadAsString(localPath) : "");
            }

            string? hash = Hash;
            if (!string.IsNullOrEmpty(hash)) {
                hash = hash!.Lower();
                switch (hash.Length) {
                    case 32:
                        string md5 = CryptographyUtils.ComputeFileHash(localPath, CryptographyUtils.HashMethod.Md5);
                        if (hash != md5) return "文件 MD5 应为 " + hash + "，实际为 " + md5;
                        break;
                    case 40:
                        string sha1 = CryptographyUtils.ComputeFileHash(localPath, CryptographyUtils.HashMethod.Sha1);
                        if (hash != sha1) return "文件 SHA1 应为 " + hash + "，实际为 " + sha1;
                        break;
                    case 64:
                        string sha256 = CryptographyUtils.ComputeFileHash(localPath, CryptographyUtils.HashMethod.Sha256);
                        if (hash != sha256) return "文件 SHA256 应为 " + hash + "，实际为 " + sha256;
                        break;
                    case 128:
                        string sha512 = CryptographyUtils.ComputeFileHash(localPath, CryptographyUtils.HashMethod.Sha512);
                        if (hash != sha512) return "文件 SHA512 应为 " + hash + "，实际为 " + sha512;
                        break;
                    default:
                        return $"无法识别的 Hash 类型（长度为 {hash.Length}）：{hash}";
                }
            }

            if (IsJson) {
                string content = FileUtils.ReadAsString(localPath);
                if (content == "") return "读取到的文件为空";
                content.DeserializeJson();
            }

            return null;
        } catch (Exception ex) {
            Logger.Warn(ex, "检查文件出错");
            return ex.GetDisplay(false);
        }
    }
}
