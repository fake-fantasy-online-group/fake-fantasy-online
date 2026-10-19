#!/usr/bin/env bash
# Compile actual JNI and non-Android branches against the recovered Unity API references.
# Does not launch Unity, Java, an Android emulator, or a Player.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
: "${FFO_DOTNET:?Source Documentation/tools/activate-dotnet-test-toolchain.sh}"
: "${FFO_DOTNET_CSC:?Source Documentation/tools/activate-dotnet-test-toolchain.sh}"
: "${FFO_FRAMEWORK_DIR:?Source Documentation/tools/activate-dotnet-test-toolchain.sh}"
python3 "$ROOT/Tests/Mobile/AndroidPackageInstaller/validate_unity_dependencies.py" "$ROOT"
UNITY="${FFO_UNITY_MANAGED_DIR:-$(dirname "$(dirname "$FFO_FRAMEWORK_DIR")")/Managed/UnityEngine}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
for PLATFORM in UNITY_ANDROID UNITY_STANDALONE_WIN UNITY_EDITOR; do
 JNI=()
 [[ "$PLATFORM" != UNITY_ANDROID ]] || JNI=("-r:$UNITY/UnityEngine.AndroidJNIModule.dll")
 "$FFO_DOTNET" exec "$FFO_DOTNET_CSC" /noconfig /nostdlib /target:library "/define:$PLATFORM" "/out:$OUT/$PLATFORM.dll" \
  "-r:$FFO_FRAMEWORK_DIR/mscorlib.dll" "-r:$FFO_FRAMEWORK_DIR/System.dll" "-r:$FFO_FRAMEWORK_DIR/Facades/netstandard.dll" \
  "-r:$UNITY/UnityEngine.CoreModule.dll" "${JNI[@]}" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Bootstrap/AndroidClientPackageInstaller.cs"
 echo "PASS actual Unity-reference bridge compile: $PLATFORM (no runtime validation)."
done
