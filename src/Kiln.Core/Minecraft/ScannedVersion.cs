namespace Kiln.Core.Minecraft;

/// <summary>扫描到的一个版本条目。哪怕是坏的也保留，界面上能看见比默默消失好。</summary>
public sealed record ScannedVersion
{
    public required string Id { get; init; }

    public string? Type { get; init; }
    public string? InheritsFrom { get; init; }
    public string? JsonPath { get; init; }
    public long SizeBytes { get; init; }
    public DateTimeOffset LastWriteTime { get; init; }

    /// <summary>继承链摊平后的结果，解析失败时为 null。</summary>
    public ResolvedVersion? Resolved { get; init; }

    /// <summary>解析失败的原因，正常时为 null。</summary>
    public string? Error { get; init; }

    public bool IsUsable => Resolved is not null;
}
