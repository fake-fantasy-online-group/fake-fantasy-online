# fake-fantasy-online

`fake-fantasy-online` 是参考吊想进行独立实现的学习项目。它提供 Unity 客户端源码，用于学习客户端结构、资源读取和网络交互；项目不包含服务端，也不提供游戏资源。

本仓库源码采用 [MIT License](LICENSE)。该许可仅适用于仓库中的源码，不授予外部游戏资源的权利。请仅使用你有权使用的资源。仓库不会下载、分发或代为提取资源文件。缺少资源时，客户端或构建过程会无法加载相应内容。

## 源码结构

- `Assets/FakeFantasyOnline/Runtime/`：Foundation、Domain、Application、Protocol、Presentation、Bootstrap、PluginApi 和 PluginHost 程序集。
- `Assets/FakeFantasyOnline/Editor/`：Unity 编辑器构建入口。
- `Assets/FakeFantasyOnline/Scenes/Client.unity`：客户端场景。
- `Assets/StreamingAssets/`：用户自行准备的运行资源目录。
- `Packages/`、`ProjectSettings/`：Unity 工程配置。

## 环境

- Unity `6000.4.9f1`。
- macOS 构建需要 macOS Standalone 支持；Windows 构建需要 Windows Build Support (Mono)。
- 下方 `make` 命令用于 macOS/Linux 的 POSIX shell 环境；macOS Player 构建及签名整理需在 macOS 执行。
- Windows 原生环境可直接使用 Unity CLI，见下方示例。

可通过 `UNITY_EDITOR_BIN` 指定 Unity Editor 可执行文件路径。默认命令名为 `Unity`。

## 用户自行准备的资源

当前源码目标版本为 1.6.0；资源需与当前读取格式匹配。将有权使用的兼容资源放到以下项目路径，并保留原有目录层级。不要提交这些资源：

```text
Assets/Resources/gbk_table.bytes
Assets/StreamingAssets/ReferenceClient/
Assets/StreamingAssets/ReferenceClient/packages/
Assets/StreamingAssets/ReferenceClient/music/
Assets/StreamingAssets/gameassets/tables/
Assets/StreamingAssets/sndeffect/
Assets/StreamingAssets/sndeffect24_pcm/   # 可选音效目录
```

`ReferenceClient` 是运行时资源根目录。把资源树放在该目录下并保留其 `Cursor/`、`creation-textures/`、`effect/`、`font/`、`tasks/`、`ui/`、`unity/`、`world/` 等原有相对路径；运行时还会读取 `res24/` 中的松散资源（若所需资源以该形式提供）。

构建入口要求 `ReferenceClient/packages/` 中恰有以下六个完整包：

```text
res_q21_000.pkg
res_q21_001.pkg
res_05diff.pkg
res_ff21.pkg
zpatch_petart.pkg
zpatch_tower10.pkg
```

音乐需放在 `ReferenceClient/music/*.ogg`，构建入口要求 51 个 OGG 文件；生成 Player 时会将其放入 `StreamingAssets/Music/`。音效 WAV 放在 `sndeffect/`；`sndeffect24_pcm/` 可选。`gameassets/tables/` 放置客户端读取的松散 JSON 表格，例如 `mapmusic.json`、`skillsound.json` 和 `skillsound24.json`。`gbk_table.bytes` 是外置编码表，需单独放在 `Assets/Resources/`。

以上目录只说明源码当前读取的相对路径，不代表项目提供资源，也不授予资源使用权。资源缺失、版本不匹配或文件不完整时，部分界面、地图、角色、音乐或音效可能不可用。

## 构建

```bash
make build-macos
make build-windows
```

默认产物位于 `Build/fake-fantasy-online.app` 和 `Build/Windows/fake-fantasy-online.exe`。可覆盖输出路径：

```bash
make build-macos UNITY_EDITOR_BIN="/path/to/Unity" BUILD_PATH="/path/to/fake-fantasy-online.app"
make build-windows UNITY_EDITOR_BIN="/path/to/Unity" WINDOWS_BUILD_PATH="/path/to/fake-fantasy-online.exe"
```

Windows 原生 PowerShell 示例（先安装相应构建模块并准备资源）：

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe" `
  -batchmode -quit -buildTarget Win64 -projectPath (Get-Location).Path `
  -executeMethod FakeFantasyOnline.Editor.ClientBuild.BuildWindows64 -logFile -
```

分发 Player 时需保留整个输出目录，包括可执行文件、`_Data` 目录和 Unity 运行库。

构建会使用当前 Unity 安装中的 Mono 编码程序集，并按运行时布局整理包、音乐与音效。资源准备和构建不包含自动下载步骤。

## 服务端与运行状态

客户端需要可兼容的私服提供账号、角色、世界状态和游戏操作等服务；服务端不在本仓库中。默认调度入口为 `http://127.0.0.1:8088/dispatch.json`，公告入口为同一地址下的 `/changelog.json`。如需更换服务地址，修改 `Assets/FakeFantasyOnline/Runtime/Protocol/PrivateServerOptions.cs` 中的 `LocalDefault()`；网络协议与公钥也需和自行部署的服务端匹配。可选的版本清单地址通过 `FFO_VERSION_MANIFEST_URL_MAC`、`FFO_VERSION_MANIFEST_URL_WIN` 环境变量配置。

本目录包含截至 2026-10-10 当前开发工作区的运行源码和必要工程配置；不包含逆向材料、恢复台账、内部验收工具或游戏素材。源码完整收录不等于已证明与参考客户端全量等价。

源码、资源准备说明和构建入口尚未在本次开源整理中通过 Unity 构建或客户端运行验证。不要将其视作已完成验收的可玩游戏。
