using System.Globalization;

namespace PCLCS;

public sealed class McVersion {

    public string VanillaName {
        get => _vanillaName;
        set {
            _vanillaName = value ?? "Unknown";
            Vanilla = NameToVersion(_vanillaName);
        }
    }
    private string _vanillaName = "Unknown";

    public Version Vanilla = new(9999, 0, 0);

    public bool Reliable;

    public bool Valid => Vanilla.Major < 1000;

    public int Drop => Valid ? Vanilla.Major * 10 + Vanilla.Minor : 209;

    public ModLoaders ModLoaders => ModLoaderVersions.Keys.Aggregate(ModLoaders.None, (result, loader) => result | loader);

    public Dictionary<ModLoaders, string> ModLoaderVersions { get; } = [];

    public string? OptiFine;

    public bool OptiFineAsMod;

    public DateTime ReleaseTime = new(1970, 1, 1, 15, 0, 0);

    public bool IsFool;

    public bool IsSnapshot;

    public bool HasOptiFine => OptiFine is not null;

    public int OptiFineCode {
        get {
            if (OptiFine is null or "未知版本" || OptiFine.Length == 0) return 0;

            int result = char.ToUpperInvariant(OptiFine[0]) - 'A' + 1;

            result = result * 100 + (int.TryParse(OptiFine.Substring(1).RegexSeek(@"\d+"), out int mainVersion) ? mainVersion : 0);

            result *= 100;
            string? preview = OptiFine.RegexSeek(@"(?i)(pre|beta)\d*");
            if (preview is null) return result + 99;
            int sequence = int.TryParse(preview.RegexSeek(@"\d+"), out int value) ? value : 1;
            return result + (preview.StartsWithF("pre", true) ? 50 : 0) + sequence;
        }
    }

    public int ForgelikeCode {
        get {
            if (!ModLoaderVersions.TryGetValue(ModLoaders.Forge, out string? version)) {
                ModLoaderVersions.TryGetValue(ModLoaders.NeoForge, out version);
            }
            if (version is null or "未知版本") return 0;

            int[] segments = version.RegexSearch(@"\d+")
                .Select(segment => int.TryParse(segment, out int value) ? value : 0).ToArray();
            return segments.Length switch {
                >= 4 => segments[0] * 1000000 + segments[1] * 10000 + segments[3],
                3 => segments[0] * 1000000 + segments[1] * 10000 + segments[2],
                2 => segments[0] * 1000000 + segments[1] * 10000,
                1 => segments[0] * 1000000,
                _ => 0
            };
        }
    }

    public McVersion(string vanillaName, bool reliable = true) {
        Reliable = reliable;
        VanillaName = vanillaName;
        IsFool = IsFoolName(VanillaName);
        IsSnapshot = IsSnapshotName(VanillaName);
    }

    public McVersion(McComponents components) : this(components.MinecraftVersion) {
        OptiFine = components.OptiFineVersion?.AfterLast("HD_").AfterFirst("_");
        foreach (var loader in components.ModLoaderVersions) ModLoaderVersions[loader.Key] = loader.Value;
    }

