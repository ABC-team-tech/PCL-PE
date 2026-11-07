namespace PCLCS;

public record McInstallManifest(int SchemaVersion, McComponents Components, DateTime LastModifiedUtc, IReadOnlyList<McInstallManifest.ManagedFile> ManagedFiles) {

    public McInstallManifest RefreshModComponents(string gameFolder) {
        List<ManagedFile> invalidFiles = [];
        foreach (ManagedFile entry in ManagedFiles) {
            if (entry.Component is null) continue;
            FileInfo file = new(Path.Combine(gameFolder, entry.RelativePath));
            if (!file.Exists || file.LastWriteTimeUtc != entry.LastWriteTimeUtc.ToUniversalTime()) invalidFiles.Add(entry);
        }
        if (!invalidFiles.Any()) return this;
        return this with {
            Components = Components with {
                OptiFineEntry = invalidFiles.Any(file => file.Component == "OptiFine") ? null : Components.OptiFineEntry,
                FabricApiVersion = invalidFiles.Any(file => file.Component == "FabricApi") ? null : Components.FabricApiVersion,
                OptiFabricVersion = invalidFiles.Any(file => file.Component == "OptiFabric") ? null : Components.OptiFabricVersion
            },
            ManagedFiles = ManagedFiles.Where(file => !invalidFiles.Contains(file)).ToList(),
            LastModifiedUtc = DateTime.UtcNow
        };
    }

    public record ManagedFile(string RelativePath, string Sha1, DateTime LastWriteTimeUtc,
        [property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)] string? Component = null);
}

[method: JsonConstructor]
public record McComponents(string MinecraftVersion, Dictionary<ModLoaders, string> ModLoaderVersions,
        [property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)] JObject? OptiFineEntry = null,
        [property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)] string? FabricApiVersion = null,
        [property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)] string? OptiFabricVersion = null) {
    [JsonIgnore]
    public string? OptiFineVersion => OptiFineEntry?["InstanceName"]?.Value<string>()?.AfterLast("-OptiFine_");
    public McComponents(string minecraftVersion, JObject? optiFineEntry, string? forgeVersion,
        string? neoForgeVersion, string? fabricVersion, string? fabricApiVersion, string? optiFabricVersion, string? liteLoaderVersion)
        : this(minecraftVersion, [], optiFineEntry, fabricApiVersion, optiFabricVersion) {
        if (forgeVersion is not null) ModLoaderVersions[ModLoaders.Forge] = forgeVersion;
        if (neoForgeVersion is not null) ModLoaderVersions[ModLoaders.NeoForge] = neoForgeVersion;
        if (fabricVersion is not null) ModLoaderVersions[ModLoaders.Fabric] = fabricVersion;
        if (liteLoaderVersion is not null) ModLoaderVersions[ModLoaders.LiteLoader] = liteLoaderVersion;
    }
    public bool IsEquivalentTo(McComponents other) =>
        MinecraftVersion == other.MinecraftVersion &&
        OptiFineVersion == other.OptiFineVersion &&
        FabricApiVersion == other.FabricApiVersion &&
        OptiFabricVersion == other.OptiFabricVersion &&
        ModLoaderVersions.Count == other.ModLoaderVersions.Count &&
        ModLoaderVersions.All(entry => other.ModLoaderVersions.TryGetValue(entry.Key, out string? version) && version == entry.Value);
}
