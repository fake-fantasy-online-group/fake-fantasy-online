using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    // Official 06000416, instance UnityEngine.Rect GameHud.MarqueeBand().
    //
    // arm64 rva 0x43a9f4 (456 bytes) and x86_64 rva 0x4524e0 (384 bytes). The Rect is an HFA of
    // four System.Single members, so arm64 returns it in v0..v3 (s0 = x, s1 = y, s2 = width,
    // s3 = height) and x86_64 returns it across XMM0/XMM1; the width/height lanes come from a
    // single packed constant on x86_64 ([rip + 0xccbe3f] = 428f, 16f, 0f, 0f) and from two
    // immediate multiplies on arm64 (s3 = s * 16.0 at 0x43ab50, s2 = s * 428.0 at 0x43ab5c).
    //
    // "uiScale <= 0" falls back to UIScale.Factor; both architectures read GameSettings.HotbarBottom
    // from the GameSettings static blob at +7 and GameHud.hotbarExpanded from the GameHud static
    // blob at +0x31 (49). The 94f/49f pair is selected by that bool and then multiplied by the
    // resolved scale, which is why the arm64 form is "fmul s0, s8, s0" (scale first) rather than a
    // raw constant move.
    //
    // Both architectures were read back independently: arm64 encodes the two constants as
    // "movz w9, #0x42bc, lsl #16" (94f) at 0x43ab24 and "movz w9, #0x4244, lsl #16" (49f) at
    // 0x43ab30, and x86_64 selects between DAT_0111b968 = 94f and DAT_0111af40 = 49f.
    public Rect MarqueeBand()
    {
        float s = uiScale;
        if (s <= 0f) s = UIScale.Factor;

        float y;
        if (GameSettings.HotbarBottom)
        {
            y = s + s;
        }
        else
        {
            float lift = 0f;
            if (showHotbar) lift = (hotbarExpanded ? 94f : 49f) * s;
            y = s + s + lift;
        }

        float x = Mathf.Max(175f, ((float)Screen.width / s - 179f + 171f) * 0.5f - 214f) * s;
        return new Rect(x, y, s * 428f, s * 16f);
    }
}

}
