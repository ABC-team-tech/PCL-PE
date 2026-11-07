namespace PCLCS;

[Flags]
public enum ResourceTypes {

    Mod = 1,

    ModPack = 2,

    ResourcePack = 4,

    Shader = 8,

    DataPack = 16,

    Plugin = 32,

    Map = 64,

    ModOrDataPack = Mod | DataPack,

    Any = Mod | ModPack | ResourcePack | Shader | DataPack | Plugin | Map,
}

[Flags]
public enum ResourcePlatforms {
    CurseForge = 1,
    Modrinth = 2,
    Any = CurseForge | Modrinth,
}

[Flags]
public enum ModLoaders {
    None = 0,
    Forge = 1,
    LiteLoader = 2,
    Fabric = 4,
    NeoForge = 16,
    All = Forge | LiteLoader | Fabric | NeoForge,
}

public enum DonationRank {
    None = 0,
    Rank6 = 6,
    Rank12 = 12,
    Rank23 = 23,
    Rank54 = 54,
    Rank98 = 98,
}

public static class Versions {

    public const char PotatoVersion = '1';

    public const int LocalModCacheVersion = 21;

    public const int McInstanceCacheVersion = 39;

    public const int JavaConfigVersion = 1;

    public const int McInstallManifestVersion = 1;
}
