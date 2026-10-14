#!/usr/bin/env python3
"""Run the existing managed test commands on an explicitly selected .NET host.

This is a command-shape adapter, not Mono. It preserves source/response files,
compiler arguments, test arguments, exit codes, and assertions. Compilation and
execution are recorded separately for reproducible validation.
"""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time
import uuid


def required(name):
    value = os.environ.get(name)
    if not value:
        raise RuntimeError(f"Missing {name}; source activate-dotnet-test-toolchain.sh")
    return value


def trace(kind, supplied, effective):
    root = Path(required("FFO_MANAGED_TRACE_DIR"))
    root.mkdir(parents=True, exist_ok=True)
    item = {
        "kind": kind,
        "host": "Microsoft.NETCore.App",
        "runtime_version": required("FFO_DOTNET_RUNTIME_VERSION"),
        "cwd": os.getcwd(),
        "original_arguments": supplied,
        "executed_arguments": effective,
        "time_ns": time.time_ns(),
    }
    path = root / f"{item['time_ns']}-{uuid.uuid4().hex}.json"
    path.write_text(json.dumps(item, indent=2) + "\n")
    return path, item


def run(kind, supplied, effective):
    path, item = trace(kind, supplied, effective)
    result = subprocess.run(effective)
    item["exit_code"] = result.returncode
    path.write_text(json.dumps(item, indent=2) + "\n")
    return result.returncode


def main():
    supplied = sys.argv[1:]
    if not supplied:
        raise RuntimeError("Expected a compiler or a test assembly")
    dotnet = required("FFO_DOTNET")
    csc = required("FFO_DOTNET_CSC")
    target, *arguments = supplied
    if target == "--version":
        print("Explicit .NET managed-test compatibility host (not Mono)", flush=True)
        return run("host-version", supplied, [dotnet, "--info"])

    # Legacy scripts use this mcs pathname; there is no mcs binary in this adapter.
    legacy_mcs = Path(required("FFO_MONO_ROOT")) / "lib/mono/4.5/mcs.exe"
    is_mcs = Path(target) == legacy_mcs
    is_csc = Path(target).resolve() == Path(csc).resolve()
    if is_mcs or is_csc:
        if arguments == ["--version"]:
            arguments = ["-version"]
        extra = []
        if is_mcs and arguments != ["-version"]:
            # mcs ordinarily supplies its implicit standard-library references.
            # Here the explicitly selected, verified Unity API profile supplies
            # them. Existing explicit Roslyn references remain untouched.
            refs = sorted(Path(required("FFO_FRAMEWORK_DIR")).glob("*.dll"))
            extra = ["-noconfig", "-nostdlib+"] + [f"-r:{p}" for p in refs]
        return run("compile", supplied, [dotnet, "exec", csc, *extra, *arguments])

    assembly = Path(target).resolve()
    if not assembly.is_file() or assembly.suffix.lower() not in (".exe", ".dll"):
        raise RuntimeError(f"Unsupported managed-test invocation: {target}")
    config = assembly.with_suffix(".runtimeconfig.json")
    expected = {"runtimeOptions": {
        "tfm": "net6.0", "framework": {
            "name": "Microsoft.NETCore.App",
            "version": required("FFO_DOTNET_RUNTIME_VERSION"),
        },
    }}
    if config.exists() and json.loads(config.read_text()) != expected:
        raise RuntimeError(f"Refusing to overwrite a different runtime configuration: {config}")
    config.write_text(json.dumps(expected, indent=2) + "\n")

    # Mono's MONO_PATH can point at previously compiled project assemblies.
    # .NET resolves these beside the executable when no deps.json is present.
    # Copy only actual project outputs, never framework reference assemblies.
    outputs = Path(required("FFO_COMPILE_DIR"))
    if outputs.is_dir() and outputs.resolve() != assembly.parent:
        for dependency in outputs.glob("*.dll"):
            destination = assembly.parent / dependency.name
            if not destination.exists():
                shutil.copy2(dependency, destination)
    return run("execute", supplied, [dotnet, "exec", str(assembly), *arguments])


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, RuntimeError, ValueError) as error:
        print(f"Managed .NET compatibility runner: {error}", file=sys.stderr)
        sys.exit(2)
