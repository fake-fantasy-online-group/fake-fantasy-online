# 插件与外部智能体运行时

插件框架与智能体桥已拆为独立可选包，客户端只保留游戏 API、游戏风格的 UI/输入适配与最小注册入口。不安装框架时，不创建插件管理器、Lua 解释器或智能体接口。

框架源码包 `com.fakefantasyonline.plugin-framework` 位于 `fake-fantasy-agent-play/packages/plugin-framework`。框架自身安装/移除需要 Unity 重建 Player；普通插件只支持 Lua，可在运行时安装、更新、启用、停用与重载。不接受 C# DLL、程序集、XML UI 或本地二进制插件。

智能体插件位于独立仓库 `addons/agent-bridge`，其 Lua 源码拥有 `ffo-agent/1` 方法映射。框架仅提供受权限控制的通用本机 JSON 通信与游戏 API 桥，不内置智能体方法路由。AI 插件不存在或未启用时没有监听端口。

## 使用入口

- 安装框架后，客户端右上角“插件”按钮或 F10 打开插件管理器。
- 插件代码放在 `Assets/StreamingAssets/AddOns/<插件目录>/`，每个插件包含 `addon.json` 和入口 Lua 文件。扫描后手动启用，可停用、重载。
- 附带字符信息、背包/Buff 观察与基础示例。插件配置保存至客户端 `persistentDataPath/AddOns`。
- 外部智能体在安装 `agent-bridge` Lua 插件并在管理器启用后，使用 `examples/agents/game_agent.py`。默认连接运行客户端的同一台机器上的 `127.0.0.1:17845`。
- 插件目录、生命周期、Lua API 和配置说明见 [ADDONS.md](ADDONS.md)。
- 外部协议、Python 用法及行为说明见 [EXTERNAL_AGENTS.md](EXTERNAL_AGENTS.md)。通过 `describe` 获取当前查询及动作定义、参数和可用性。
- 原生演劫系统的共享观察、目录/嵌篆/问鼎/洞天动作、精确 UID 与回执重试说明见 [ENDGAME.md](ENDGAME.md)。

## 实现分层

1. `PluginApi`：不可变状态快照、查询定义、动作回执、独立事件游标、插件 UI 控件协议。
2. `Presentation/ReferenceAssets/PluginGameApi*`：从现有游戏状态读取数据，将游戏动作交给现有移动、战斗、物品及对话处理代码。
3. 独立框架包 `Runtime/Host/AddonHost*`：Lua 插件加载、生命周期、事件、配置、控件和执行预算。
4. 框架 `LoopbackJsonServer` 提供有界本机通信，`AddonHost.Loopback` 用临时句柄传递请求；智能体插件 `main.lua` 定义协议与方法路由。整数与敏感登录参数不转换为 Lua 浮点数或可持久化的凭据字段。
5. 独立框架包 `Runtime/Bootstrap/PluginBootstrap`：插件装配、管理控制器、更新循环与停止清理。客户端 `OptionalPluginRuntime` 仅调用可选注册工厂。

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

游戏素材继续使用外置资源目录，不包含在本功能提交中。框架包 `Runtime/ThirdParty/MoonSharp` 为解释器源码依赖，许可证和上游/补丁说明随源码提供。

源码与独立测试的详细范围见 [VALIDATION.md](VALIDATION.md)。
