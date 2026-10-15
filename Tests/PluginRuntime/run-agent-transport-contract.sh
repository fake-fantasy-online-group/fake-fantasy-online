#!/usr/bin/env bash
# Actual Lua agent addon, real interpreter/loopback, fake game. No Unity or live server.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Activate documented managed toolchain}"
: "${FFO_PLUGIN_FRAMEWORK_PACKAGE:?Set framework package checkout}"
: "${FFO_AGENT_ADDON:?Set path to Lua agent-bridge addon}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
find "$FFO_PLUGIN_FRAMEWORK_PACKAGE/Runtime/ThirdParty/MoonSharp/Interpreter" -name '*.cs' > "$OUT/sources.rsp"
printf '%s\n' "$ROOT"/Assets/FakeFantasyOnline/Runtime/PluginApi/*.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Foundation/*.cs \
 "$FFO_PLUGIN_FRAMEWORK_PACKAGE"/Runtime/Host/*.cs \
 "$ROOT/Tests/PluginRuntime/AgentTransportContract.cs" >> "$OUT/sources.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/AgentTransportContract.exe" @"$OUT/sources.rsp"
"$FFO_MONO_ROOT/bin/mono" "$OUT/AgentTransportContract.exe" "$FFO_AGENT_ADDON"
# Adapter preserves the Python fixture CLI while supplying the checked-out Lua addon.
cat > "$OUT/host" <<'HOST'
#!/usr/bin/env bash
assembly="$1"; shift
exec "$FFO_MONO_ROOT/bin/mono" "$assembly" "$FFO_AGENT_ADDON" "$@"
HOST
chmod +x "$OUT/host"
python3 "$ROOT/Tests/PluginRuntime/test_agent_client.py" "$OUT/host" "$OUT/AgentTransportContract.exe"
