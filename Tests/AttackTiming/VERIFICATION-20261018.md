# Attack windup/recovery regression, 2026-10-18

Base: `5d79234`. Cloud-only verification. No Unity Editor, rendered animation/device execution, Player build, deployment, or live-server test claimed.

## Reproduced causes

- `CombatWorld.Update` formerly cleared Recovery whenever a moving monster crossed the range boundary (or LOS failed). Returning in range began a new request and restarted `PlayerController.PlayAttack` before the previous interval completed. The animation is not itself canceled by moving monsters: `StartActPose` resets elapsed time when another request starts its pose.
- Mobile attack is a release command (`GameHud.MobileInput`), not a held-repeat command. Each release called `SetTarget`; repeating the current target cleared the attack state/time. Desktop repeated target clicks share that entry.
- While already steering, a second-finger attack could reactivate autoattack, whose approach/stop code subsequently overwrote or cleared the joystick path. The moving-player attack gate must wait while manual navigation retains ownership.

## Change

Recovery advances before transient range/LOS gates. Attack initiation still uses the same range, LOS, connected, riding and movement checks. Same-target/same-intent selection retains its clock (including PvP); intent revision notification and selection remain. Changed targets, explicit stop, death, and skill casts retain their cancellation behavior. Mobile steering owns navigation until released.

No range enlargement, animation suppression, request/opcode changes, damage edits, server edits, or attack-interval formula changes.

Server inspection: `server/internal/game/scene/dispatch.go:onAttack` validates target/status/cooldown and reserves cooldown at acceptance; `scene.go:resolveAttacks` resolves the queued strike later. There is no acceptance handshake in the existing client send-and-pose path; this patch does not claim to eliminate all network/server rejections. Keeping local recovery prevents the reproducible premature request/pose restart that generates extra cooldown rejections.

## Executed checks

With `source Documentation/tools/activate-dotnet-test-toolchain.sh`:

- `bash Tests/AttackTiming/run-contract.sh`: 35 assertions execute the actual Update, Targeting, TryAttack, CastState and PlayerController.Approach carriers against explicit Unity/network boundaries. Covers moving-range and LOS excursions, normal stationary cadence, repeated input, out-of-range chase, manual steering ownership and release, disconnected/riding gates, skill pause/stop, death, and same-PvP-target interval retention.
- Three independent negative controls substituting each original `5d79234` carrier fail respectively: “moving boundary must not restart swing”, “same target preserves recovery state”, “joystick owns path during chase”. `ATTACK_SOURCE_DIR` permits source-substitution reproduction.
- `bash Tests/Mobile/run-panel-input-contract.sh`: 24 checks using actual mobile HUD/input routing, including held attack producing no commands before its one release and simultaneous joystick ownership.
- `bash Tests/Mobile/run-move-cast-contract.sh`: 330 checks.
- `bash Tests/Mobile/run-world-player-gesture-contract.sh`: 17 checks.
- `bash Tests/Mobile/run-skill-gesture-contract.sh`: passed gesture/cancellation contract.
- `python3 Documentation/tools/compile-current-presentation.py`: all 1199 Presentation sources compile with real recovered Unity references, zero errors (existing warnings).
- `git diff --check`: passed.

These managed boundary tests establish state/request behavior, not visually inspected Unity playback or authoritative damage outcomes.
