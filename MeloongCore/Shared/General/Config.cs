using System.Security.Cryptography;
namespace MeloongCore;

#region ConfigProvider | 配置提供器

public interface IConfigProvider {
    void Set<T>(string key, T? value, bool encrypted);
    void Remove(string key);
    T? Read<T>(string key, T? defaultValue, bool encrypted);
    bool HasValue(string key);
    void DiscardCache();
}

public class JsonConfigProvider : IConfigProvider {
    public static readonly ConcurrentBag<WeakReference<JsonConfigProvider>> allProviders = [];
    private readonly string filePath;

#pragma warning disable CS8618
    public JsonConfigProvider(string filePath) {
#pragma warning restore CS8618
        this.filePath = filePath;
        allProviders.Add(new(this));
        saveWorker = new(Save, 500, RateLimitMode.WaitUnillNoNewInvoke);
        InitJson();
    }

    private Lazy<JObject> json;

    private readonly ConcurrentDictionary<string, object?> cache = new();
    private void InitJson() {
        json = new(() => {
            try {
                return FileUtils.Exists(filePath) ? ((JObject?) FileUtils.ReadAsJson(filePath) ?? []) : [];
            } catch (Exception ex) {
                Logger.Error(ex, $"读取配置文件失败（{filePath}）", LogBehavior.Alert);
                return [];
            }
        });
    }

    public void Set<T>(string key, T? value, bool encrypted) {
        cache[key] = value;
        lock (this) {
            if (value is null) {

                json.Value[key] = JValue.CreateNull();
                Logger.Trace($"配置已修改：{key} = null（{filePath}）");
            } else if (encrypted) {

                throw new NotImplementedException("加密需要改为使用识别码，现在尚未实现。");

            } else {

                JToken token = JToken.FromObject(value);
                json.Value[key] = token;
                Logger.Trace($"配置已修改：{key} = {token.ToString(Formatting.None)}（{filePath}）");
            }
        }
        MakeDirty();
    }
    public T? Read<T>(string key, T? defaultValue, bool encrypted) {
        if (cache.TryGetValue(key, out var cachedValue)) return (T?) cachedValue;
        JToken entry;
        lock (this) {
            if (!json.Value.TryGetValue(key, out entry!)) return defaultValue;
            entry = entry.DeepClone();
        }
        try {
            T? result;
            if (entry.Type is JTokenType.Null or JTokenType.Undefined) {
                result = default;
            } else if (encrypted) {
                string plainText = CryptographyUtils.AesDecrypt(entry.Value<string>())!;
                result = typeof(T) == typeof(string) ? (T) (object) plainText : JsonConvert.DeserializeObject<T>(plainText);
            } else {
                result = entry.ToObject<T>();
            }
            cache[key] = result;
            return result;
        } catch (CryptographicException ex) {
            Logger.Error(ex, $"解密配置失败，该配置将被重置（{key}）", LogBehavior.Alert);
            Remove(key);
            return defaultValue;
        }
    }

    public bool HasValue(string key) {
        lock (this) return json.Value.ContainsKey(key);
    }
    public void Remove(string key) {
        lock (this) json.Value.Remove(key);
        cache.TryRemove(key, out _);
        MakeDirty();
    }
    public void DiscardCache() {
        lock (this) InitJson();
        cache.Clear();
    }

    private readonly RateLimitedWorker saveWorker;
    private bool isDirty = false;
    public void MakeDirty() {
        lock (this) isDirty = true;
        saveWorker.Start();
    }

    public void Save() {
        lock (this) {
            if (!isDirty) return;
            FileUtils.WriteAtomic(filePath, json.Value.ToString(Formatting.Indented));
            isDirty = false;
        }
    }
}

public static class ConfigProviders {
    public static JsonConfigProvider Default = new(Path.Combine(Paths.AppDataThenName, "config.json"));
}

#endregion

#region ConfigEntry | 单个配置项

public class PreviewGetEventArgs<T>(T? value) : EventArgs {
    public T? Value { get; set; } = value;
}

public class ConfigEntry<T>(string key, T? defaultValue, bool encrypted = false, IConfigProvider? provider = null) {
    public IConfigProvider Provider { get; } = provider ?? ConfigProviders.Default;

    public event Action<T?>? Changed;

    public event Action<PreviewGetEventArgs<T>, IConfigProvider>? PreviewGet;

    public void Edit(Func<T?, T?> editFunc) => Set(editFunc(Get()));
    public void Edit(RefAction editAction) {
        var current = Get();
        editAction(ref current);
        Set(current);
    }
    public delegate void RefAction(ref T? value);

    public void Set(T? value) {
        Provider.Set(key, value, encrypted);
        Changed?.Invoke(value);
    }
    public void Reset() {
        if (!Provider.HasValue(key)) return;
        Provider.Remove(key);
        Changed?.Invoke(defaultValue);
    }
    public T? Get() {
        PreviewGetEventArgs<T> args = new(Provider.Read(key, defaultValue, encrypted));
        PreviewGet?.Invoke(args, Provider);
        return args.Value;
    }
    public bool HasValue() => Provider.HasValue(key);
}

public class DynamicConfigEntry<T>(string key, T? defaultValue, bool encrypted = false) {

    public event Action<T?>? Changed;

    public event Action<PreviewGetEventArgs<T>, IConfigProvider>? PreviewGet;

    public void Edit(Func<T?, T?> editFunc, IConfigProvider provider) => Set(editFunc(Get(provider)), provider);
    public void Edit(RefAction editAction, IConfigProvider provider) {
        var current = Get(provider);
        editAction(ref current);
        Set(current, provider);
    }
    public delegate void RefAction(ref T? value);

    public void Set(T? value, IConfigProvider provider) {
        provider.Set(key, value, encrypted);
        Changed?.Invoke(value);
    }
    public void Reset(IConfigProvider provider) {
        if (!provider.HasValue(key)) return;
        provider.Remove(key);
        Changed?.Invoke(defaultValue);
    }
    public T? Get(IConfigProvider provider) {
        PreviewGetEventArgs<T> args = new(provider.Read(key, defaultValue, encrypted));
        PreviewGet?.Invoke(args, provider);
        return args.Value;
    }

    public bool HasValue(IConfigProvider provider) => provider.HasValue(key);
}

#endregion

public static class ConfigUtils {

    public static void SaveAll() {
        foreach (var weakRef in JsonConfigProvider.allProviders) {
            if (weakRef.TryGetTarget(out var provider)) provider.Save();
        }
    }

    private static readonly Lazy<JObject> secret = new(() => {
        string filePath = Path.Combine(Paths.AppData, "Meloong", "Dev", "secret.json");
        try {
            return FileUtils.Exists(filePath) ? JObject.Parse(FileUtils.ReadAsString(filePath)) : [];
        } catch {

            throw new InvalidDataException($"读取密钥文件失败（{filePath}），请检查文件内容与加密格式。");
        }
    });

    public static string GetSecret(string key) {
        string value = secret.Value.Value<string>(key) ?? throw new KeyNotFoundException($"未找到密钥：{key}");
        try {
            return CryptographyUtils.AesDecrypt(value)!;
        } catch {
            throw new InvalidDataException($"读取密钥失败（{key}），请检查文件内容与加密格式。");
        }
    }

}
