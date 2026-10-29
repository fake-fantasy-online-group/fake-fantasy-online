# 任务用途准入 WIP checkpoint — 2026-10-29 04:05 UTC

用户优先处理背包整理/堆叠，任务门控暂停。基线是已集成前6项的client95b51f5/server29236d2。不要直接合并/发布此checkpoint。

当前生产白名单仅三项：29400↔356净幢、29900↔359天塔、30000↔361凌霄。Active0/Deliver1可见可进；无记录/已放弃/Finished2拒绝。未增加一次机会、队友资格、清场或协议。普通产出副本/特殊试炼/洞天保持原规则。

29200↔355九转仅待处理候选，已从生产门控白名单撤下：缺少发布/步骤NPC无泪公，init41会disable该任务，LoadQuests剔除定义；已存Active无法通过当前ActiveQuestsOf正确下发。不能宣称任务可接或用门控锁掉既有路径。

保留0x8049全部原始行，客户端只过滤显示，closure捕获原i，因此不重排0x1072索引。TaskClient现成Active/Done驱动菜单与旧点击复核。server共用openPlayerDungeon、directTeleport和普通登录恢复复核自己的QuestLog；RiftRunID与已有洞天恢复不继承任务图模板限制。

暂停前已完成短测（撤下355之前的版本）：393客户端断言、domain/scene状态矩阵通过。撤下355后的生产/测试白名单尚未重跑。新写的store finished-history、direct请求、transport状态变化测试尚未运行；完整Presentation/其他回归尚未跑。还需增加实际重登实例测试、检查scene transfer nil安全/回原图路径、确认三任务定义正常加载、跨语言映射一致、更新旧README声明，交独立review。

四组队天魔24400/24500/26800/26900现有明确装备/真装备产出，root已指示不gate。准确任务关联分别309/311/313/307，关联不等于入口限制。其余分类由classify_quest_only_dungeons继续提供。
