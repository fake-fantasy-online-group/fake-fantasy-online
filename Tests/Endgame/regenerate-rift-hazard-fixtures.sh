#!/usr/bin/env bash
# Production Go riftView/encoder and unexported collision oracle, without modifying server sources.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SERVER="$(cd "${1:?Pass paired server repository}/server" && pwd)"
OUT="$ROOT/Tests/Endgame/Fixtures"
TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT
python3 - "$SERVER" "$ROOT" "$TMP/overlay.json" <<'PY'
import json,sys
from pathlib import Path
server,client,out=map(Path,sys.argv[1:])
virtual=server/'internal/game/scene/elite_client_wall_fixture_test.go'
if virtual.exists():raise SystemExit('Refusing to replace an existing server source')
json.dump({'Replace':{str(virtual):str(client/'Tests/Endgame/generate-rift-wall-fixture_test.go')}},out.open('w'))
PY
(cd "$SERVER" && RIFT_ELITE_WIRE_FIXTURE="$OUT/elite-wire-v6.json" go test ./internal/game/scene -run '^TestRiftEliteWireFixture$' -count=1)
(cd "$SERVER" && RIFT_WALL_GEOMETRY_FIXTURE="$OUT/elite-wall-geometry-v6.json" go test -overlay "$TMP/overlay.json" ./internal/game/scene -run '^TestClientRiftWallGeometryFixture$' -count=1)