    public McVersion(string instanceName, JObject json, JObject? jarVersionJson = null) {
        string jsonText = json.ToString(Formatting.None);
        DateTime releaseTime = new(1970, 1, 1, 15, 0, 0);
        try {
            if (json["releaseTime"] is JToken releaseTimeToken) releaseTime = releaseTimeToken.ToObject<DateTime>();
        } catch {
        }
        bool reliable = true;

        string vanillaName = (GetVanillaName(instanceName, json, jarVersionJson, releaseTime, ref reliable) ?? "Unknown").Replace("_unobfuscated", "").Replace(" Unobfuscated", "");
        Reliable = reliable;
        VanillaName = vanillaName;
        ReleaseTime = releaseTime;

        string jsonType = json.Value<string>("type") ?? "";

        OptiFine = jsonText.ContainsIgnoreCase("optifine") ? jsonText.RegexSeek(@"(?<=HD_U_)[^\"":/]+") ?? "未知版本": null;

        DateTime fixedReleaseTime = releaseTime.ToUniversalTime().AddHours(2);
        IsFool = jsonType == "fool" || IsFoolName(vanillaName) || (fixedReleaseTime.Month == 4 && fixedReleaseTime.Day == 1 && jsonType == "snapshot");
        IsSnapshot = IsSnapshotName(vanillaName) || instanceName.ContainsIgnoreCase("combat") || jsonType is "snapshot" or "pending" or "fool";
        if (!Reliable) Logger.Info($"无法完全确认 MC 版本号的版本：{VanillaName}");

        if (jsonText.ContainsIgnoreCase("liteloader")) {
            ModLoaderVersions[ModLoaders.LiteLoader] = jsonText.RegexSeek(@"(?<=com\.mumfrey:liteloader:)[^\"":/]+") ?? "未知版本";
        }

        if (jsonText.ContainsIgnoreCase("net.fabricmc:fabric-loader") || jsonText.ContainsIgnoreCase("org.quiltmc:quilt-loader")) {
            ModLoaderVersions[ModLoaders.Fabric] = jsonText.RegexSeek(@"(?<=(net\.fabricmc:fabric-loader:|org\.quiltmc:quilt-loader:))[0-9\.]+(\+build.[0-9]+)?")?
                                                        .Replace("+build", "") ?? "未知版本";
        } else if (jsonText.ContainsIgnoreCase("net.neoforge")) {

            ModLoaderVersions[ModLoaders.NeoForge] = FindArgumentValue(json, "--fml.neoForgeVersion") ?? FindArgumentValue(json, "--fml.forgeVersion") ?? "未知版本";
        } else if (jsonText.ContainsIgnoreCase("minecraftforge")) {
            ModLoaderVersions[ModLoaders.Forge] = jsonText.RegexSeek(@"(?<=forge:[0-9\.]+(?:_pre[0-9]*)?-)[0-9\.]+")
                                                    ?? jsonText.RegexSeek(@"(?<=net\.minecraftforge:minecraftforge:)[0-9\.]+")
                                                    ?? jsonText.RegexSeek(@"(?<=net\.minecraftforge:fmlloader:[0-9\.]+-)[0-9\.]+") ?? "未知版本";
        }
    }

    private static string? GetVanillaName(string instanceName, JObject json, JObject? jarVersionJson, DateTime releaseTime, ref bool reliable) {

        if (releaseTime.Year is > 2000 and < 2013) return "Old";

        if (json.Value<string>("type") == "pending") return "pending";

        if (json["clientVersion"] is JToken clientVersion) return clientVersion.ToString();

        if (json["patches"] is JArray patches) {
            JToken? gameVersion = patches.OfType<JObject>().FirstOrDefault(patch => patch.Value<string>("id") == "game")?["version"];
            if (gameVersion is not null) return gameVersion.ToString();
        }

        if (FindArgumentValue(json, "--fml.mcVersion") is { Length: > 0 } fmlVersion) return fmlVersion;

        string inherit = json["inheritsFrom"]?.ToString() ?? "";
        if (inherit.Length > 0) {
            string jar = json["jar"]?.ToString() ?? "";
            return jar.Length > 0 ? jar : inherit;
        }

        string? download = json["downloads"]?.ToString(Formatting.None).RegexSeek(@"(?<=launcher.mojang.com/mc/game/)[^/]*");
        if (download is not null) return download;

        string librariesText = json["libraries"]!.ToString();
        string[] libraryPatterns = [
            @"(?<=net.minecraftforge:forge:)[0-9]{1,2}.[0-9+.]+",
            @"(?<=net.minecraftforge:fmlloader:)[0-9]{1,2}.[0-9+.]+",
            @"(?<=optifine:OptiFine:)[0-9]{1,2}.[0-9+.]+",
            @"(?<=((fabricmc)|(quiltmc)):intermediary:)[^\""]*"
        ];
        foreach (string pattern in libraryPatterns) {
            if (librariesText.RegexSeek(pattern) is string match) return match;
        }

        if (json["jar"] is JToken jarToken) return jarToken.ToString();

        if (jarVersionJson?["name"] is JToken jarNameToken && jarNameToken.ToString() is { Length: < 32 } jarName) {
            Logger.Info($"从版本 jar 中的 version.json 获取到版本号：{jarName}");
            return jarName;
        }
        string VersionPattern = @"(([1-9][0-9]w[0-9]{2}[a-g])|((1|[2-9][0-9])\.[0-9]+(\.[0-9]+)?(-(pre|rc|snapshot-?)[1-9]*| Pre-Release( [1-9])?)?))(_unobfuscated)?";

