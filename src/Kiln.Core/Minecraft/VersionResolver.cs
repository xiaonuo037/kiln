using System.Text.Json;

namespace Kiln.Core.Minecraft;

/// <summary>
/// 沿着 inheritsFrom 往上把版本文件叠起来。
/// 规则（和官方启动器、各类加载器保持一致）：
///  - 子版本优先：mainClass / assets / assetIndex / jar / type 取链上第一个非空值；
///  - 列表追加：libraries 子在前、父在后，按 group:artifact 去重；参数同理，子在前。
/// </summary>
internal static class VersionResolver
{
    public static bool TryResolve(
        string id,
        IReadOnlyDictionary<string, VersionJson> index,
        out ResolvedVersion? resolved,
        out string? error)
    {
        resolved = null;
        error = null;

        if (!index.TryGetValue(id, out var first))
        {
            error = $"找不到版本 {id} 的版本文件";
            return false;
        }

        var chain = new List<VersionJson>();
        var chainIds = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var currentId = id;
        while (true)
        {
            if (!index.TryGetValue(currentId, out var current))
            {
                error = $"继承链断了：找不到父版本 {currentId}";
                return false;
            }

            if (!visited.Add(currentId))
            {
                error = $"继承链出现循环：{string.Join(" → ", chainIds)} → {currentId}";
                return false;
            }

            chain.Add(current);
            chainIds.Add(currentId);

            if (string.IsNullOrWhiteSpace(current.InheritsFrom)) break;
            currentId = current.InheritsFrom;
        }

        // libraries：子版本覆盖父版本里同名（同 group:artifact）的库
        var libraryNames = new List<string>();
        var libraryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in chain)
        {
            if (node.Libraries is null) continue;
            foreach (var library in node.Libraries)
            {
                if (string.IsNullOrWhiteSpace(library.Name)) continue;
                if (libraryKeys.Add(GetLibraryKey(library.Name)))
                {
                    libraryNames.Add(library.Name);
                }
            }
        }

        resolved = new ResolvedVersion
        {
            Id = id,
            Type = FirstNonNull(chain, n => n.Type),
            MainClass = FirstNonNull(chain, n => n.MainClass),
            AssetsId = FirstNonNull(chain, n => n.AssetIndex?.Id) ?? FirstNonNull(chain, n => n.Assets),
            JarVersionId = FirstNonNull(chain, n => n.Jar) ?? id,
            LibraryNames = libraryNames,
            GameArguments = CollectArguments(chain, n => n.Arguments?.Game),
            JvmArguments = CollectArguments(chain, n => n.Arguments?.Jvm),
            InheritanceChain = chainIds,
        };

        return true;
    }

    /// <summary>group:artifact:version → group:artifact</summary>
    private static string GetLibraryKey(string name)
    {
        var parts = name.Split(':');
        return parts.Length >= 2 ? $"{parts[0]}:{parts[1]}" : name;
    }

    private static string? FirstNonNull(List<VersionJson> chain, Func<VersionJson, string?> selector)
    {
        foreach (var node in chain)
        {
            var value = selector(node);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return null;
    }

    /// <summary>参数项可能是纯字符串，也可能是带 rules 的对象，这里只取字面值。</summary>
    private static List<string> CollectArguments(List<VersionJson> chain, Func<VersionJson, List<JsonElement>?> selector)
    {
        var result = new List<string>();
        foreach (var node in chain)
        {
            var items = selector(node);
            if (items is null) continue;
            foreach (var item in items)
            {
                switch (item.ValueKind)
                {
                    case JsonValueKind.String:
                        var text = item.GetString();
                        if (!string.IsNullOrEmpty(text)) result.Add(text);
                        break;
                    case JsonValueKind.Object when item.TryGetProperty("value", out var value):
                        if (value.ValueKind == JsonValueKind.String)
                        {
                            var single = value.GetString();
                            if (!string.IsNullOrEmpty(single)) result.Add(single);
                        }
                        else if (value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var entry in value.EnumerateArray())
                            {
                                var text2 = entry.GetString();
                                if (!string.IsNullOrEmpty(text2)) result.Add(text2);
                            }
                        }

                        break;
                }
            }
        }

        return result;
    }
}
