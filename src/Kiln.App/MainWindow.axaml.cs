using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Kiln.App.ViewModels;
using Kiln.Core.Minecraft;

namespace Kiln.App;

public partial class MainWindow : Window
{
    private readonly VersionScanner _scanner = new();
    private MinecraftFolder? _folder;

    public MainWindow()
    {
        InitializeComponent();
        VersionList.ItemsSource = Versions;
        Opened += (_, _) => LoadDetectedFolder();
    }

    public ObservableCollection<VersionListItem> Versions { get; } = [];

    private void LoadDetectedFolder()
    {
        // 命令行参数或环境变量可以直接指定目录，方便调试和做快捷方式。
        var explicitPath = Environment.GetCommandLineArgs()
            .Skip(1)
            .FirstOrDefault(path => !path.StartsWith('-') && Directory.Exists(path));
        explicitPath ??= Environment.GetEnvironmentVariable("KILN_MINECRAFT_DIR");

        if (!string.IsNullOrWhiteSpace(explicitPath) && Directory.Exists(explicitPath))
        {
            Load(new MinecraftFolder(explicitPath));
            return;
        }

        var folder = MinecraftFolder.Detect();
        if (folder is null)
        {
            ShowNothing("没有找到默认游戏目录。点右上角「选择目录」，指到你的 .minecraft。");
            SetStatus("未找到游戏目录");
            return;
        }

        Load(folder);
    }

    private void Load(MinecraftFolder folder)
    {
        _folder = folder;
        FolderText.Text = folder.RootPath;
        Versions.Clear();

        try
        {
            var versions = _scanner.Scan(folder);
            foreach (var version in versions)
            {
                Versions.Add(VersionListItem.From(version));
            }

            ListHeader.Text = $"版本 · {Versions.Count}";

            if (Versions.Count == 0)
            {
                ShowNothing(folder.LooksValid
                    ? "这个目录里还没有任何版本。下一步做「安装原版」之后，这里就会长出东西。"
                    : "这个目录看起来不是 .minecraft：既没有 versions 目录，也没有启动器留下的配置文件。");
                SetStatus("0 个版本");
                return;
            }

            var broken = Versions.Count(item => item.Source.Resolved is null);
            SetStatus(broken == 0
                ? $"扫到 {Versions.Count} 个版本，全部解析正常"
                : $"扫到 {Versions.Count} 个版本，其中 {broken} 个解析失败");

            VersionList.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            ShowNothing($"扫描出错：{ex.Message}");
            SetStatus("扫描失败");
        }
    }

    private void Present(VersionListItem? item)
    {
        if (item is null)
        {
            ShowNothing("从左边挑一个版本。");
            return;
        }

        var source = item.Source;
        var resolved = source.Resolved;

        DetailId.Text = source.Id;
        DetailSubtitle.Text = BuildSubtitle(source);

        if (resolved is null)
        {
            DetailCard.IsVisible = false;
            ErrorBox.IsVisible = true;
            ErrorText.Text = source.Error ?? "无法解析这个版本。";
            HintText.Text = source.JsonPath is null
                ? null
                : $"版本文件：{source.JsonPath}";
            return;
        }

        ErrorBox.IsVisible = false;
        DetailCard.IsVisible = true;

        VType.Text = resolved.Type ?? "未标注";
        VChain.Text = string.Join("  →  ", resolved.InheritanceChain);
        VMainClass.Text = resolved.MainClass ?? "未标注";
        VAssets.Text = resolved.AssetsId ?? "未标注";
        VJar.Text = resolved.JarVersionId ?? source.Id;
        VLibs.Text = $"{resolved.LibraryNames.Count} 个（已按继承链去重）";
        VArgs.Text = $"游戏 {resolved.GameArguments.Count} 项 · JVM {resolved.JvmArguments.Count} 项";

        HintText.Text = "目前只做只读解析：不修改任何文件，也还不能启动。";
    }

    private static string BuildSubtitle(ScannedVersion source)
    {
        if (source.JsonPath is null) return "没有版本文件";

        var size = source.SizeBytes < 1024
            ? $"{source.SizeBytes} B"
            : $"{source.SizeBytes / 1024.0:F1} KB";

        var written = source.LastWriteTime == default
            ? "未知时间"
            : source.LastWriteTime.LocalDateTime.ToString("yyyy-MM-dd HH:mm");

        return $"版本文件 {size} · 修改于 {written}";
    }

    private void ShowNothing(string message)
    {
        DetailId.Text = "—";
        DetailSubtitle.Text = string.Empty;
        DetailCard.IsVisible = false;
        ErrorBox.IsVisible = false;
        HintText.Text = message;
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private void OnVersionSelected(object? sender, SelectionChangedEventArgs e) =>
        Present(VersionList.SelectedItem as VersionListItem);

    private void OnRefresh(object? sender, RoutedEventArgs e)
    {
        if (_folder is null)
        {
            LoadDetectedFolder();
            return;
        }

        Load(_folder);
    }

    private async void OnPickFolder(object? sender, RoutedEventArgs e)
    {
        try
        {
            var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择 .minecraft 目录",
                AllowMultiple = false,
            });

            if (picked.Count == 0) return;

            var path = picked[0].TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path)) return;

            Load(new MinecraftFolder(path));
        }
        catch (Exception ex)
        {
            SetStatus($"选择目录失败：{ex.Message}");
        }
    }
}
