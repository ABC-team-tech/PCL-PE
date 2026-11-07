namespace PCLCS;

public class WikiEntry {

    public int Id;

    public string? ChineseName;

    public Dictionary<ResourcePlatforms, string> Slugs = [];

    public int Popularity;

    public static readonly Lazy<List<WikiEntry>> All = new(() => {
        var dataLines = FileUtils.ReadAsLines(@"Resource\WikiEntries.txt", type: typeof(WikiEntry)).ToList();

        Queue<int> popularities = [];
        for (int i = 0; i < dataLines[^1].Length; i += 3) {
            popularities.Enqueue(int.Parse(dataLines[^1].Substring(i, 3).ConvertRadix(86, 10)));
        }
        dataLines.RemoveAt(dataLines.Count - 1);

        List<WikiEntry> results = [];
        var lineNumber = 0;
        foreach (var lineData in dataLines) {
            lineNumber++;
            if (lineData == "") continue;

            var popularity = popularities.Dequeue();
            foreach (var entryData in lineData.Split('¨')) {
                var entry = new WikiEntry();
                var parts = entryData.Split('|');
                var slugs = parts[0];
                if (slugs.StartsWithF("@")) {
                    entry.Slugs.Add(ResourcePlatforms.Modrinth, slugs.Replace("@", ""));
                } else if (slugs.EndsWithF("@")) {
                    entry.Slugs.Add(ResourcePlatforms.CurseForge, slugs.TrimEnd('@'));
                    entry.Slugs.Add(ResourcePlatforms.Modrinth, entry.Slugs[ResourcePlatforms.CurseForge]);
                } else if (slugs.Contains("@")) {
                    var splitSlugs = slugs.Split('@');
                    entry.Slugs.Add(ResourcePlatforms.CurseForge, splitSlugs[0]);
                    entry.Slugs.Add(ResourcePlatforms.Modrinth, splitSlugs[1]);
                } else {
                    entry.Slugs.Add(ResourcePlatforms.CurseForge, slugs);
                }

                entry.Id = lineNumber;
                entry.Popularity = popularity;
                if (parts.Length >= 2) {
                    entry.ChineseName = parts[^1];
                    if (entry.ChineseName.Contains("*")) {
                        var englishName = entry.Slugs.Values.First().Replace("-", " ").Capitalize();
                        entry.ChineseName = entry.ChineseName.Replace("*", $" ({englishName})");
                    }
                }
                results.Add(entry);
            }
        }
        return results;
    });

    public override string ToString()
        => $"{Slugs.GetOrDefault(ResourcePlatforms.CurseForge)}&{Slugs.GetOrDefault(ResourcePlatforms.Modrinth)}|{Id}|{ChineseName}，浏览量 {Popularity}";
}
