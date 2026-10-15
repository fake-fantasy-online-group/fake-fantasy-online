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
assert map_source.count("!touchControls && resources.Sketch") == 0, "Original map faces remain visible on mobile"
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
assert "DrawMobileButton(MobileHudArtwork.Inset(MobileClientPresentation.MapActionRect" not in hud, "No second row of mobile map faces"
art = (source / "MobileHudArtwork.cs").read_text()
assert "DrawTextureWithTexCoords" not in art, "Top mobile menu renders complete artwork without bottom-hotbar UV crop"
assert "MobileClientPresentation.AvatarRect" in hud, "Avatar rendering uses the same safe-area anchor as pointer exclusion"
assert "touchControls:true" in hud and "GUI.enabled=false" not in hud, "Mobile map is not greyed out to disable old inputs"
assert "ctrlTex[index]" not in hud, "Function icons cannot index the three-frame array as one frame per entry"
assert "GUI.Box(" not in hud, "Mobile HUD must not use default boxes"
wheel_text = hud.split("private void DrawMobileGroupText", 1)[1].split("private void DrawMobileRoundCooldown", 1)[0]
assert hud.count("GUI.Label(") == wheel_text.count("GUI.Label(") == 2, "Only scaled wheel text may use explicit GUI.Label"
assert "Mathf.RoundToInt(size*scale)" in wheel_text and "mobileGroupWheelStyle" in wheel_text, "Wheel uses scaled explicit text style"
wheel_draw = hud.split("private void DrawMobileGroupWheel", 1)[1].split("private void DrawMobileRoundCooldown", 1)[0]
for forbidden in ("DrawMobileRoundFace", "GUI.Box(", "GUI.DrawTexture", "MobileHudArtwork.Image", "Handles.", "DrawLine"):
    assert forbidden not in wheel_draw, "Wheel has no number frames, outer ring or sector borders: " + forbidden
mobile_input = (source / "GameHud.MobileInput.cs").read_text()
assert "MobilePageSwipe" not in mobile_input and "PageAfter" not in mobile_input, "Group selector never cycles pages with swipes"
assert "mobilePage=selected" in mobile_input, "Release commits the directly selected group"
assert "ActivateControlButtonForIndependentRuntime(index)" in hud, "Menu still routes to original native windows"
assert "DrawMobileRoundCooldown(iconRect,hotSkill[slot])" in hud, "Mobile cooldown is clipped to the icon circle"
assert "CooldownFrameIndexForIndependentRuntime(fraction)" in hud, "Native cooldown timing/frame selection remains"
assert "MenuItemRect(index)" in hud, "Drawing and touch exclusion use the same top menu layout"
assert "lianliRing" not in hud and "socketFill" not in hud, "Mobile joystick cannot reuse a partial progress arc or generated black disc"
assert "hb24Block" not in hud, "Combat faces use complete original mobile-appropriate artwork instead of enlarged empty hotbar squares"
print("PASS mobile HUD source contracts: desktop bodies unchanged; native map faces and full top-menu art retained. Not Unity rendering.")
