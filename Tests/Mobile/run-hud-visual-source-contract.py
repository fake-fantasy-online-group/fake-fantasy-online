#!/usr/bin/env python3
"""Narrow provenance checks for the mobile HUD refinement; not runtime rendering."""
from hashlib import sha256
from pathlib import Path

root = Path(__file__).resolve().parents[2]
source = root / "Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"

def check_digest(text, expected, label):
    assert sha256(text.encode()).hexdigest() == expected, label

map_source = (source / "ReferenceFirstWorldMinimapHud.cs").read_text()
assert "bool touchControls = false" in map_source, "Desktop is the default map rendering path"
assert map_source.count("!touchControls && GUI.Button(") == 5, "All legacy map input owners are disabled only for mobile"
assert map_source.count("!touchControls && resources.Sketch") == 4, "All four duplicate legacy map faces are hidden only for mobile"
desktop_map = (map_source
    .replace("int? viewportHeight = null,\n        bool touchControls = false)", "int? viewportHeight = null)")
    .replace("!touchControls && ", "")
    .replace("if (!touchControls) ShowNearestGateTip", "ShowNearestGateTip"))
check_digest(desktop_map, "c20781c5de3b207e50e452c9fdf1af8f546cee5298349d179396701e90a6641f",
    "Desktop map body must remain identical to f2d3335 after removing mobile-only guards")
check_digest((source / "GameHud.GuiOwner.cs").read_text(),
    "743ee95aeaa1783109329934b06520264e754b68f831901f101f6947dfa6adc1",
    "Desktop HUD entry/draw body must remain unchanged")
check_digest((source / "GameHud.Resources.cs").read_text(),
    "814b13baff3b3b92c6e580d490e6f43169313e9aefc754785ef2d96207f05c50",
    "Desktop-owned resource loading must remain unchanged")
hud = (source / "GameHud.Mobile.cs").read_text()
check_digest(hud.split("    private void DrawMobileHud()")[0],
    "eea7dec25c715a2b29e74c873b7a893737b5ff00d7072131bf695da6dd7ba3bc",
    "Mobile movement, gesture ownership, skill dispatch, map actions and cancellation must remain unchanged")
assert "touchControls:true" in hud and "GUI.enabled=false" not in hud, "Mobile map is not greyed out to disable old inputs"
assert "ctrlTex[index]" not in hud, "Function icons cannot index the three-frame array as one frame per entry"
assert "GUI.Box(" not in hud and "GUI.Label(" not in hud, "Mobile HUD must not use default boxes or unscaled system text"
assert "ActivateControlButtonForIndependentRuntime(index)" in hud, "Menu still routes to original native windows"
assert "DrawCdOverlay(rect,hotSkill[slot])" in hud, "Original skill cooldown overlay remains"
assert "MenuItemRect(index)" in hud, "Drawing and touch exclusion use the same top menu layout"
assert "lianliRing" not in hud and "socketFill" not in hud, "Mobile joystick cannot reuse a partial progress arc or generated black disc"
assert "hb24Block" not in hud, "Combat faces use complete original mobile-appropriate artwork instead of enlarged empty hotbar squares"
print("PASS mobile HUD source contracts: desktop bodies and mobile dispatch unchanged; duplicate map faces/default skin removed. Not Unity rendering.")
