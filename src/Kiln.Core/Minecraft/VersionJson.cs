using System.Text.Json;

namespace Kiln.Core.Minecraft;

/// <summary>
/// version.json 的原始结构。字段刻意保持宽松：加载器（Forge / NeoForge / Fabric）
/// 生成的 json 经常缺字段或多字段，解析失败比崩溃更常见。
/// </summary>
internal sealed class VersionJson
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? InheritsFrom { get; set; }
    public string? MainClass { get; set; }
    public string? Assets { get; set; }
    public string? Jar { get; set; }
    public AssetIndexJson? AssetIndex { get; set; }
    public List<LibraryJson>? Libraries { get; set; }
    public ArgumentsJson? Arguments { get; set; }

    /// <summary>1.13 之前的旧格式，参数是一整行命令行。</summary>
    public string? MinecraftArguments { get; set; }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static VersionJson? Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<VersionJson>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

internal sealed class AssetIndexJson
{
    public string? Id { get; set; }
    public string? Sha1 { get; set; }
    public long? Size { get; set; }
    public string? Url { get; set; }
}

internal sealed class LibraryJson
{
    public string? Name { get; set; }
    public LibraryDownloadsJson? Downloads { get; set; }
    public List<RuleJson>? Rules { get; set; }
}

internal sealed class LibraryDownloadsJson
{
    public ArtifactJson? Artifact { get; set; }
}

internal sealed class ArtifactJson
{
    public string? Path { get; set; }
    public string? Sha1 { get; set; }
    public long? Size { get; set; }
    public string? Url { get; set; }
}

internal sealed class RuleJson
{
    public string? Action { get; set; }
    public RuleOsJson? Os { get; set; }
}

internal sealed class RuleOsJson
{
    public string? Name { get; set; }
    public string? Arch { get; set; }
    public string? Version { get; set; }
}

internal sealed class ArgumentsJson
{
    public List<JsonElement>? Game { get; set; }
    public List<JsonElement>? Jvm { get; set; }
}
