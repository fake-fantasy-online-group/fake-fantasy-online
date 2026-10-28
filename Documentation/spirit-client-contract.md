# 九类灵：客户端交互与验收

## 范围

支持鞋、拳、羽、衣、项、面、帽、法宝、武器灵。戒灵不进入此流程。
NPC 的地图、实体、名称、脚本由当前服务器定义和当前世界对象共同校验；关闭、换图、重连、实体替换后旧回调不再有效。

- 原 NPC 菜单：解封、加工 / 回退、归元。
- 原 PKG 的 27 个背景，原按钮路径，原双槽位置；不复制素材。
- 背包右键或携带物品点击槽位：对应灵、对应元神。选中的是实际袋页/位置/物品 ID/可用 UID，不虚构库存。
- 加工栏展示旧值 + 本次值 = 总值，元神消耗；悬停展示每属性加工次数、完美次数、当前铜币与回退消耗。
- 单属性增量 20 的完美结果由服务端保存。按钮使用 CanUnseal/CanProcess/CanRollback/CanReset，不用表面等级 15 误挡其他属性补齐。
- 元神不足等结果由服务端右下角提示；本地拒绝错误材料也用同一现有提示入口。
- 右键解封灵、九类分离符、服务端公布的灵外形道具：复用装备目标光标，可选背包或穿戴装备。保留普通换形、旧威能卷轴流程。
- 原生装备说明继续由服务端生成，包含独立灵、融合灵和属性；不叠加一套客户端重算的说明格式。
- 21 个缺少原条件/加成配置的衣灵后期外形不由客户端猜测补齐。

## 线协议与并发

复用已存在的 Endgame JSON 通道，协商 `endgame.spirit.v1`；原 Lua Handle 参数 0/1/2/3 不作为网络 opcode。
服务器 hello 提供九类定义和支持的 appearance IDs。
`spirit.preview` 只读，不随机；`unseal/process/rollback/reset/fuse/separate/appearance` 使用不可变 requestId、expectedRevision。

材料数量不由客户端提交，服务器按实际配方扣除。UI 保留选择，不挪动物品。选择更换/关闭使旧预览失效。
重复点击未决动作只重试原 requestId，不生成第二笔交易。背包原生发布后必须等权威快照；目标工具类型、UID、代际和 revision 均复核。
旧封印灵解封首次分配 UID，仅允许精确成功回执加对应提交快照重新绑定原槽位。

## 验收证据与限制

实际 C# 编译、确定性 Unity/Artwork 替身，**不是 Unity 引擎或三端游戏实机测试**：

- SpiritWireContracts：103 项（含实际 Go 服务端生成的 hello / preview / result）。
- EndgameUiContract：3211 项（原 2953 + 灵 258）。
- EndgameWorldEntryContract：83 项。
- 原生背包/托管 ItemActions 路由：34 项。
- 移动原生动作链：99 项。
- 原协议 234 项；成长协议 95 项。
- 27 个原背景 + 45 个按钮路径对本地原 PKG 索引解析通过，27 布局边界契约通过。
- 原生源集成 guards、git diff --check 通过。

`run-spirit-wire-contract.sh` 支持已有 Mono，或 FFO_DOTNET_ROOT + FFO_ROSLYN_CSC。
原 `run-ui-contract.sh`、`run-world-entry-contract.sh` 已包含本次新增测试；云端使用同一源清单通过 Roslyn/.NET 6 执行。
可选 `FFO_PKG_INDEX` 传给 `spirit_source_contract.py`，只查路径，不把素材加入仓库或产物。

发布前剩余现场检查：真实 Unity 的像素/字体/触控表现、联网 NPC 距离拒绝提示、三端装备外形展示。客户端此提交不触发打包、合并或发布。
