#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
SOURCE="$ROOT/Assets/Plugins/Android/ClientPackageInstaller.androidlib/src/main/java"
JAVA="${JAVA:-java}"
"$JAVA" -m jdk.compiler/com.sun.tools.javac.Main -encoding UTF-8 -source 8 -target 8 -d "$OUT/policy" \
 "$SOURCE/org/fakefantasyonline/updates/PackagePolicy.java" \
 "$ROOT/Tests/Mobile/AndroidPackageInstaller/PackagePolicyContract.java"
"$JAVA" -cp "$OUT/policy" org.fakefantasyonline.updates.PackagePolicyContract
python3 "$ROOT/Tests/Mobile/AndroidPackageInstaller/write_android_stubs.py" "$OUT/stubs"
find "$OUT/stubs" -name '*.java' > "$OUT/stubs.rsp"
"$JAVA" -m jdk.compiler/com.sun.tools.javac.Main -encoding UTF-8 -source 8 -target 8 -d "$OUT/lifecycle" \
 @"$OUT/stubs.rsp" "$SOURCE"/org/fakefantasyonline/updates/*.java \
 "$ROOT/Tests/Mobile/AndroidPackageInstaller/InstallerLifecycleContract.java"
"$JAVA" -cp "$OUT/lifecycle" org.fakefantasyonline.updates.InstallerLifecycleContract
python3 "$ROOT/Tests/Mobile/test_android_package_source.py"
if [[ -n "${ANDROID_JAR:-}" ]]; then
 [[ -f "$ANDROID_JAR" ]] || { echo 'ANDROID_JAR is not a readable official SDK platform jar.' >&2; exit 1; }
 "$JAVA" -m jdk.compiler/com.sun.tools.javac.Main -encoding UTF-8 -source 8 -target 8 \
  -classpath "$ANDROID_JAR" -d "$OUT/android" "$SOURCE"/org/fakefantasyonline/updates/*.java
 echo 'PASS Java plug-in compilation against supplied Android SDK API references. No Android runtime or APK build.'
else
 echo 'SKIP Android SDK compile: supply ANDROID_JAR to a verified SDK platform android.jar (compileSdk 33+).'
fi
