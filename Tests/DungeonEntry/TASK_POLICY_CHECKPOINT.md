# Task policy checkpoint history

The pause checkpoint was client dd386b2 / server 66045bb, based on integrated
client95b51f5/server29236d2. Work resumed after the owner explicitly requested
both inventory and task-entry fixes continue in parallel.

This is no longer an unvalidated three-map proposal. Final policy covers the
four exact solo task maps355/356/359/361, including a narrowly scoped existing
receipt projection for real saved unfinished355 records. Missing or completed
355 never gains access merely because its definition is disabled. No NPC,
acceptance flow, reward, once-only counter or protocol field was invented.

Current scope, tests and limitations are in README.md and the server's
`docs/task-only-dungeon-admission.md`; those supersede the old WIP notes.
