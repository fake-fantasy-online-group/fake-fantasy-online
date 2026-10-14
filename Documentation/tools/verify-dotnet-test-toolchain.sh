#!/usr/bin/env bash
# Real compile/execute checks plus negative controls for the compatibility host.
set -euo pipefail
source "$(cd "$(dirname "$0")" && pwd)/activate-dotnet-test-toolchain.sh"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$FFO_DOTNET" --list-runtimes
"$FFO_DOTNET" exec "$FFO_DOTNET_CSC" -version
cat > "$OUT/Smoke.cs" <<'CS'
using System;
using System.Collections.Generic;
using System.Linq;
class Smoke {
    static int Main() {
        List<int> values = new() { 2, 3, 5 };
        if (values.Sum() != 10) throw new Exception("Incorrect calculation");
        Console.WriteLine("PASS actual C# 9 compile and execute");
        return 0;
    }
}
CS
"$FFO_MONO" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:9.0 -out:"$OUT/Smoke.exe" "$OUT/Smoke.cs"
"$FFO_MONO" "$OUT/Smoke.exe"
printf '%s\n' 'class Negative { static int Main() { return 7; } }' > "$OUT/Negative.cs"
"$FFO_MONO" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -out:"$OUT/Negative.exe" "$OUT/Negative.cs"
status=0
"$FFO_MONO" "$OUT/Negative.exe" || status=$?
[[ "$status" -eq 7 ]] || { echo "Runtime exit-code propagation failed: $status" >&2; exit 1; }
printf '%s\n' 'class Broken { deliberately invalid source }' > "$OUT/Broken.cs"
status=0
"$FFO_MONO" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -out:"$OUT/Broken.exe" "$OUT/Broken.cs" > "$OUT/compiler-negative.log" 2>&1 || status=$?
[[ "$status" -ne 0 && ! -e "$OUT/Broken.exe" ]] || { echo 'Compiler failure was not propagated.' >&2; exit 1; }
echo 'PASS compiler-error and runtime-exit-code negative controls; no test failure is hidden.'
