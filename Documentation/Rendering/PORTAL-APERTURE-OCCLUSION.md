# Retired asset-specific portal rendering

The asset-specific portal aperture profiles and building support refinements were
removed at the user's request. They are not a generic renderer or a claim of
Tencent-compatible behavior. The complete prior revision remains in the private
checkpoint; the public code-only archive is explanatory logic without its data.

Map objects now proceed through the existing ordinary paths: authored slices,
supported footprint columns, or static fallback. The special portal backplane
clamp is removed from both owner and no-owner authored placement. Existing fade,
texture ownership, animation, and ordinary sorting behavior remain unchanged.

This removal concerns rendering only. NPC dungeon menus, portal interactions,
teleporting, and the rift scene portal are not removed. No replacement rendering
algorithm, new asset profiles, release, or deployment is included.

Regression checks must verify ordinary dispatch and preserve existing UI/portal
interaction contracts; retired profile-specific expectations no longer apply.
