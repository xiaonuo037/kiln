namespace Kiln.Core.Minecraft;

/// <summary>
/// 把 inheritsFrom 继承链摊平之后的结果。
/// 启动游戏只需要这一份，不需要再回头翻一堆 json。
/// </summary>
public sealed record ResolvedVersion
{
    public required string Id { get; init; }

    /// <summary>release / snapshot / old_beta / 加载器自填的类型。</summary>
    public string? Type { get; init; }

    public string? MainClass { get; init; }

    /// <summary>assets/indexes 下的索引名，老版本可能为 "legacy" 或 "pre-1.6"。</summary>
    public string? AssetsId { get; init; }

    /// <summary>客户端 jar 所在的版本号目录。默认等于 Id，继承来的版本会指回父版本。</summary>
    public string? JarVersionId { get; init; }

    /// <summary>库的坐标（group:artifact:version），已按继承链去重。</summary>
    public IReadOnlyList<string> LibraryNames { get; init; } = [];

    public IReadOnlyList<string> GameArguments { get; init; } = [];
    public IReadOnlyList<string> JvmArguments { get; init; } = [];

    /// <summary>从自己到最顶层父版本的 id 顺序。</summary>
    public IReadOnlyList<string> InheritanceChain { get; init; } = [];
}