        if (json["id"]!.ToString().RegexSeek(VersionPattern, RegexOptions.IgnoreCase) is string idMatch) return idMatch;

        Logger.Info($"无法完全确认 MC 版本号的版本：{instanceName}");
        reliable = false;

        if (instanceName.RegexSeek(VersionPattern, RegexOptions.IgnoreCase) is string folderMatch) return folderMatch;

        JObject withoutLibraries = (JObject) json.DeepClone();
        withoutLibraries.Remove("libraries");
        return withoutLibraries.ToString(Formatting.None).RegexSeek(VersionPattern, RegexOptions.IgnoreCase);
    }

    private static bool IsFoolName(string name) {
        name = name.Lower();
        return name.StartsWithF("2.0") || name is "15w14a" or "1.rv-pre1" or
            "3d shareware v1.34" or "20w14∞" or "22w13oneblockatatime" or "23w13a_or_b" or
            "24w14potato" or "25w14craftmine" or "26w14a" || name.StartsWithF("20w14inf");
    }

    private static bool IsSnapshotName(string name) =>
        new[] { "w", "snapshot", "rc", "pre", "experimental", "-" }.Any(name.ContainsIgnoreCase);

    private static string? FindArgumentValue(JObject json, string name) {
        JArray? arguments = json["arguments"]?["game"] as JArray;
        if (arguments is null) return null;
        for (int i = 0; i + 1 < arguments.Count; i++) {
            if (arguments[i]?.ToString() == name) return arguments[i + 1]?.ToString();
        }
        return null;
    }

    public static bool IsStandardFormat(string? version) {
        if (version is null) return false;
        if (Regex.IsMatch(version, @"^1\.\d")) return true;
        return double.TryParse(version.RegexSeek(@"^[2-9]\d\.\d+"), NumberStyles.Float,
            CultureInfo.InvariantCulture, out double value) && value >= 26;
    }

    public static int VersionToDrop(string? vanillaName) {
        if (vanillaName is null) return 209;
        Version version = NameToVersion(vanillaName);
        return version.Major >= 1000 ? 209 : version.Major * 10 + version.Minor;
    }

    public static string DropToVersion(int drop) {
        if (drop >= 250) return $"{drop / 10}.{drop % 10}";
        if (drop != 209) return $"1.{drop / 10}";
        Logger.Warn(new Exception("尝试将旧快照版 Drop 序数转换为版本字符串，这不应该发生！"));
        return "1.20";
    }

    public static Version NameToVersion(string? name) {
        if (name is null) return new Version(9999, 0, 0);
        name = name.Lower().Replace("_unobfuscated", "").Replace(" unobfuscated", "");

        if (name.StartsWithF("2.0")) return NameToVersion("1.5.1");
        if (name == "15w14a") return NameToVersion("1.8.3");
        if (name.ContainsIgnoreCase(".rv-pre")) return NameToVersion("1.9.2");
        if (name.ContainsIgnoreCase("shareware")) return NameToVersion("1.13.2");
        if (name.StartsWithF("20w14") && name != "20w14a") return NameToVersion("1.15.2");
        if (name.ContainsIgnoreCase("oneblockatatime")) return NameToVersion("1.18.2");
        if (name.ContainsIgnoreCase("23w13a") && name != "20w13a") return NameToVersion("1.19.4");
        if (name == "24w14potato") return NameToVersion("1.20.4");
        if (name == "25w14craftmine") return NameToVersion("1.21.4");
        if (name == "26w14a") return NameToVersion("26.1.1");

        static int ParseSegment(string[] segments, int index) {
            if (index >= segments.Length) return 0;

            return int.TryParse(segments[index].RegexSeek(@"^\d+"), out int value) ? value : 0;
        }
        string[] segments = name.Split([' ', '_', '-', '.'], StringSplitOptions.None);
        if (name.StartsWithF("1.")) {
            return new Version(ParseSegment(segments, 1), 0, ParseSegment(segments, 2));
        } else if (name.RegexCheck(@"^[2-9][0-9]\.")) {
            return new Version(ParseSegment(segments, 0), ParseSegment(segments, 1), ParseSegment(segments, 2));
        } else {
            return new Version(9999, 0, 0);
        }
    }
}
