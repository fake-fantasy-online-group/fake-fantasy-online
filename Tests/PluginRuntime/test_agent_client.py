#!/usr/bin/env python3
"""Cross-language client contract against the temporary C# fake-game bridge."""
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[2]
CLIENT = ROOT / "examples/agents/game_agent.py"
spec = importlib.util.spec_from_file_location("game_agent", CLIENT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def main():
    host = subprocess.Popen([sys.argv[1], sys.argv[2], "--python-fixture"],
                            stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                            stderr=subprocess.PIPE, text=True)
    try:
        port = int(host.stdout.readline().strip())
        with module.GameAgent(port) as agent:
            description = agent.describe()
            assert description["protocol"] == "ffo-agent/1"
            assert description["observationDiscoveryAvailable"]
            assert agent.observe("player")["data"]["hp"] == 84
            first = agent.act("fake.step", {"x": 12}, request_id="python-step", scene_revision=7)
            repeated = agent.act("fake.step", {"x": 12}, request_id="python-step", scene_revision=7)
            assert first["actionId"] == repeated["actionId"]
            assert agent.wait(first["actionId"], 0)["state"] == "sent"
            assert not agent.cancel(first["actionId"])["cancelled"]
            running = agent.act("fake.wait", {}, request_id="python-wait")
            assert agent.cancel(running["actionId"])["cancelled"]
            assert agent.wait(running["actionId"], 0)["state"] == "cancelled"
            batch = agent.events(2)
            assert batch["resyncRequired"] and agent.cursor == 4
            assert agent.events(2)["nextSequence"] == 6
            try:
                agent.call("unknown")
            except module.AgentError as error:
                assert error.code == "unknown_method"
            else:
                raise AssertionError("Protocol error not surfaced")
        cli = subprocess.run([sys.executable, str(CLIENT), "--port", str(port), "--describe"],
                             check=True, capture_output=True, text=True, timeout=5)
        assert json.loads(cli.stdout)["protocol"] == "ffo-agent/1"
        cli = subprocess.run([sys.executable, str(CLIENT), "--port", str(port),
                              "--action", "fake.step", "--arguments", '{"x":24}',
                              "--request-id", "cli-step", "--scene-revision", "7"],
                             check=True, capture_output=True, text=True, timeout=5)
        assert '"state": "sent"' in cli.stdout and '"requestId": "cli-step"' in cli.stdout
        host.stdin.write("stop\n")
        host.stdin.flush()
        _, stderr = host.communicate(timeout=5)
        assert host.returncode == 0, stderr
        print("Python client integration passed: discovery, observations, retries, status, cancellation, event cursors, errors, CLI.")
    finally:
        if host.poll() is None:
            host.kill()
            host.communicate(timeout=5)


if __name__ == "__main__":
    main()
