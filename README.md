# PCL Professional Edition（简称 PCL PE）

PCL Professional Edition（简称 PCL PE）是基于 [PCL2（Plain Craft Launcher 2）](https://github.com/Meloong-Git/PCL) 二次开发的 Minecraft 启动器修改版，作者：[ABC-team-tech](https://github.com/ABC-team-tech)，项目仓库：[ABC-team-tech/PCL-PE](https://github.com/ABC-team-tech/PCL-PE)。

> 本项目为第三方修改版，非 PCL 官方仓库。PCL 及其相关知识产权归原作者龙腾猫跃所有，本项目继续沿用上游的[《PCL 存储库合理使用指南》](https://github.com/Meloong-Git/PCL/blob/main/LICENCE)，并以同一份指南作为使用协议。

## 相比上游的主要改动

- 内置陶瓦联机（Terracotta），开房即用，房间码形如 `U/XXXX-XXXX-XXXX-XXXX`，无需端口映射
- 外置登录（Authlib-Injector）与微软登录流程修复
- 精简冗余模块，界面与动效保持 PCL2 原版（不做额外玻璃/发光特效）
- 材质下载回退、地图壁纸支持
- 移除动态视频壁纸（避免随包分发素材的版权与体积问题），仅保留原生静态壁纸

## 目录结构

| 路径 | 说明 |
| --- | --- |
| `PCLPro/` | 启动器主工程（VB.NET + WPF，.NET Framework 4.8） |
| `PCLCS/` | 伴随 C# 工程 |
| `MeloongCore/` | 上游核心库（已随仓库以源码形式内置，不依赖子模块） |
| `PCLPro.sln` | 解决方案文件 |

## 构建

环境要求：Windows 10/11、Visual Studio 2022（含 .NET 桌面开发与 VB 工作负载）、.NET Framework 4.8 目标包。

```powershell
# 还原并编译 Release
msbuild PCLPro.sln -restore -p:Configuration=Release
```

输出位于 `PCLPro\bin\PCL Professional Edition.exe`。

> `PCLPro.vbproj` 是旧格式工程，没有 `Restore` 目标，因此还原请针对整个解决方案（`-restore`），不要对单个 .vbproj 使用 `-t:Restore`。

## 分发

与 PCL2 一致，只发布单个 exe：直接运行即为便携版，不使用独立的安装器或补丁机制（避免杀软误报）。

## 许可

- 本项目基于 PCL 二次创作，继续沿用上游的[《PCL 存储库合理使用指南》](https://github.com/Meloong-Git/PCL/blob/main/LICENCE)，不附加额外条款。
- `MeloongCore/` 为上游以 Apache-2.0 许可发布的组件，许可证全文见 [MeloongCore/LICENSE.txt](MeloongCore/LICENSE.txt)。
- 本项目公开源代码，未经授权不得以软件本体作为收费主体进行分发。

## 说明

- 仓库不再包含动态视频壁纸资源与相关代码
- 仓库不包含历史备份目录、编译产物（`bin/`、`obj/`）与更新补丁包
- 微软登录 OAuth Client ID、CurseForge API Key 未写入仓库，需通过环境变量 `PCL_MS_CLIENT_ID`、`PCL_CURSEFORGE_API_KEY` 提供

## 致谢

- [ABC-team-tech/PCL-PE](https://github.com/ABC-team-tech/PCL-PE)：本项目仓库
- [Meloong-Git/PCL](https://github.com/Meloong-Git/PCL)：PCL2 本体
- [LTCatt/PCL2Help](https://github.com/LTCatt/PCL2Help)：帮助文档库
- Terracotta 联机核心及其贡献者
