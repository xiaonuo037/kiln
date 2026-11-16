using Kiln.Core.Minecraft;

namespace Kiln.Core.Tests;

public sealed class VersionScannerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "kiln-tests", Guid.NewGuid().ToString("N"));

    private MinecraftFolder CreateFolder() => new(_root);

    private void WriteVersion(string id, string json)
    {
        var directory = Path.Combine(_root, "versions", id);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, id + ".json"), json);
    }

    [Fact]
    public void 没有_versions_目录时返回空列表()
    {
        Directory.CreateDirectory(_root);

        var versions = new VersionScanner().Scan(CreateFolder());

        Assert.Empty(versions);
    }

    [Fact]
    public void 能解析单个原版版本()
    {
        WriteVersion("1.20.1", """
        {
          "id": "1.20.1",
          "type": "release",
          "mainClass": "net.minecraft.client.main.Main",
          "assets": "5",
          "libraries": [
            { "name": "com.mojang:authlib:4.0.43" },
            { "name": "org.lwjgl:lwjgl:3.3.1" }
          ]
        }
        """);

        var versions = new VersionScanner().Scan(CreateFolder());

        var version = Assert.Single(versions);
        Assert.Equal("1.20.1", version.Id);
        Assert.True(version.IsUsable);
        Assert.Null(version.Error);
        Assert.Equal("release", version.Type);
        Assert.Equal("net.minecraft.client.main.Main", version.Resolved!.MainClass);
        Assert.Equal("5", version.Resolved.AssetsId);
        Assert.Equal(2, version.Resolved.LibraryNames.Count);
        Assert.Equal(["1.20.1"], version.Resolved.InheritanceChain);
    }

    [Fact]
    public void 继承链会合并库并去重_主类取子版本()
    {
        WriteVersion("1.20.1", """
        {
          "id": "1.20.1",
          "type": "release",
          "mainClass": "net.minecraft.client.main.Main",
          "assets": "5",
          "libraries": [
            { "name": "com.mojang:authlib:4.0.43" },
            { "name": "org.lwjgl:lwjgl:3.3.1" }
          ],
          "arguments": {
            "game": ["--username", "${auth_player_name}"],
            "jvm": ["-Djava.library.path=${natives_directory}"]
          }
        }
        """);

        WriteVersion("fabric-loader-0.15-1.20.1", """
        {
          "id": "fabric-loader-0.15-1.20.1",
          "inheritsFrom": "1.20.1",
          "mainClass": "net.fabricmc.loader.impl.launch.knot.KnotClient",
          "libraries": [
            { "name": "org.lwjgl:lwjgl:3.3.1" },
            { "name": "net.fabricmc:fabric-loader:0.15.0" }
          ],
          "arguments": { "game": ["--fabric"] }
        }
        """);

        var versions = new VersionScanner().Scan(CreateFolder());
        var child = versions.Single(version => version.Id == "fabric-loader-0.15-1.20.1");

        Assert.True(child.IsUsable);
        var resolved = child.Resolved!;

        Assert.Equal("net.fabricmc.loader.impl.launch.knot.KnotClient", resolved.MainClass);
        Assert.Equal("5", resolved.AssetsId);
        Assert.Equal(["fabric-loader-0.15-1.20.1", "1.20.1"], resolved.InheritanceChain);

        Assert.Equal(
            ["org.lwjgl:lwjgl:3.3.1", "net.fabricmc:fabric-loader:0.15.0", "com.mojang:authlib:4.0.43"],
            resolved.LibraryNames);

        Assert.Equal(["--fabric", "--username", "${auth_player_name}"], resolved.GameArguments);
        Assert.Equal(["-Djava.library.path=${natives_directory}"], resolved.JvmArguments);
    }

    [Fact]
    public void 父版本缺失时给出明确错误而不是崩溃()
    {
        WriteVersion("broken-child", """
        {
          "id": "broken-child",
          "inheritsFrom": "1.19.2"
        }
        """);

        var versions = new VersionScanner().Scan(CreateFolder());

        var version = Assert.Single(versions);
        Assert.False(version.IsUsable);
        Assert.Contains("继承链断了", version.Error);
        Assert.Contains("1.19.2", version.Error);
    }

    [Fact]
    public void 循环继承会被挡住()
    {
        WriteVersion("a", """{ "id": "a", "inheritsFrom": "b" }""");
        WriteVersion("b", """{ "id": "b", "inheritsFrom": "a" }""");

        var versions = new VersionScanner().Scan(CreateFolder());

        Assert.All(versions, version => Assert.Contains("循环", version.Error));
    }

    [Fact]
    public void 坏掉的_json_只影响自己()
    {
        WriteVersion("good", """{ "id": "good", "type": "release" }""");
        WriteVersion("bad", "{ 这不是 json ");

        var versions = new VersionScanner().Scan(CreateFolder());

        Assert.Equal(2, versions.Count);
        Assert.True(versions.Single(version => version.Id == "good").IsUsable);
        Assert.False(versions.Single(version => version.Id == "bad").IsUsable);
    }

    [Fact]
    public void 缺少版本文件的目录也会被列出来()
    {
        var directory = Path.Combine(_root, "versions", "empty-version");
        Directory.CreateDirectory(directory);

        var versions = new VersionScanner().Scan(CreateFolder());

        var version = Assert.Single(versions);
        Assert.False(version.IsUsable);
        Assert.Contains("没有 empty-version.json", version.Error);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // 临时目录清理失败不该让测试挂掉
        }
    }
}
