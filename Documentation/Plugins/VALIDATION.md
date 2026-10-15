> 历史验证记录：下文保留此前内置实现的测试背景；当前源码边界已经改为可选框架与独立 Lua 插件。最新入口见 [README.md](README.md) 和 `Tests/PluginRuntime/`，框架测试需 `FFO_PLUGIN_FRAMEWORK_PACKAGE`，智能体测试另需 `FFO_AGENT_ADDON`。旧 `PluginHost/GameAgent*` 路径和内置桥启动说明已不适用。独立源码/模拟测试不代表 Unity Player 或移动真机验收。

# 验证记录（2026-10-10）

本记录覆盖插件运行时分支的源码与独立测试，不代表 Unity 实机或真实服务器验收。

## 编译

使用已安装 Unity 6000.4.9f1 的 Roslyn / .NET Standard / Unity 托管引用，独立编译全部 10 个 asmdef 程序集。源码编译通过。没有运行 Unity 导入、Editor/Player 构建或游戏。

## 独立测试

测试入口位于 `Tests/PluginRuntime/`，设置 `UNITY_EDITOR_DIR` 后运行对应 shell 脚本。观察测试还需要 `FFO_COMPILE_DIR` 指向上述程序集输出目录。

- `run-game-contracts.sh`：723 项；不可变数据、严格 JSON、数值精度、主线程队列、取消竞争。
- `run-addon-runtime-tests.sh`：58 项；真实 Lua 解释器，假游戏/UI，生命周期、事件、存储、预算、停用清理和三个示例。
- `run-addon-ui-tests.sh`：39 项；UI 桩测试，布局/拖动、状态、输入捕获、资源释放。
- `check-addon-ui-integration.py`：13 项；实际源码装配和防点击穿透检查。
- `run-agent-transport-contract.sh`：83 项 C# 连接/协议断言，另含 Python 客户端与 CLI 集成测试；使用临时回环端口和假游戏 API。
- `run-game-actions.sh`：38 项；生产动作适配器结合原生对象替身，验证场景/物品/对话过期、重试去重、参数先验证、发送与完成区分、动作所有权及取消。
- `run-game-observations.sh`：51 项；已编译观察适配器的离线不变量、事件窗口、独立游标、查询缓存/版本和线程检查。

原 UI 素材回归保持通过：32 项素材输入契约、200 项背包/社交源码与几何检查、26 项原生滚动源码桩测试。

## 尚未验收的部分

- Unity 实际窗口绘制、字体/外置素材显示、原生鼠标与键盘交互。
- 实际登录后的游戏状态完整性、真实战斗/技能/物品/任务响应。
- 智能体接任务、寻路、战斗、拾取、返回交任务的真实服务器全流程。
- Lua 为进程内解释器，单对象与执行预算不等于每插件完整堆内存隔离。

没有通过修改原生状态伪造服务器成功；发送回执与实际游戏结果保持区别。两项未适配动作 `inventory.move`、`ui.open` 在发现结果中明确标为不可用。

## Endgame adapter extension (2026-10-11)

`run-game-actions.sh` now also accepts `FFO_MONO_ROOT` without a Unity installation. It compiles both actual `PluginGameApi.Endgame` partials and all actual Application `Endgame*.cs` sources alongside the existing production action adapter and shared Foundation JSON codec.

Recorded focused run: **186 assertions**, comprising all **38** original native-action regressions and **148** endgame assertions. Coverage includes discovery/schema limits, canonical Int64 string IDs above 2^53, max-int64 ordered rune inputs, retry equality/order conflicts, quote-only versus execution, stale item/revision/quote/run/cursor guards, binding/trade/native slot25 equipment, pending transient and final authoritative receipts, explicit immutable retry, generation reset, wrong-kind receipt rejection, actual-server `cube.result`, late post-deadline receipts, matching published/current revisions, two-chunk publication, 128-response retention and bounded item/catalog/rift projections. Current-server uncorrelated snapshots are separately tested to publish `snapshotId` while refresh remains sent; synthetic/future correlated snapshots complete only after full publication.

The new projection tests substitute only generic observation plumbing and native owners; they execute production endgame mappings and paging. They do not execute the complete generic observation envelope/cache or establish native Unity behavior. The existing **116** endgame model/codec/Go-fixture contracts and **723** shared plugin contracts also passed after integration. No Unity Editor/Player, live server or real gameplay was run.

