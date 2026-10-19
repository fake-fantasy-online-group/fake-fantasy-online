# 本地帧率与分辨率配置

登录页不需要帧率或分辨率控件。启动时读取 `Application.persistentDataPath/display-settings.ini`，并在首个场景载入后应用。该路径独立于游戏素材目录、安装目录和资源下载包；无需修改 `resource-bootstrap.json`。

## 使用

1. 启动一次客户端，正常退出。目录可写时会生成带说明的 UTF-8 模板。
2. 用文本编辑器修改 `display-settings.ini`，例如：

   ```ini
   fps=60
   resolution=1280x800
   ```

3. 保存后重新启动客户端。文件没有实时监听；建议只在客户端退出后编辑。

文件只接受两个键，区分大小写，不使用 `[section]`。允许空行、空白，以及以 `#` 或 `;` 开头的整行注释；不接受行尾注释、重复键或额外键。最大 16 KiB，UTF-8，可带 BOM。不要写入账号、密码、令牌或服务器配置。

### fps

- `default`：跟随平台默认。移动端（包括 Android）请求 60 fps，桌面保留原 120 档位。新生成文件使用这个值，明确区分“跟随默认”和用户指定的数值。
- `60`：原“标准 60”模式。
- `120`：原“流畅 120+”模式，仍可显式选择。
- `refresh`：跟随显示器刷新率。

这些值沿用原显示同步档位，并非保证测得的每秒帧数。桌面继续使用垂直同步分频；例如 144 Hz 显示器选择 `60` 时，原算法使用二分频，目标实际为 72 Hz。Android 使用 `Application.targetFrameRate`，`refresh` 显式传入当前刷新率，避免 Unity 的 `-1` 在移动端变成默认 30 fps。最终帧率仍取决于设备刷新率、平台调度、性能和省电设置。[Unity 帧率 API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-targetFrameRate.html)

### resolution

- `default`：不发出启动分辨率变更，保留 Player/系统自己的启动窗口尺寸。新安装的默认值。
- `native`：使用主显示器报告的原生尺寸，进入全屏窗口模式。
- 固定档位：`800x600`、`1024x768`、`1280x800`、`1600x900`、`1920x1080`、`2560x1440`、`3840x2160`。

固定档位保持现有桌面行为：尺寸触及显示器边界时使用原生尺寸全屏，否则使用窗口模式，并按现有窗口可用客户区限制尺寸。Android 始终请求全屏，由系统决定实际渲染尺寸；建议先用 `default` 或 `native`，固定档位不保证适配每种手机纵横比。配置不改变系统显示器的物理分辨率。[Unity 分辨率 API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Screen.SetResolution.html)

## 文件位置

以运行时 `Application.persistentDataPath` 为准，文件名始终为 `display-settings.ini`。当前工程的 company/product 均为 `fake-fantasy-online`。常见目录：

