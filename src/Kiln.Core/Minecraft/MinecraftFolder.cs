namespace Kiln.Core.Minecraft;

/// <summary>
/// 一个游戏目录（.minecraft）。只负责路径推算，不做任何 IO 副作用。
/// </summary>
public sealed class MinecraftFolder
{
    public const string DefaultFolderName = ".minecraft";

    public MinecraftFolder(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        RootPath = Path.GetFullPath(rootPath);
    }

    public string RootPath { get; }

    public string VersionsPath => Path.Combine(RootPath, "versions");
    public string LibrariesPath => Path.Combine(RootPath, "libraries");
    public string AssetsPath => Path.Combine(RootPath, "assets");
    public string ModsPath => Path.Combine(RootPath, "mods");

    /// <summary>有 versions 目录，或者有启动器留下的 profiles 文件，就认为像个游戏目录。</summary>
    public bool LooksValid =>
        Directory.Exists(VersionsPath) ||
        File.Exists(Path.Combine(RootPath, "launcher_profiles.json")) ||
        File.Exists(Path.Combine(RootPath, "launcher_profiles_microsoft_store.json"));

    /// <summary>官方默认位置：%APPDATA%\.minecraft（其它平台按各自规范推）。</summary>
    public static string GetDefaultRoot()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, DefaultFolderName);
    }

    /// <summary>探测一个可用的游戏目录，找不到就返回 null。</summary>
    public static MinecraftFolder? Detect()
    {
        var candidates = new List<string> { GetDefaultRoot() };

        // 便携式：启动器所在目录旁边放一个 .minecraft
        var exeDir = AppContext.BaseDirectory;
        candidates.Add(Path.Combine(exeDir, DefaultFolderName));
        candidates.Add(exeDir);

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            var folder = new MinecraftFolder(candidate);
            if (folder.LooksValid) return folder;
        }

        return null;
    }
}
