# Ordinary building rendering regression

The former per-asset refinements and portal aperture/backplane rendering special
cases were retired. Their resource-derived fixtures are not part of this test.

Run `bash Tests/BuildingLayers/run-building-layer-contract.sh` after activating
the documented managed toolchain. The test compiles the actual Build/PlaceObject,
authored slicing, footprint, and ground-band code with Unity substitutes. Inputs
are invented tiny images, generic category IDs, and synthetic mask columns.

It covers owner/no-owner paths, HD scale, PZ projection, authored and footprint
orders, static fallback, full source coverage, ordinary fade, and an order-change
negative control. This is a managed-host regression, not Unity visual acceptance
or a new claim that the retained generic policy matches Tencent semantics.

NPC/dungeon entrance interactions remain covered by their separate contracts.
