# 世界内入口与情境说明：第一轮完整改进

## 边界与基线

客户端 `b7bddb8671dfbd8d13e5744259ba97a273856bf1`（开始时工作树干净），服务端 `c2b1a03e3a03652bdec69cc35ff620df14f7b974`（已有未发布实现，完整保留）。只改云端源码，不触碰正在旧版本验收的本机。后续独立界面修正已移除 F8、全局入口及跨模块页签；不新增资产，不修改经济数值，不强制把四个系统串为任务。

## 参考与取舍

- Blizzard [Welcome to Challenge Rifts](https://news.blizzard.com/en-us/article/21353255/welcome-to-challenge-rifts)：在专用场景的方尖碑启动挑战。采用其“在世界对象处进入活动”的分工，映射到既有龙城八仙使者，不照搬挑战秘境规则。
- Blizzard [2.0.4 patch notes](https://news.blizzard.com/en-gb/article/13532431/patch-2-0-4-now-live)：城镇方尖碑、Orek、Kadala的位置调整说明活动入口与NPC服务本就分布在世界中。
- Blizzard [Season 20](https://news.blizzard.com/en-us/article/23319442/season-20-now-live)：魔盒作为器物改造系统，与佐敦库勒身份关联；借鉴服务有世界身份，接到本作巧匠左慈。官方旧版魔盒指南当前无法读取，未据此推断细节。
- Blizzard [2.0 PTR / Runewords](https://news.blizzard.com/en-gb/article/24130178/the-2-0-ptr-what-you-need-to-know)：符文通过掉落、镶嵌组合、工匠合成衔接。此为历史PTR设计参考，非现行规则说明；本作仍保留自己的宿序和职业限制。

由此得出的本作设计判断：洞天需要明确的守卫入口；鼎炉需要工匠服务身份；仙遗与星篆优先靠掉落后的物品说明和已有装备界面自然理解，而不是四条强制教学任务。配方/宿阵完整目录应是主动查阅的工具。

## 实施计划

1. 在既有对话菜单添加守卫的演劫洞天、巧匠的周天鼎服务；试炼小仙仅补可选说明。原试炼、传送、合成炉功能不变。沿用现有对话上下文和服务端RPC，不伪造开境权限。
2. 复用原背包/装备 tooltip，增加拥有物品的孔位顺序、未生效原因、星篆属性和下一步。完整宿阵目录改为明确的可选查阅；按所处页签请求需要的目录。
3. 复用 ArtworkSkin、BitmapFont 的现有洞天HUD，显示当下目标、首领阶段、保存与待领奖状态；直接从HUD打开战果。所有阶段取自服务器，不根据本地倒计时宣布失败，不猜测奖励数值或晋阶。
4. 现有问鼎/确认投料流程和不可变重试回执继续保留。结果反馈优先使用服务端确认；未知结果不显示成功。
5. 执行生产源码的managed编译、面板/协议/宿主测试和新增交互回归。无Unity Player构建。记录实际未验证的画面、NPC路径、资源和实机战斗。

## 协议与兼容

本轮不新增DTO字段、数据库迁移、存档状态或权限。现有wire schema 1与服务端权威不变。客户端没有服务端解锁层数与逐项战果字段，不虚构这些内容；精确晋阶/掉落汇总需未来协议设计，不能以库存差值冒充本次奖励。

## 已实施与验证（2026-10-11）

- NPC：守卫直达洞天；巧匠/左慈直达周天鼎；试炼NPC提供可选符钥用途。原对话关闭仅隐藏窗口，保留现有服务端NPC上下文；仍由服务器验证开境。
- 物品：背包/装备原tooltip保留并扩充；UID-less符钥与材料有用途；星篆展示实际目录数据；宿阵规则与未生效原因可见。后续修正移除了玩家图鉴与跨模块页签，保留已有物品的 tooltip 元数据和必要的炼化操作、投料、成品选择。
- 洞天：HUD按服务器阶段呈现目标与短提示，重连/切换角色不重播；HUD可打开待领奖视图，不自动领取；已闭但未领的持久战果不会被误写成已入背包。面板打开时HUD让出指针区域。
- 周天鼎：显示已有材料数量、缺口与三种材料的真实来源，原报价/有效期/明确投料/同请求重试流程保留。
- 独立审查修正：惰性目录跳转、缩放悬浮坐标、旧版星宿字段缺省、已闭洞天待领状态、失败后层数选择、HUD遮挡点击、跨洞天旧提示残留。

执行结果：

- 七个engine-free程序集的642个真实源文件完整编译通过（无Unity替身）。
- 真实宿主/网关/加密集成：38；协议/状态：116；Go/C# DTO：112。
- 实际面板逻辑：40；NPC入口：19；物品情境：15；源码/metadata：81；NPC菜单接线：14。
- 原插件回归：723 contract、148 endgame adapter、186 game action、39 addon UI、83 transport、58 real-Lua runtime，以及Python transport integration；原ArtworkSkin 32。
- 配对服务器 domain、scene、protocol、store、session、gameserver 测试通过；本轮没有服务端源码修改，开始/结束的 staged 与 unstaged patch 字节一致。
- `git diff --check` 通过。没有提交或发布；未触碰本机旧版验收。

新增可复跑脚本：`Tests/Endgame/run-world-entry-contract.sh`、`Tests/Endgame/run-loot-context-contract.sh`。使用与README相同的 `FFO_MONO_ROOT`。

### 精确验收边界

面板、NPC入口单元测试使用明示的Unity/原生替身；NPC菜单以源码接线检查补充，未完整编译Unity依赖的Presentation/Bootstrap/Editor程序集。没有运行Unity编辑器、Player构建、渲染截图或实机战斗。

下一次真实客户端验收需检查：从守卫/巧匠进入各自独立功能，确认 F8 不再打开界面；旧试炼/传送/生活技能/合成炉仍可用；低分辨率和缩放后的tooltip位置与菜单布局；队长与队员开境；打满进度/首领出现/保存中/背包满领奖/断线再领；关闭与重复点击；实际旧素材与字体可用性。场景碰撞、网络时序、视觉节奏不能由上述测试证明。

当前功能入口与完整炼化操作对应关系见 [MODULE-ENTRYPOINTS.md](MODULE-ENTRYPOINTS.md)。角色装备窗口内的“其他装备”保留服务器 14–25 槽位的正常图标、tooltip 与卸下等原生交互。
