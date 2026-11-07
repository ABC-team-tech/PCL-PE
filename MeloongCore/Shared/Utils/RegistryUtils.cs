using Microsoft.Win32;
namespace MeloongCore;

public static class RegistryUtils {

    public const string NULL_STRING = "__NULL__";

    public static (object? result, bool exists) Read(string folder, string entry) {
        using RegistryKey? folderKey = GetRegistryKey(folder);
        var result = folderKey?.GetValue(entry);
        if (result is null) return (null, false);
        return (result is string str && str == NULL_STRING ? null : result, true);
    }

    public static object? TryRead(string folder, string entry, object? defaultValue = null) {
        try {
            var (result, exists) = Read(folder, entry);
            return exists ? result : defaultValue;
        } catch (Exception ex) {
            Logger.Warn(ex, $"读取注册表值失败（{folder}，{entry}）");
            return defaultValue;
        }
    }

    public static void Write(string folder, string entry, object? value) {
        using RegistryKey folderKey = GetRegistryKey(folder, create: true)!;
        folderKey.SetValue(entry, value ?? NULL_STRING);
    }

    public static bool Has(string folder, string entry) {
        using RegistryKey? folderKey = GetRegistryKey(folder);
        if (folderKey is null) return false;
        object notFound = new();
        return !ReferenceEquals(folderKey.GetValue(entry, notFound), notFound);
    }

    public static void Delete(string folder, string entry) {
        using RegistryKey? folderKey = GetRegistryKey(folder, writable: true);
        folderKey?.DeleteValue(entry, throwOnMissingValue: false);
    }

    private static RegistryKey? GetRegistryKey(string fullPath, bool writable = false, bool create = false) {
        using RegistryKey key = fullPath.BeforeFirst("\\").ToUpperInvariant() switch {
            "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
            "HKEY_CURRENT_USER" => Registry.CurrentUser,
            "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKEY_USERS" => Registry.Users,
            "HKEY_CURRENT_CONFIG" => Registry.CurrentConfig,
            _ => throw new ArgumentException($"注册表路径有误（{fullPath}）", nameof(fullPath))
        };
        string subKey = fullPath.AfterFirst("\\");
        return create ? key.CreateSubKey(subKey) : key.OpenSubKey(subKey, writable);
    }

}
