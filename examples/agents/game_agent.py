#!/usr/bin/env python3
"""External agent client for the opt-in FFO loopback bridge (Python 3, stdlib only).

Defaults to discovery and observations. Game actions require an explicit --action.
This module is also importable by a planner/LLM loop; no game-specific SDK required.
"""

import argparse
import json
import socket
import sys
import time
import uuid


class AgentError(RuntimeError):
    def __init__(self, code, message):
        super().__init__(f"{code}: {message}")
        self.code = code


class GameAgent:
    MAX_REQUEST_BYTES = 65536
    MAX_RESPONSE_BYTES = 1048576
    TERMINAL_STATES = {"completed", "rejected", "cancelled", "timedout", "unknown"}

    def __init__(self, port=17845, timeout=15.0):
        # Intentionally fixed to loopback. Never forward this unauthenticated port.
        self.socket = socket.create_connection(("127.0.0.1", port), timeout=timeout)
        self.reader = self.socket.makefile("rb")
        self.cursor = 0  # Each agent owns its cursor. Persist it outside this object if desired.

    def close(self):
        self.reader.close()
        self.socket.close()

    def __enter__(self):
        return self

    def __exit__(self, *_):
        self.close()

    def call(self, method, **params):
        envelope_id = uuid.uuid4().hex
        body = json.dumps({"id": envelope_id, "method": method, "params": params},
                          ensure_ascii=False, allow_nan=False, separators=(",", ":")).encode("utf-8")
        if len(body) > self.MAX_REQUEST_BYTES:
            raise ValueError("Request exceeds the bridge frame limit")
        self.socket.sendall(body + b"\n")
        # Bounded read: a malicious/broken endpoint cannot allocate an unlimited line.
        line = self.reader.readline(self.MAX_RESPONSE_BYTES + 2)
        if not line:
            raise ConnectionError("Bridge closed; the outcome of an in-flight act may be unknown")
        if len(line) > self.MAX_RESPONSE_BYTES + 1 or not line.endswith(b"\n"):
            self.close()
            raise AgentError("response_too_large", "Bridge response exceeded the frame limit")
        response = json.loads(line.decode("utf-8"))
        if not response.get("ok"):
            error = response.get("error", {})
            raise AgentError(error.get("code", "protocol_error"), error.get("message", "Unknown error"))
        if response.get("id") != envelope_id:
            raise AgentError("correlation_mismatch", "Response does not match this request")
        return response["result"]

    def describe(self):
        return self.call("describe")

    def observe(self, domain, **arguments):
        return self.call("observe", domain=domain, arguments=arguments)

    def act(self, name, arguments, *, request_id, scene_revision=None):
        """Save request_id BEFORE calling. A retry must use the same payload/revision/id."""
        params = {"name": name, "arguments": arguments, "requestId": request_id}
        if scene_revision is not None:
            params["expectedSceneRevision"] = scene_revision
        return self.call("act", **params)

    def status(self, action_id):
        return self.call("status", actionId=action_id)

    def cancel(self, action_id):
        return self.call("cancel", actionId=action_id)

    def events(self, max_count=128):
        batch = self.call("events", afterSequence=self.cursor, maxCount=max_count)
        self.cursor = batch["nextSequence"]
        # The caller MUST rebuild its observations if resyncRequired is true.
        return batch

    def wait(self, action_id, seconds=5.0):
        """Poll for a bounded time. Sent remains sent; no completion is invented."""
        deadline = time.monotonic() + seconds
        while True:
            receipt = self.status(action_id)
            if receipt["state"] in self.TERMINAL_STATES or time.monotonic() >= deadline:
                return receipt
            time.sleep(min(0.2, max(0, deadline - time.monotonic())))


def emit(value):
    print(json.dumps(value, indent=2, ensure_ascii=False), flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=17845)
    parser.add_argument("--domain", default="player", help="Observation domain when no action/status/cancel is selected")
    parser.add_argument("--arguments", default="{}", help="JSON object for observation or action arguments")
    parser.add_argument("--request-id", help="Saved retry identity; if omitted a fresh UUID is printed before an action")
    parser.add_argument("--scene-revision", type=int, help="Scene revision of the snapshot used to choose an action")
    parser.add_argument("--wait", type=float, default=0, help="Seconds to poll action status, without treating sent as completed")
    choices = parser.add_mutually_exclusive_group()
    choices.add_argument("--action", help="Explicit action name from describe.actions")
    choices.add_argument("--status", metavar="ACTION_ID")
    choices.add_argument("--cancel", metavar="ACTION_ID")
    choices.add_argument("--describe", action="store_true")
    args = parser.parse_args()
    arguments = json.loads(args.arguments)
    if not isinstance(arguments, dict):
        parser.error("--arguments must contain a JSON object")
    if args.wait < 0:
        parser.error("--wait must be nonnegative")
    with GameAgent(args.port) as agent:
        if args.status:
            emit(agent.status(args.status))
        elif args.cancel:
            emit(agent.cancel(args.cancel))
        elif args.action:
            discovery = agent.describe()
            definition = next((item for item in discovery["actions"] if item["name"] == args.action), None)
            if definition is None or not definition["available"]:
                raise AgentError("unavailable_action", "Action is not advertised as available: " + args.action)
            request_id = args.request_id or uuid.uuid4().hex
            # An agent should durably save this record, along with the decision that authorized it.
            emit({"sending": args.action, "requestId": request_id, "arguments": arguments,
                  "expectedSceneRevision": args.scene_revision})
            receipt = agent.act(args.action, arguments, request_id=request_id, scene_revision=args.scene_revision)
            emit(receipt)
            if args.wait and receipt.get("actionId"):
                emit(agent.wait(receipt["actionId"], args.wait))
        elif args.describe:
            emit(agent.describe())
        else:
            emit(agent.observe(args.domain, **arguments))
            emit(agent.events())


if __name__ == "__main__":
    try:
        main()
    except (AgentError, OSError, ValueError) as error:
        print(str(error), file=sys.stderr)
        sys.exit(1)
