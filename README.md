# 炉火 · Kiln

一个从零写起的 Minecraft Java 版启动器。

不是 PCL 的分支，也不是二次开发。**没有一行业务代码来自其它启动器**——只参考了它们的产品形态（左侧版本列表、一个大启动按钮、下载页分栏这类东西），实现和界面都是自己的。

名字取自「窑」：窑是烧火的地方，启动器是把火点起来的那一下。

## 现在到哪了

**v0.0.1 — 只读骨架。** 能扫描游戏目录、解析版本文件、把 `inheritsFrom` 继承链摊平。还不能启动游戏，也还不下载任何东西。

已经能做的：

- 扫描 `versions/` 下所有版本，坏的版本不崩、单独标记原因
- 解析版本文件：`mainClass`、`assetIndex`、`libraries`、`arguments`
- 摊平继承链：库按 `group:artifact` 去重、参数按"子在前"拼接、主类取子版本
- 识别继承链断裂和循环继承，给出具体是哪一环断的
- 界面：版本列表 + 详情面板，支持自选目录

还没做的：启动、下载、登录、加载器、模组。

## 怎么跑

需要 [.NET 11 SDK](https://dotnet.microsoft.com/download)。

```bash
dotnet build
dotnet run --project src/Kiln.App
```

想直接指到某个游戏目录：

```bash
dotnet run --project src/Kiln.App -- "D:/Games/.minecraft"
```

也可以设环境变量 `KILN_MINECRAFT_DIR`。不指定的话会自动找 `%APPDATA%\.minecraft`。

跑测试：

```bash
dotnet test
```

## 目录结构

```
src/
  Kiln.Core/            # 启动器的内核，不依赖 UI，可单测
    Minecraft/          # 版本扫描、继承链合并
  Kiln.App/             # Avalonia 桌面端
tests/
  Kiln.Core.Tests/      # 内核单元测试
```

分层的原则很简单：**内核不认识窗口**。`Kiln.Core` 里不出现任何 UI 类型，以后要换界面、做 CLI、做服务端，内核都不用动。

## 技术栈

- **.NET 11 + Avalonia 12**：单一语言，跨平台，启动器后端就是 HTTP / JSON / Zip / 起进程，C# 干这些很顺
- **xunit**：内核逻辑全部走单测，尤其是继承链合并这种一错就启动不了的逻辑

## 路线图

| 阶段 | 目标 | 状态 |
| --- | --- | --- |
| v0.0.1 | 只读扫描 + 继承链合并 | ✅ |
| v0.1.0 | **能启动**：Java 探测、classpath 拼装、占位符替换、进程输出回显 | 进行中 |
| v0.2.0 | 安装原版：版本清单、资源下载、多镜像、SHA-1 校验 | 计划中 |
| v0.3.0 | 加载器：Fabric / Quilt / NeoForge / Forge | 计划中 |
| v0.4.0 | 模组与整合包：Modrinth API、`.mrpack` 导入导出 | 计划中 |
| v0.5.0 | 体验：Java 运行时管理、崩溃分析、多账号、自动更新 | 计划中 |

先把「能启动已有的实例」打通，再碰下载。启动这条路是纯本地的，零合规风险，但包含了这个领域大半的坑；下载涉及镜像、版权和分发，放后面单独做。

## 关于参考

产品形态上有参考 Plain Craft Launcher（作者：龙腾猫跃）等启动器的设计思路，特此致谢。本项目与 PCL 无任何隶属关系，不使用其代码，也不以 PCL 命名。

## 开发者

- **念安** — 主开发
- **DeepSeek** — 协作开发（AI 辅助）

## 许可证

[GPL-3.0](LICENSE)。可以自由使用、修改、分发，但改完的东西也必须开源。
