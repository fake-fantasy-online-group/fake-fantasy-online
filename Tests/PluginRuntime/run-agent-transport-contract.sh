#!/usr/bin/env bash
# Standalone C# compilation + ephemeral loopback tests against a fake IGameApi.
# Uses managed compiler/runtime only; never launches Unity or builds a Player.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${UNITY_EDITOR_DIR:?Set UNITY_EDITOR_DIR to the Editor directory containing Data/MonoBleedingEdge}"
MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest \
  -out:"$OUT/AgentTransportContract.exe" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/PluginApi/GameApi.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/PluginApi/GameApiData.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/PluginHost/GameJson.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/PluginHost/MainThreadGameDispatcher.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/PluginHost/GameAgentProtocol.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/PluginHost/GameAgentServer.cs" \
  "$ROOT/Tests/PluginRuntime/AgentTransportContract.cs"
"$MONO/bin/mono" "$OUT/AgentTransportContract.exe"
python3 "$ROOT/Tests/PluginRuntime/test_agent_client.py" "$MONO/bin/mono" "$OUT/AgentTransportContract.exe"
