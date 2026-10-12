# Earlier status icon audit, superseded by original SmallMap recovery

> The JSON/skill-only audit below was incomplete. Original `res_ff21.pkg/zipbin/ov_exceptdesc.bin` has a per-level `ResExceptDesc.SmallMap` field, verified by `ov_res.bin`, which the server importer omitted. The final paired-server report is `docs/native-status-icons.md`; it audits all 203 positive status IDs and all 40 level slots, restores exact native fields, and preserves authored zero. The earlier source-icon propagation and zero-refresh preservation conclusions below must not be used as original status artwork evidence. The client now accepts authoritative wire icons including zero and does not replace them with previous/source skill artwork. No new substitute artwork was introduced by the native recovery.


## Evidence and scope (2026-10-12)

The installed resource index and skill metadata were inspected in the cloud. The complete raw resource inventory remains outside the repository. All 149 historical five-profession skill rows have positive icon IDs present in the installed item resource index. The installed `skills.json` has 222 rows: 190 positive icon IDs, all present, and 32 zero-icon rows. No independent status/buff/except JSON table exists among the 28 installed table files or inspected packages; there are no skill rows with IDs 1000–1300. Historical metadata is a local historical artifact, not an independently authenticated publisher original.

An observed live weakening row had `skillId=12005`, `iconId=7069`, name `衰弱`. Its installed skill row has icon zero. This proves that server-positive resource IDs must win, and that treating a status/skill identity as a resource ID is incorrect. Resource index presence is not proof of decoded pixels or live Player rendering.

## Root causes and changes

- Server `LoadStatuses` has no separate native icon column. Normal active skills, passive skill triggers, pet AI, item use, and reveal applications already call `applyStatusSource`, preserving an explicit native icon or copying the actual skill/item resource ID. These working paths were retained.
- Poison-on-hit and ice-shield retaliation passed through `applyStatus` without an originating SkillDef, losing their existing carrier status icon. They now copy presentation metadata from the responsible carrier; duration, level, chance, effects and target range are unchanged.
- Seven source-bound equipment auras supplied synthetic identities but zero resource IDs. Each now carries a deliberate existing-art reuse in both self and entity snapshots: white tiger 5017 (铁城神盾), ember 7084 (火盾术), holy feather 7006 (光盾术), empty talisman 5062 (魔镜术), north ice 7093 (冰盾术), backward flame 7085 (火系掌握), paper frost 7086 (冰系掌握). These are new aura art assignments, not claims of original canonical status mappings. Their existing world-effect aliases, radius and permanent sentinels are untouched.
- `Win05.SkillIconId` previously returned zero immediately when negotiated gameplay metadata was incomplete, hiding installed skill artwork. Only a positive negotiated icon now overrides installed art. Gameplay targeting still remains authoritative.
- Both native and compatibility BuffSnapshot receivers preserve a previous positive icon on a zero refresh only for the same skill identity and status name, then try the existing local skill resource mapping. Positive wire icons always win; empty snapshots still clear.
- Monster status receipts likewise preserve matching prior icon metadata. Baked status texture signatures now include icon and beneficial polarity, so changing art with the same status identity repaints.
- `GameHud.BuffIcon` no longer permanently caches null while resource loading is unavailable. Successful textures remain cached.

The existing packet layouts remain unchanged. Self BuffSnapshot carries source skill identity (or status identity if no skill source), icon resource ID and signed seconds separately. Entity snapshots carry status identity, resource ID and signed milliseconds separately. Unknown resource metadata remains unknown; no universal generated placeholder was added.

## Verification

- `Tests/Endgame/run-status-icon-contract.py`: 20 assertions execute actual production icon mapping, receipt preservation, texture cache, monster receipt and signature methods with deterministic rendering/resource substitutes; checks both receive call sites.
- `Tests/Endgame/run-aura-buff-contract.py`: existing source-bound, permanent, finite tooltip and world-effect alias contracts.
- Paired server `status_icon_test.go` in scene: source positive/negative status preservation, secondary poison/freeze identity and icon propagation, all seven aura self/entity snapshots and no countdown.
- Paired server protocol `status_icon_test.go`: exact field order and distinct identity/resource/permanent values for both packet types.

These checks do not launch Unity or prove live rendered pixels. No binary assets, full resource indexes, commits or pushes were produced.
