# 炉火 Kiln

Minecraft Java 版启动器。开源，免费，界面和内核都是自己写的。

Kiln 是窑的意思。窑用来烧火，这个启动器用来点火进游戏。

## 现在能做什么

- 自动找到游戏目录，也可以手动指定一个
- 列出所有已安装的版本，原版和各类加载器生成的版本文件都读得懂
- 摊平 `inheritsFrom` 继承链：依赖库按坐标去重，参数按子在前拼接，主类取对的那一个
- 版本文件损坏、继承链断开、父子互相继承，会直接告诉你断在哪一环，不会闪退

## 还不能做什么

启动、下载、登录、装模组都还没做。现在它只能看懂你硬盘上有什么。

## 运行

需要 [.NET 11 SDK](https://dotnet.microsoft.com/download)。

```bash
dotnet build
dotnet run --project src/Kiln.App
```

指到指定的游戏目录：

```bash
dotnet run --project src/Kiln.App -- "D:/Games/.minecraft"
```

也可以设环境变量 `KILN_MINECRAFT_DIR`。都不指定的话，会自己找 `%APPDATA%\.minecraft`。

跑测试：

```bash
dotnet test
```

## 目录结构

```
src/
  Kiln.Core/            启动器内核，不依赖 UI，可以直接单测
    Minecraft/          版本扫描、继承链合并
  Kiln.App/             Avalonia 桌面端
tests/
  Kiln.Core.Tests/      内核单元测试
```

分层的原则只有一条：内核不认识窗口。`Kiln.Core` 里不出现任何 UI 类型，以后要换界面、做命令行、做别的壳，内核都不用动。

## 路线图

| 版本 | 内容 | 状态 |
| --- | --- | --- |
| v0.0.1 | 只读扫描、继承链合并 | 已完成 |
| v0.1.0 | 能启动：Java 探测、classpath 拼装、占位符替换、日志回显 | 进行中 |
| v0.2.0 | 安装原版：版本清单、资源下载、多镜像、SHA-1 校验 | 计划 |
| v0.3.0 | 加载器：Fabric / Quilt / NeoForge / Forge | 计划 |
| v0.4.0 | 模组与整合包：Modrinth API、`.mrpack` 导入导出 | 计划 |
| v0.5.0 | Java 运行时管理、崩溃分析、多账号、自动更新 | 计划 |

先把已有实例跑起来，再碰下载。启动这条路全在本地，不依赖任何服务，但坑最多；下载牵扯镜像、版权和分发，留到后面单独做。

## 开发

- 念安（[@xiaonuo037](https://github.com/xiaonuo037)）：主开发
- DeepSeek：协作开发

## 致谢

界面和交互上参考过 PCL、HMCL、Prism Launcher 的做法，谢谢这些先行者。

## 许可证

[GPL-3.0](LICENSE)。随便用、随便改、随便分发，改完的东西也请继续开源。
