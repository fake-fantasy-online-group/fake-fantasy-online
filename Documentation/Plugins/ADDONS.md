# Lua 插件开发

安装可选插件框架后，按 F10 打开插件管理，扫描、启用、停用或重载插件。插件默认不自动启用。
源码放在 `Assets/StreamingAssets/AddOns/<id>/`；Player 中使用对应的 StreamingAssets 目录。
仅安装和启用你信任的插件。安装路径与 Git 来源管理由可选框架负责，客户端核心不包含加载器。

已提供三个真实 Lua 示例：
- `hello-world`：游戏状态欢迎面板
- `character-info`：可拖动角色面板、生命/法力进度条、刷新按钮和独立保存数据
- `bag-buff-observer`：有数量上限的背包与增益列表；剩余时间明确标为服务器收到时的值

## 包格式

目录名必须等于清单 id；id 使用小写字母开头，随后可用小写字母、数字、下划线或连字符，最多 64 字符。
每个包包含 `addon.json` 和 Lua 入口源码：

```json
{
  "id": "my-addon",
  "name": "我的插件",
  "version": "1.0.0",
  "apiVersion": "1.0",
  "entry": "main.lua",
  "dependencies": [],
  "capabilities": ["game.observe", "game.events", "ui.panel", "storage.local"]
}
```

`dependencies` 是必须先启用的插件 ID；缺失依赖和依赖循环会使启用失败。停用依赖也会停用其使用者；重载依赖会清理旧运行实例并重新启用原来活跃的使用者。
清单上限 16 KiB，入口源码上限 128 KiB。入口必须为包内相对 `.lua` 路径；禁止路径穿越、符号链接、预编译字节码和动态文件加载。
单个 host 最多扫描 64 个目录。包版本是说明信息；目前依赖不支持版本范围。

能力名称：
- `game.observe`：查询状态、查询/动作发现与动作状态
- `game.act`：发起游戏操作及取消本插件发起的动作
- `game.events`：事件轮询及订阅
- `ui.panel`：创建及更新插件面板
- `storage.local`：独立保存数据

启用会授予清单声明且 host 支持的能力。能力声明用于控制桥接访问；不是对恶意本地源码的安全认证。

## 生命周期与调度

```lua
function on_load() end
function on_enable() end
function on_update(delta_seconds) end
function on_event(event) end
function on_ui(event) end
function on_disable() end
function on_unload() end
```

所有回调可选。顺序为入口代码、on_load、on_enable，然后更新/事件/UI 回调。启用请求被接受后状态先为 Loading，Tick 完成后才为 Enabled。
整个运行实例使用私有调度协程，避免每帧重新分配解释器大栈。每次 Tick 对每个插件最多执行 10,000 条解释器指令；单次回调最多 50 个调度片段。超限或脚本异常只停止对应插件，并释放其面板、订阅和本地动作。`addon.yield()` 可主动让出本帧；它不会重置回调总预算。
全局帧调度预算为软限制 3 毫秒，已开始的有界 C# API 调用不会在中途强制中断。所有 API 必须在构建 host 的线程使用。
停用立刻丢弃待执行回调，on_disable/on_unload 各最多执行一个指令片段，然后无条件清理；已发给服务器的指令无法撤回，取消仅能停止本地仍受控的工作。

## 游戏 API

```lua
local queries = game.queries()
local actions = game.actions()
local snapshot = game.observe("player")
local player = snapshot.data
if player.available then print(player.name) end

-- 必须声明 game.act。只使用发现结果中 available=true 的动作及其参数。
local receipt = game.act("target.select", {entityId = "从观察中获取的实体 ID"}, "请求去重标识", snapshot.sceneRevision)
local status = game.action_status(receipt.actionId)
local cancelled = game.cancel(receipt.actionId)
```

`observe` 返回 `{domain,sceneRevision,revision,data}`。查询参数为第二个可选 table。不可用的数据返回 available/code/reason，不能把未加载理解为真实的空状态。
`game.queries()` 返回 `{domain,description,arguments}` 列表；`game.actions()` 返回 `{name,description,arguments,available,unavailableReason}` 列表。
`act(name,arguments?,requestId?,expectedSceneRevision?)` 返回 `{actionId,requestId,name,state,code,message,data}`。state 使用小写：queued、sent、running、completed、rejected、cancelled、timedout、unknown。sent 仅表示发送；不能当作服务端执行成功。
可选 requestId 自动加 `<addon-id>:` 前缀，前缀加后缀最多 128 字符。`game.cancel` 只接受该实例发起、仍被其跟踪的动作。
游戏数据始终转换为新 Lua table，不暴露 CLR/Unity 对象。循环、混合键表、稀疏数组和非有限数字不能跨越桥接。空 table 作为 JSON 对象；数组须连续、从 1 开始。
超过 Lua double 安全整数范围的 Int64/UInt64，以及无法无损往返的 decimal，会以十进制字符串返回。场景版本/事件游标输入支持非负 Int64 十进制字符串。

```lua
function on_event(event)
    -- event = {name, sequence, sceneRevision, data}
    if event.name == "events.resync_required" then
        -- 事件缓冲区已出现缺口，重新查询完整状态。
    end
end
local subscription = game.subscribe("observation.changed", function(event) end)
game.unsubscribe(subscription)
local batch = game.events(0, 32) -- {events,nextSequence,resyncRequired}
```

