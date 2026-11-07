namespace PCLCS;
public static class Configs {

    public static readonly DynamicConfigEntry<McInstallManifest> McInstallManifest = new("MinecraftInstallManifest", null);

    public static readonly ConfigEntry<int> JavaConfigVersion = new("JavaConfigVersion", 0);
    public static readonly ConfigEntry<ConcurrentList<Java>> JavaList = new("JavaList", []);
    public static readonly ConfigEntry<List<string>> JavaRemovedList = new("JavaRemovedList", []);
    public static readonly DynamicConfigEntry<bool> JavaMigrated = new("InstanceMigratedJava", false);
    public static readonly DynamicConfigEntry<Java> JavaForced = new("InstanceForcedJava", null);

    public static readonly ConfigEntry<bool> HintModifyInstanceWarning = new("HintModifyInstanceWarning", false);

    static Configs() {

        JavaForced.PreviewGet += (args, provider) => {
            if (args.Value is not null && !JavaList.Get()!.Contains(args.Value)) {
                args.Value = null;
                JavaForced.Reset(provider);
            }
        };
    }
}
