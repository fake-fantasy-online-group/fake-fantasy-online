# Rack visible-icon contract

Run `python3 Tests/RackShop/run-visible-icons-contract.py` with Mono and a C# compiler
on PATH, or pass `--mono /path/to/mono --csc /path/to/mcs.exe`.

The runner compiles the production `RackShopDlg.Icons.cs` and the exact production
`SetGoods` method and `GoodsBuffer` class extracted from `ReferenceWorldReceiveOwner.RackShop.cs`, with
small deterministic Unity/network API stubs. It checks that receiving 996 rows
performs no icon reads, then exercises first/second pages, repeated draws,
cart/filter rows, shared pet icons, missing/corrupt icons, late resource readiness,
and refreshed catalog invalidation after icon-map changes or missing-icon recovery.
Source assertions cover all five normal/flag/cart render call sites.

The first page renders 12 distinct goods including the five featured rows. Later
pages may also render the original five featured rows; these are already cached.
Scrolling the cart can load up to nine additional visible goods. Icons are loaded
on demand and cached by the window; resource textures remain owned by ResLoader.
Missing icons retain the existing blank-icon presentation, with item text and
controls still usable. A decoder exception is logged once per icon/window instead
of aborting catalog receipt or window rendering.

These are compiled behavioral and source-wiring checks, not a Unity GUI run,
real-asset decode benchmark, or proof of the reported user's hang cause. Verify
cold/warm rack opening, paging/filtering, purchases and close input with real
assets in a client build before claiming the runtime incident is fixed.
