#!/usr/bin/env bash
# Compile core engine-free assemblies; optional framework is compiled only when explicitly selected.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT to MonoBleedingEdge-compatible runtime}"
: "${FFO_CSC:?Set FFO_CSC to official Roslyn csc.exe (C# 9+)}"
: "${FFO_FRAMEWORK_DIR:?Set FFO_FRAMEWORK_DIR to .NET Framework 4.7.2 reference directory}"
: "${FFO_COMPILE_DIR:?Set FFO_COMPILE_DIR to an output directory outside Assets}"
OUT="$FFO_COMPILE_DIR"; mkdir -p "$OUT"
export MONO_PATH="$FFO_FRAMEWORK_DIR/Facades${MONO_PATH:+:$MONO_PATH}"
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
compile() {
 local name="$1"; shift
 local source="$ROOT/Assets/FakeFantasyOnline/Runtime/$name"
 [[ "$name" != "PluginHost" ]] || source="$FFO_PLUGIN_FRAMEWORK_PACKAGE/Runtime/Host"
 find "$source" -name '*.cs' | sort > "$OUT/$name.sources"
 "$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -target:library -out:"$OUT/FakeFantasyOnline.$name.dll" "$@" @"$OUT/$name.sources" > "$OUT/$name.log" 2>&1 || { cat "$OUT/$name.log"; return 1; }
 echo "PASS $name ($(wc -l < "$OUT/$name.sources") actual source files)"
}
compile Foundation
compile Domain -r:"$OUT/FakeFantasyOnline.Foundation.dll"
compile Application -r:"$OUT/FakeFantasyOnline.Foundation.dll" -r:"$OUT/FakeFantasyOnline.Domain.dll"
compile Protocol -r:"$OUT/FakeFantasyOnline.Foundation.dll" -r:"$OUT/FakeFantasyOnline.Application.dll"
compile PluginApi -r:"$OUT/FakeFantasyOnline.Foundation.dll"
if [[ -n "${FFO_PLUGIN_FRAMEWORK_PACKAGE:-}" ]]; then
find "$FFO_PLUGIN_FRAMEWORK_PACKAGE/Runtime/ThirdParty/MoonSharp" -name '*.cs' | sort > "$OUT/MoonSharp.sources"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -define:MOONSHARP_NO_UNITY -target:library -out:"$OUT/MoonSharp.Interpreter.dll" @"$OUT/MoonSharp.sources" > "$OUT/MoonSharp.log" 2>&1 || { cat "$OUT/MoonSharp.log"; exit 1; }
echo "PASS MoonSharp ($(wc -l < "$OUT/MoonSharp.sources") actual source files)"
compile PluginHost -r:"$OUT/FakeFantasyOnline.Foundation.dll" -r:"$OUT/FakeFantasyOnline.Application.dll" -r:"$OUT/FakeFantasyOnline.PluginApi.dll" -r:"$OUT/MoonSharp.Interpreter.dll"

else
 echo "PASS core-only compile (framework and AI packages absent from compiler inputs)"
fi
