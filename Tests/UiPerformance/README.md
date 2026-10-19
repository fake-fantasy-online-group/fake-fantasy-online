# Repeated UI work contracts

Run using the existing offline managed-test toolchain:

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Tests/UiPerformance/run-chat-cache-contract.sh
bash Tests/UiPerformance/run-bitmap-emote-contract.sh
bash Tests/Mobile/run-chat-contract.sh
bash Tests/Mobile/run-chat-visual-contract.sh
python3 Tests/Mobile/run-chat-history-source-contract.py
python3 Tests/UiArtwork/run-selection-presentation-contract.py
```

These compile the real changed C# sources with deterministic API substitutes.
They do not start Unity, build a Player/APK, access a device, or measure phone
power use, frame time or temperature.

- Chat history and visible-row caches are bounded by the existing 800-message
  history and three owner streams. Tests cover append, clear, same-count replace,
  full-history eviction, channel and character filter changes, replacement view,
  exact width, scale/text size, row capacity, font metric identity and zero rows.
  Share lists remain live; row caches do not snapshot their mutable metadata.
- At 800 messages, 1,000 unchanged collection/partition/layout passes perform zero
  channel-filter calls, zero wrapping calls and zero measured managed allocations.
  Filter identity and font wrapping are deterministic substitutes in that test.
  This excludes the live character-ID string formatting in LoadFilter, the rest
  of OnGUI, Unity native work, share hover/drawing and history-window snapshots.
- Bitmap/emote tests compare recorded glyph and emoji draw calls against the old
  run-string algorithm: order, positions, UVs, colors, all eight outline samples,
  missing glyphs, spaces, invalid emote tokens, fractional scales and animated
  frames. They also cover exact wrap boundaries, late font readiness, font metric
  replacement and the 512-entry wrap-cache bound.
- 1,000 warmed mixed-emote draw-and-measure passes allocate zero managed bytes in
  this harness. The renderer still emits the original draw calls, including eight
  outline samples; this change reduces transient strings and layout work rather
  than reducing glyph quality or changing the font.
- Existing compact-chat contracts keep input, send, focus, channel picker,
  canceled/multi-touch expansion and original history-window routing exercised.
  No OnGUI event was globally skipped and no input-event path was removed.

The scope is repeated chat collection/layout and text-run allocation. Remaining
costs include the shared-item span/substring renderer, history-window snapshots,
GUI draw calls, and other windows' observation updates. Real-device profiling is
still needed to establish thermal or battery improvements.
