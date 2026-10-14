#!/usr/bin/env python3
"""Compile all current Presentation sources against recovered real Unity inputs.

Preserve the archived compiler switches/defines/generators, but never replay its
old source list. Requires current engine-free dependency assemblies first.
No Unity process, import, Player build, or runtime execution is involved.
"""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys


def main():
    source = Path(__file__).resolve().parents[2]
    tools_root = Path(os.environ.get("FFO_TOOLCHAINS_ROOT", "/workspace/shared/toolchains"))
    package = tools_root / "prior-backups/batch18-managed-toolchain"
    dependencies = Path(os.environ["FFO_COMPILE_DIR"])
    output = Path(os.environ.get("FFO_PRESENTATION_COMPILE_DIR", str(dependencies.parent / "dotnet-presentation-compile")))
    output.mkdir(parents=True, exist_ok=True)
    manifest = json.loads((package / "manifest.json").read_text())
    invocation = json.loads((package / "metadata/invocation.json").read_text())
    archived_files = {entry["packaged_path"]: entry for entry in manifest["files"]}
    original_to_packaged = manifest["original_to_packaged"]
    output_bindings = invocation["project_output_path_bindings"]
    verified = set()

    def vendor(relative):
        entry = archived_files[relative]
        path = package / relative
        if relative not in verified:
            data = path.read_bytes()
            if len(data) != entry["bytes"] or hashlib.sha256(data).hexdigest() != entry["sha256"]:
                raise RuntimeError(f"Recovered reference/configuration checksum mismatch: {relative}")
            verified.add(relative)
        return path

    settings = []
    for filename in ["FakeFantasyOnline.Presentation.rsp", "FakeFantasyOnline.Presentation.rsp2"]:
        for line in vendor("responses/" + filename).read_text().splitlines():
            if not line.strip() or line.strip('"').startswith("Assets/"):
                continue
            if line.startswith(("-out:", "-refout:")):
                switch = line.split(":", 1)[0]
                suffix = ".ref.dll" if switch == "-refout" else ".dll"
                settings.append(f'{switch}:"{output / ("FakeFantasyOnline.Presentation" + suffix)}"')
            elif line.startswith(("-r:", "-analyzer:")):
                switch, value = line.split(":", 1)
                value = value.strip('"')
                if value in original_to_packaged:
                    path = vendor(original_to_packaged[value])
                elif value in output_bindings:
                    name = output_bindings[value]["output_file_name"].replace(".ref.dll", ".dll")
                    path = dependencies / name
                    if not path.is_file():
                        raise RuntimeError(f"Compile the current engine-free dependencies first: {path}")
                else:
                    raise RuntimeError(f"No verified reference relocation for: {value}")
                settings.append(f'{switch}:"{path}"')
            elif line.startswith(("/additionalfile:", "-additionalfile:")):
                switch, value = line.split(":", 1)
                relative = invocation["additional_file_argument_bindings"][value.strip('"')]
                content = vendor(relative).read_text()
                content = content.replace(invocation["original_source_root"], str(source))
                target = output / Path(relative).name
                target.write_text(content)
                settings.append(f'{switch}:"{target}"')
            else:
                settings.append(line)

    current = sorted((source / "Assets/FakeFantasyOnline/Runtime/Presentation").rglob("*.cs"))
    if not current:
        raise RuntimeError("No current Presentation sources found")
    response = output / "current-presentation.rsp"
    response.write_text("\n".join(settings + ['"' + str(path) + '"' for path in current]) + "\n")
    audit = {
        "assembly": "FakeFantasyOnline.Presentation",
        "source_count": len(current),
        "source_files": [{"path": str(path.relative_to(source)), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()} for path in current],
        "verified_archived_inputs": sorted(verified),
        "profile": "Archived Unity 6000.4.9f1 compiler switches and defines, current full Presentation source list",
        "unity_process_or_player_build": False,
        "runtime_validation": False,
    }
    command = [os.environ["FFO_DOTNET"], "exec", os.environ["FFO_DOTNET_CSC"], "/nostdlib", "/noconfig", "@" + str(response)]
    audit["command"] = command
    with (output / "compile.log").open("w") as log:
        result = subprocess.run(command, cwd=source, stdout=log, stderr=subprocess.STDOUT)
    audit["exit_code"] = result.returncode
    (output / "source-and-profile-manifest.json").write_text(json.dumps(audit, indent=2) + "\n")
    print((output / "compile.log").read_text(), end="")
    if result.returncode == 0:
        print(f"PASS complete current Presentation compilation: {len(current)} source files, real recovered Unity references. No Unity execution.")
    return result.returncode


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (KeyError, OSError, RuntimeError) as error:
        print(f"Current Presentation compile: {error}", file=sys.stderr)
        sys.exit(2)
