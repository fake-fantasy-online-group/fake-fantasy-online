#!/usr/bin/env bash
# Execute production rift HUD partials against deterministic Unity substitutes.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
cat > "$OUT/Runner.cs" <<'CS'
using System;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class RiftHudRunner
{
    static void Main()
    {
        var type = typeof(EndgameUiContract);
        foreach (string name in new[] { "RiftHudProgressContracts", "NativeRiftNoticeContracts", "QuietRiftLifecycleContracts", "QuietRiftRecoveryContracts", "GuardianArrivalSchemaContracts", "GuardianServerWireContracts" })
            type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        var client = type.GetNestedType("Client", BindingFlags.NonPublic).GetConstructor(Type.EmptyTypes).Invoke(null);
        var panel = new EndgamePanel(); panel.Bind((IEndgameClient)client);
        type.GetMethod("MultiFloorContracts", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new[] { client, panel });
        Console.WriteLine("PASS rift HUD/time/artwork/phase/dropdown/multi-floor assertions: " + type.GetField("assertions", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
    }
}
CS
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest -define:RIFT_HUD_TARGET_CONTRACT -main:RiftHudRunner -out:"$OUT/RiftHudContract.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/EndgamePanel*.cs \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/TooltipContent.Get.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/GameHud.BasicHit.cs" \
 "$ROOT/Tests/Endgame/EndgameUiStubs.cs" "$ROOT/Tests/Endgame/EndgameUiContract.cs" "$ROOT/Tests/Endgame/RiftHudProgressContract.cs" "$ROOT/Tests/Endgame/RiftQuietUiContract.cs" "$ROOT/Tests/Endgame/RiftWorldVisualContract.cs" "$OUT/Runner.cs"
FFO_RIFT_WIRE_FIXTURES="${FFO_RIFT_WIRE_FIXTURES:-$ROOT/Tests/Endgame/Fixtures/rift79-quiet-wire.json}" "$MONO/bin/mono" "$OUT/RiftHudContract.exe"
python3 "$ROOT/Tests/Endgame/rift_hud_source_contract.py"