`GameObservationTests.cs` was updated for **17** discoverable domains, offline endgame fields, exact string UID filters, required catalog discovery, offset/limit schemas and parser guard cases. **This full compiled-Presentation observation suite was blocked in this run:** `run-game-observations.sh` could not compile because `FakeFantasyOnline.Presentation.dll` in the managed output and `Data/Managed/UnityEngine/UnityEngine.CoreModule.dll` in the supplied runtime were missing. It did not execute any assertions. The focused endgame projection checks above do not replace that suite.

## Ordinary party adapter follow-up (2026-10-11, unpublished)

Baseline: `feature/daoist-endgame` at `c7e9dd3848a774267cefcd1d3d7b5d330d441058`.
The working tree was clean before this follow-up. No commit/push or running-build
replacement is part of these changes.

- Live discovery now includes `party` and seven ordinary native session actions:
  create, invite, join request, nonleader leave, leader dismiss, kick and leader
  transfer. No direct protocol writer, backend shortcut or optimistic membership
  update was added. Native gateway tasks retain shared queued/sent/unknown receipt
  handling. Local scene/API-instance, request-ID replay and journal semantics are
  unchanged.
- Party schemas have bounded strings, a canonical exact decimal-string revision,
  16-character native-UI name bound, and observed scene-qualified target/name
  guards. Native persistent character identity takes precedence over recycled
  entity IDs. Captured immutable-root, revision, role and self-identity checks run
  before dispatch; native queued writes still depend on server authority.
- `run-game-actions.sh`: **254 assertions passed**, including **31** party
  production-projection assertions, **148** existing endgame assertions and
  **75** ordinary action assertions (38 previous + 37 party). Native substitutes
  cover create/join/invite/leave/dismiss/kick/transfer, stale inputs, exact string
  revisions, native name bounds, replay, leadership, persistent self identity,
  zero-leader sentinel, root replacement before version increment, revision/role
  changes during validation, late write settlement, bounded roster pages, exact
  character IDs, optional native fields and read-only projection behavior.
- Shared plugin contracts: **723 assertions passed**.
- External transport: **83 assertions passed**, plus its Python client/CLI
  integration suite.
- All seven engine-free assemblies compiled successfully: Foundation, Domain,
  Application, Protocol, PluginApi, MoonSharp and PluginHost (642 actual source
  files). This explicitly excludes Presentation and Unity-dependent assemblies.
- `GameObservationTests.cs` now covers 18 discoverable domains and adds party
  query schema/parser, projection, cache token, identity and text-budget cases.
  **The full compiled-Presentation observer suite was not executed**: the existing
  workspace lacks `FakeFantasyOnline.Presentation.dll` and Unity managed engine
  references. A syntax/signature compile using the real Application/PluginApi
  assemblies and a temporary API-signature stub passed; this does not establish
  the real full adapter's behavior. Executable production projection checks above
  are narrower than the full observation envelope/cache tests.
- Diff whitespace and shell syntax checks passed.

The companion toolkit's Python wait deadline fix passes 76 tests. It reproduces
and removes a post-deadline extra 1 ms RPC; the exact reported local timeout log
is still needed to match that symptom. Long-chase pathfinding was not established
as a defect and was not changed. No Unity Editor/Player build or launch, live
bridge/server/gameplay call, asset change, dependency, credential, server edit or
user-computer operation was performed. Local acceptance remains on its existing
builds pending review and a separately authorized update.

Remaining party adapter gaps: pending invitation/join approval UI, matchmaking
party lists, native follow behavior and atomic create-and-invite batching are not
exposed. Incoming approval still belongs to the ordinary native player flow.

## Native title adapter extension (2026-10-11)

Offline `run-game-actions.sh`: **338 assertions**, including **59 title adapter assertions**, using actual production title actions/projections and deterministic native owners. Covers stale instance/revision, wrong page/loading/update/modal, secret argument rejection before retention (including ordinary unknown actions), replay, native server/name identity, local confirmation, callback outcomes/timeouts/redaction, owner replacement, connected matching new-world completion, title teardown, bounded immutable title observations and ephemeral sensitive submission. Separate native bridge runner covers 32 assertions plus source routing checks.

Engine-free source compile passed for Foundation (5), Domain (1), Application (147), Protocol (221), PluginApi (10), MoonSharp (248), and PluginHost (11), **643 source files**. Unity reference assemblies are absent in the current environment, so full Presentation/Unity compilation and Player/real-server verification remain unverified. No real login or credential transmission occurred in these tests.
