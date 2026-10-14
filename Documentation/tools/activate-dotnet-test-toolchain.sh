#!/usr/bin/env bash
# Source this file. Offline activation only; never downloads or invokes Unity.
# FFO_MONO_ROOT is retained solely to run unchanged legacy test entry points.
set -euo pipefail
FFO_TOOLCHAINS_ROOT="${FFO_TOOLCHAINS_ROOT:-/workspace/shared/toolchains}"
export FFO_DOTNET="$FFO_TOOLCHAINS_ROOT/dotnet-runtime/6.0.36/dotnet"
export FFO_DOTNET_RUNTIME_VERSION=6.0.36
export FFO_DOTNET_CSC="$FFO_TOOLCHAINS_ROOT/client/roslyn-4.8.0/tasks/netcore/bincore/csc.dll"
export FFO_FRAMEWORK_DIR="$FFO_TOOLCHAINS_ROOT/prior-backups/batch18-managed-toolchain/references/Unity-6000.4.9f1/Resources/Scripting/UnityReferenceAssemblies/unity-4.8-api"
export FFO_COMPILE_DIR="${FFO_DOTNET_COMPILE_DIR:-$FFO_TOOLCHAINS_ROOT/client/dotnet-managed-compile}"
export FFO_MANAGED_TRACE_DIR="${FFO_MANAGED_TRACE_DIR:-$FFO_TOOLCHAINS_ROOT/client/dotnet-traces}"
export FFO_MONO_ROOT="$FFO_TOOLCHAINS_ROOT/client/dotnet-command-adapter"
export FFO_DOTNET_MANAGED_HOST="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/dotnet-managed-host.py"
export DOTNET_ROOT="$(dirname "$FFO_DOTNET")"
[[ -x "$FFO_DOTNET" && -f "$FFO_DOTNET_CSC" && -f "$FFO_FRAMEWORK_DIR/mscorlib.dll" ]] || {
  echo 'Missing verified .NET runtime, Roslyn compiler, or recovered Unity API references.' >&2
  return 1 2>/dev/null || exit 1
}
mkdir -p "$FFO_MONO_ROOT/bin" "$FFO_COMPILE_DIR" "$FFO_MANAGED_TRACE_DIR"
cat > "$FFO_MONO_ROOT/bin/mono.$$" <<'HOST'
#!/usr/bin/env bash
# Explicit legacy command-shape adapter; this is NOT a Mono executable.
exec python3 "$FFO_DOTNET_MANAGED_HOST" "$@"
HOST
chmod +x "$FFO_MONO_ROOT/bin/mono.$$"
mv "$FFO_MONO_ROOT/bin/mono.$$" "$FFO_MONO_ROOT/bin/mono"
# Some original Python runners select their managed host using the .exe suffix.
# This transparent alias resolves to the same verified netcore compiler DLL.
export FFO_CSC="$FFO_MONO_ROOT/csc.exe"
if [[ -e "$FFO_CSC" || -L "$FFO_CSC" ]]; then
  [[ "$(readlink -f "$FFO_CSC")" == "$(readlink -f "$FFO_DOTNET_CSC")" ]] || {
    echo 'Refusing to overwrite a different compiler alias.' >&2
    return 1 2>/dev/null || exit 1
  }
else
  ln -s "$FFO_DOTNET_CSC" "$FFO_CSC"
fi
export FFO_MONO="$FFO_MONO_ROOT/bin/mono"
export PATH="$FFO_MONO_ROOT/bin:$PATH"
printf 'Managed-test profile: official .NET %s + Roslyn 4.8 + verified Unity 4.8 API references. Not Mono/Unity runtime validation.\n' "$FFO_DOTNET_RUNTIME_VERSION"
