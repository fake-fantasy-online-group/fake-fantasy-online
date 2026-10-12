# 插件与外部智能体运行时

本分支增加可加载 Lua 插件、插件界面及外部智能体接口。它们共用同一个 `IGameApi`，由客户端主线程调用现有游戏逻辑。

## 使用入口

- 客户端右上角“插件”按钮或 F10 打开插件管理器。
- 插件代码放在 `Assets/StreamingAssets/AddOns/<插件目录>/`，每个插件包含 `addon.json` 和入口 Lua 文件。扫描后手动启用，可停用、重载。
- 附带字符信息、背包/Buff 观察与基础示例。插件配置保存至客户端 `persistentDataPath/AddOns`。
- 外部智能体在管理器开启连接后，使用 `examples/agents/game_agent.py`。默认连接运行客户端的同一台机器上的 `127.0.0.1:17845`。
- 插件目录、生命周期、Lua API 和配置说明见 [ADDONS.md](ADDONS.md)。
- 外部协议、Python 用法及行为说明见 [EXTERNAL_AGENTS.md](EXTERNAL_AGENTS.md)。通过 `describe` 获取当前查询及动作定义、参数和可用性。

## 实现分层

1. `PluginApi`：不可变状态快照、查询定义、动作回执、独立事件游标、插件 UI 控件协议。
2. `Presentation/ReferenceAssets/PluginGameApi*`：从现有游戏状态读取数据，将游戏动作交给现有移动、战斗、物品及对话处理代码。
3. `PluginHost/AddonHost*`：Lua 插件加载、生命周期、事件、配置、控件和执行预算。
4. `PluginHost/GameAgent*`：JSON 行协议及 TCP 连接；通过主线程队列执行游戏 API。
5. `Bootstrap/PluginBootstrap`：客户端装配、插件管理器、更新循环与停止清理。

## 行为约定

- 查询覆盖世界、角色、实体、NPC、掉落、背包、装备、技能、Buff、目标、任务、对话与 UI。
- 可用动作及参数以 `describe.actions` 为准，缺失的适配明确返回不可用。
- 指令发送成功只报告 `sent`。背包变化、任务奖励、战斗结果仍需观察游戏状态确认。
- 每次行动可携带场景版本及请求 ID。相同的保留请求 ID 可重试，内容不同则拒绝；保留窗口外需重新观察后决策。
- 每个消费者持有自己的事件游标，历史窗口缺失时需要重新获取快照。
- 插件停用和客户端退出清理其资源；已经发送至游戏服务器的请求无法撤回。

## 验证范围

`Tests/PluginRuntime/` 包含纯 C# 协议/队列测试、Lua 解释器测试、带假游戏 API 的连接测试，以及 UI/适配器测试。脚本头部说明各自使用的桩、引用和运行方式。

独立源码编译、模拟 API 及桩测试不等同于 Unity 实机验收。此轮不运行 Unity/Player 构建；实际画面、真实服务器交互及完整任务流程仍需在可运行客户端中验收。

游戏素材继续使用外置资源目录，不包含在本功能提交中。`Assets/ThirdParty/MoonSharp` 为解释器源码依赖，许可证和上游/补丁说明随源码提供。

源码与独立测试的详细范围见 [VALIDATION.md](VALIDATION.md)。