- Windows：`%USERPROFILE%\AppData\LocalLow\fake-fantasy-online\fake-fantasy-online\`
- macOS：`~/Library/Application Support/unity.fake-fantasy-online.fake-fantasy-online/`；已有 Editor 数据目录时也可能沿用 `~/Library/Application Support/fake-fantasy-online/fake-fantasy-online/`
- Linux：`$XDG_CONFIG_HOME/unity3d/fake-fantasy-online/fake-fantasy-online/`，未设置时通常从 `~/.config/` 起
- Android：通常为 `/storage/emulated/0/Android/data/<实际包名>/files/`。不同系统/用户位置可能不同；Android 对 `Android/data` 的访问可能受限制，需要设备允许的文件管理或 USB/ADB 方式。本文不假定实际发行包名，也不承诺所有设备上的普通文件管理器都能编辑。

这些位置采用 Unity 的跨平台持久化路径规则，未硬编码桌面路径或申请新的存储权限。[Unity 持久化路径 API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html)

## 默认、旧偏好与容错

- 缺文件时，仅尝试导入原有有效 `opt_fps`、`res_w`、`res_h`。没有有效旧帧率偏好时生成 `fps=default`：移动端请求 60，桌面保留 120。没有有效旧分辨率偏好时仍为 `resolution=default`，不会强制把新安装窗口改成 1024×768，也不降低移动端原生渲染分辨率或画质。生成的模板不含任何账号或凭据。
- 已有 `fps=60`、`fps=120`、`fps=refresh` 和有效旧 `opt_fps` 均保留。旧版自动生成的 `fps=120` 与用户主动选择的 `120` 没有可靠来源标记，因此升级不能静默把它改为 `60`；已有安装是否降到 60 取决于当前文件/旧偏好。新版模板使用 `fps=default`，以免后续再猜测数值来源。
- 文件存在后，它是显示偏好的唯一持久化来源。缺少的键使用平台默认值；任何非法值、未知/重复键、无效编码或超大文件使整份显示配置退回平台默认帧率和 `resolution=default`（移动端 60、桌面 120），不读取旧偏好来覆盖它，也不自动重写坏文件。
- 读写失败不会阻断登录或资源启动流程。日志仅提示通用错误，不记录配置内容。
- 其他显示/音量/操作偏好仍走现有设置逻辑，不在这个文件中存储。普通设置“保存”不会重写该文件。
- 游戏内既有系统设置窗口的分辨率操作写入同一个文件，并保留文件里的帧率值，包括 `fps=default` 标记。旧 `res_w/res_h/opt_fps` 不再写入；旧值不删除，便于恢复。保存使用同目录临时文件加原子替换；不支持替换或文件不可写时，保留原文件，分辨率只在当前会话应用。
- 要恢复新安装默认，直接把文件内容改为 `fps=default`、`resolution=default`。只想为已有手机安装降低帧率时，可把 `fps` 改为 `60`，保留原有分辨率键，然后重启。删除文件会再次尝试导入旧偏好，不等同于重置。
- 旧版客户端可能不识别新增的 `fps=default`。需要回退到旧版时，先退出游戏，把它改为明确的 `60`、`120` 或 `refresh`，保留原有分辨率配置。

## 验证范围

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Tests/DisplaySettings/run-local-display-config-contract.sh
bash Tests/Mobile/run-settings-scale-contract.sh
bash Tests/SettingsMenu/run-settings-menu-contract.sh
python3 Tests/SettingsMenu/check-entry-coverage.py
python3 Documentation/tools/compile-current-presentation.py
```

显示配置契约直接编译并运行生产解析、文件读写、加载、保存和启动代码；Unity/窗口/PlayerPrefs API 使用显式测试替身。覆盖支持值、旧偏好、启动默认、重复和无效输入、UTF-8、大小上限、不可写目标、不覆盖已有模板、原子替换、其他设置保存、同文件分辨率更新以及桌面/移动端分支。还覆盖 60/90/120/144 Hz 下的移动默认 60 与桌面默认 120、旧版数值模板的保守迁移、显式 `refresh`、默认来源标记的保存/重载和重置与删除的区别。完整 Presentation 编译使用恢复的真实 Unity API 引用。

2026-10-19 移动帧率默认变更检查：显示配置 475 项断言通过；既有系统设置适配/输入/保存/取消 3220 项断言、设置菜单 18 项断言和入口静态检查重新通过。重新编译当前 5 个无引擎核心程序集后，含同期 UI 优化的全部 1221 个 Presentation 源文件使用恢复的真实 Unity 引用编译通过；这些检查不等于新版真机验证。

这不替代 Unity Player、Android 真机、实际屏幕刷新率/分辨率或特定设备文件访问的验证；本次没有运行 Unity、构建 Player/APK 或访问 Mac/Win 设备。默认帧率降低的是请求的帧率上限，尚未测量真机温度、功耗或持续帧率，不能据此声称发热问题已消失。