自动事件游标按插件隔离；手动 events 的游标由调用者保存。订阅最多 6 个，可用 `*` 匹配所有事件；发生缺口也会通知匹配的订阅。若同时定义 on_event 和 subscribe，两者都会收到事件。
每个插件每次 Tick 最多 64 次原生桥接/有界辅助函数调用，待执行回调最多 128 个；事件过载会使插件报错，而不会无限增长队列。

## UI 与保存数据

```lua
function on_enable()
    ui.panel({id="status", title="状态", text="等待数据"})
    ui.set_text("status", "已加载")
    local count = storage.get("count") or 0
    storage.set("count", count + 1)
end
```

面板 ID 自动命名为 `<addon-id>:<local-id>`，插件只能操作自己注册的面板。本地 ID 遵循与插件 ID 相同的规则。
`ui.widget_panel` 提供 label、button、progress，布局参考 character-info；参数为 id/title/width/height/anchor/x/y/draggable/widgets。
anchor 可为 TopLeft、TopRight、BottomLeft、BottomRight、Center。
控件参数为 id/kind/text/x/y/width/height/progress/enabled。
更新方法为 `ui.set_widget_text(panel,widget,text)`、`ui.set_progress(panel,widget,value)`、`ui.show(panel,visible)`、`ui.close(panel)`。
`on_ui` 接收 `{panelId,widgetId,name}`，panelId 已去除命名空间，name 为 click/close/moved。UI 事件在主线程 Tick 排队执行，不在 OnGUI 中直接运行脚本。
每个插件最多 8 个面板，每个面板最多 64 个控件。UI 使用现有 ArtworkSkin 和外置资源，不附带游戏图片、字体或音效。

保存数据位于 `Application.persistentDataPath/AddOns/<id>/storage.json`。键最多 128 字符，整个文件最多 64 KiB；每次成功 set 使用临时文件替换，超额失败保留旧数据。`storage.set(key,nil)` 删除该键。
停用/重载释放内存状态，但保留保存文件。启用状态目前不会跨启动自动恢复。

## 安全与边界

Lua 环境没有 IO、OS、require/load、debug、自动 CLR 类型暴露、元表、pcall 或可自行创建的协程。
只提供少量有界 string/table 函数：string.len/lower/upper/sub/rep；table.insert/remove/concat。
不提供 string 模式匹配、format、table.sort 或 unpack，避免原生调用或嵌套 Lua 回调绕过调度预算。
string.rep/concat 结果最多 32,768 字符，table 原生操作最多 4,096 个数组项；解释器 VM 的字符串拼接也在分配前限制为 32,768 字符，每个 Lua table 最多 8,192 个条目。

这些保护可以终止普通死循环、协程长任务以及明显的单对象膨胀。解释器仍在游戏进程中运行，没有每插件总堆配额，也不能安全隔离蓄意消耗整体内存或利用解释器缺陷的恶意源码。源码解析和同步游戏 API 不是可抢占操作。不要启用不可信插件，也不要把本机插件目录开放为远程代码执行入口。

MoonSharp 官方源代码以固定版本纳入仓库，授权与三处可选限制补丁见 `Assets/ThirdParty/MoonSharp/README.md`。

## 不启动 Unity 的运行测试

```bash
UNITY_EDITOR_DIR=/path/to/Unity/Editor Tests/PluginRuntime/run-addon-runtime-tests.sh
```

该测试执行真实 Lua 解释器和 host，游戏/UI 为 fakes，覆盖生命周期、依赖、重载、指令预算、隔离、保存数据、API 桥接与示例脚本。它不代表 Unity 导入、真机画面或服务器游戏行为已经验收。


## 可选框架与 WoW 风格子集

框架只加载 Lua；不加载下载的 C#、DLL、XML 或可执行文件。规范目录清单为 `<id>/<id>.toc`，旧 `addon.json` 保持兼容。`.toc` 的 `## Interface: 1` 是本项目接口版本，非 WoW 客户端版本；Lua 文件按清单顺序执行，各文件拥有独立局部变量，`local addonName, namespace = ...` 共享插件命名空间。

支持 `CreateFrame("Frame")` 的匿名、非视觉事件容器与 `RegisterEvent`、`UnregisterEvent`、`UnregisterAllEvents`、`SetScript("OnEvent", fn)`、`SetScript("OnUpdate", fn)`。处理函数按现有指令/队列预算调度。`ADDON_LOADED` 在源码执行、已保存变量恢复后触发，第三个实参为插件 ID；`PLAYER_LOGIN` 只在 `world.worldReady` 实际变为 true 时触发（需 `game.observe`）。原有游戏事件名原样提供，不模拟全部 WoW 事件、控件或函数。

`SavedVariables` 仅持久化声明的全局 JSON 兼容值，需要 `storage.local` 权限；在停用、重载、退出时保存，与插件本地存储共用 64 KiB 配额。`SavedVariablesPerCharacter` 明确拒绝，直到宿主提供稳定的账户/服务器/角色身份。不要保存凭据。

`net.loopback` 允许启用后监听本机；`game.remote` 允许通过临时请求句柄使用保留外部 requestId 的游戏操作；`game.sensitive-login` 另行允许不保留凭据的登录操作。这些权限需在安装/升级时明确审核。普通 `game.act` 的插件请求 ID 隔离不变。

测试脚本通过 `FFO_PLUGIN_FRAMEWORK_PACKAGE` 指定框架目录，真实智能体 Lua 测试另设 `FFO_AGENT_ADDON` 指向其源码目录。源码/模拟运行测试不等同于 Unity Android/iOS 真机验证。
