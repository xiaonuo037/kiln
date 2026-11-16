using Kiln.Core.Minecraft;

namespace Kiln.App.ViewModels;

/// <summary>版本列表里的一行。只做展示，不承担任何逻辑。</summary>
public sealed class VersionListItem
{
    public required string Id { get; init; }
    public required string Subtitle { get; init; }
    public required ScannedVersion Source { get; init; }

    public static VersionListItem From(ScannedVersion version) => new()
    {
        Id = version.Id,
        Subtitle = BuildSubtitle(version),
        Source = version,
    };

    private static string BuildSubtitle(ScannedVersion version)
    {
        if (version.Resolved is null)
        {
            return "解析失败";
        }

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(version.Type)) parts.Add(version.Type);
        if (version.Resolved.InheritanceChain.Count > 1) parts.Add($"继承 {version.Resolved.InheritanceChain.Count - 1} 层");
        parts.Add($"{version.Resolved.LibraryNames.Count} 个库");

        return string.Join(" · ", parts);
    }
}
