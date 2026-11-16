namespace Kiln.Core.Minecraft;

/// <summary>
/// 扫描 versions 目录，把每个版本的版本文件解析出来并摊平继承链。
/// 完全只读：不下载、不修复、不写任何东西。
/// </summary>
public sealed class VersionScanner
{
    public IReadOnlyList<ScannedVersion> Scan(MinecraftFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        if (!Directory.Exists(folder.VersionsPath))
        {
            return [];
        }

        var sources = new Dictionary<string, (string JsonPath, VersionJson Json)>(StringComparer.OrdinalIgnoreCase);
        var failures = new Dictionary<string, (string? JsonPath, string Error)>(StringComparer.OrdinalIgnoreCase);
        var directoryNames = new List<string>();

        foreach (var directory in Directory.EnumerateDirectories(folder.VersionsPath))
        {
            var id = Path.GetFileName(directory);
            if (string.IsNullOrEmpty(id)) continue;
            directoryNames.Add(id);

            var jsonPath = Path.Combine(directory, id + ".json");
            if (!File.Exists(jsonPath))
            {
                failures[id] = (null, $"版本目录里没有 {id}.json");
                continue;
            }

            try
            {
                var json = VersionJson.Parse(File.ReadAllText(jsonPath));
                if (json is null)
                {
                    failures[id] = (jsonPath, "版本文件不是合法的 JSON");
                    continue;
                }

                sources[id] = (jsonPath, json);
            }
            catch (IOException ex)
            {
                failures[id] = (jsonPath, $"读不了版本文件：{ex.Message}");
            }
        }

        var index = sources.ToDictionary(pair => pair.Key, pair => pair.Value.Json, StringComparer.OrdinalIgnoreCase);
        var results = new List<ScannedVersion>(directoryNames.Count);

        foreach (var id in directoryNames)
        {
            if (failures.TryGetValue(id, out var failure))
            {
                results.Add(new ScannedVersion
                {
                    Id = id,
                    JsonPath = failure.JsonPath,
                    Error = failure.Error,
                });
                continue;
            }

            var source = sources[id];
            var fileInfo = new FileInfo(source.JsonPath);

            VersionResolver.TryResolve(id, index, out var resolved, out var error);
            results.Add(new ScannedVersion
            {
                Id = id,
                Type = resolved?.Type,
                InheritsFrom = source.Json.InheritsFrom,
                JsonPath = source.JsonPath,
                SizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
                LastWriteTime = fileInfo.Exists ? fileInfo.LastWriteTime : default,
                Resolved = resolved,
                Error = resolved is null ? error : null,
            });
        }

        return results
            .OrderByDescending(version => version.LastWriteTime)
            .ThenBy(version => version.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
