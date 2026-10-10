using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    // Official 0600044a — frozen arm64 0x4397cc (1980 B) / x86_64 0x450c40 (2288 B). Single
    // argument is the per-frame UI scale GameHud.OnGUI (06000414) publishes into instance slot
    // +0x4c; call site is the straight-line block LAB_00430354, shared with DrawBuffBar (06000449),
    // DrawPermBuffBox (0600044e) and DrawZoneBar (0600044b) and carrying no per-item gate.
    //
    // Gate: CharData +0x91 read as a byte and compared against 1 (arm64 0x4398d0/0x4398d4).
    //
    // Clock: one Time.get_realtimeSinceStartup read (arm64 0x4398f8) minus the +0xa8 stamp, floored
    // once (arm64 0x439950 frintm / 0x439954 fcvtms / fcmp +INF / csel int.MinValue =
    // Mathf.FloorToInt) and then reused by both countdowns. The clamp is applied to each countdown
    // and not to the elapsed value:
    //   0x43996c sub w8,w22,w8 ; 0x439970 bic w22,w8,w8,asr #31   -> max(0, nextSec   - elapsed)
    //   0x439b70 sub w20,w24,w25 ; 0x439ba0 bic w24,w20,w20,asr #31 -> max(0, remainSec - elapsed)
    // A negative incoming +0x9c keeps the -1 sentinel (arm64 0x439930 tbnz w22,#0x1f -> 0x439b2c
    // mov w22,#-1), which picks "怪物已全部出现" and, because the colour test is the unsigned
    // 0x439cb4 cmp w22,#0xa / 0x439cbc b.ls, also stays on the idle fill.
    //
    // Literals are metadata_usage kind5 ordinals (decode_state exact), concatenated in slot order:
    // 17268 "天神考验 {0}/{1}关   ", 17453 "怪物已全部出现" | 16957 "下一波 " + 16616
    // "{0:00}:{1:00}", 10476 "   剩余 ", 16616 again, 10477 "   场上 {0} 只" | 10335 "".
    internal void DrawRaidTimer(float scale)
    {
        if (!CharData.raidActive)
        {
            return;
        }
        Rect band = MarqueeBand();
        float now = Time.realtimeSinceStartup;
        int elapsed = Mathf.FloorToInt(now - CharData.raidRecvTime);
        int nextLeft = CharData.raidNextSec < 0
            ? -1
            : Mathf.Max(0, CharData.raidNextSec - elapsed);
        int remainLeft = Mathf.Max(0, CharData.raidRemainSec - elapsed);

        string[] parts = new string[5];
        parts[0] = string.Format("天神考验 {0}/{1}关   ", CharData.raidWave, CharData.raidTotal);
        parts[1] = nextLeft < 0 ? "怪物已全部出现" : "下一波 " + MinutesSeconds(nextLeft);
        parts[2] = "   剩余 ";
        parts[3] = MinutesSeconds(remainLeft);
        parts[4] = CharData.raidAlive > 0
            ? string.Format("   场上 {0} 只", CharData.raidAlive)
            : "";
        string text = string.Concat(parts);

        Color32 fill;
        if (nextLeft >= 0 && nextLeft <= 10)
        {
            // arm64 0x439ce4 frintm s0,s15 / 0x439ce8 fsub: fractional part is now - floor(now),
            // scaled by the immediates -1000f (0xc47a0000) and +500f (0x43fa0000) then / 500f.
            float frac = now - Mathf.Floor(now);
            float t = Mathf.Abs(frac * -1000f + 500f) / 500f;
            // Native inline words 0xff3c5aff / 0xff78dcff, not owner static state.
            fill = Color32.Lerp(new Color32(255, 90, 60, 255), new Color32(255, 220, 120, 255), t);
        }
        else
        {
            fill = new Color32(235, 225, 180, 255);
        }
        DrawMarqueeLine(band, scale, text, fill);
    }

    // Official 0600044b — frozen arm64 0x439f88 (2264 B) / x86_64 0x451530 (3760 B). Same argument
    // and same host block as DrawRaidTimer.
    //
    // Gate: ZoneOverlay.get_Showing (060010a2) and get_PhaseSecNow (060010a3) are the already
    // recovered product owners, so the stale-window, cross-map and -1 rules are the official ones.
    // Literals (metadata_usage kind5 ordinals): 17732 "毒圈 {0}/{1}   ", then 17368 "已是最后一圈"
    // for a negative phase clock or 16616 "{0:00}:{1:00}" wrapped by 17575 "收缩中 " on the
    // +0x24 == 1 arm and by 10650 " 后收缩" otherwise, then 10478 "   存活 {0}", then 10473
    // "   ★圈外失血 {0}/秒" only while +0x3c is set and 10335 "" otherwise.
    internal void DrawZoneBar(float scale)
    {
        if (!ZoneOverlay.Showing)
        {
            return;
        }
        Rect band = MarqueeBand();
        int phaseSec = ZoneOverlay.PhaseSecNow;
        bool outside = ZoneOverlay.outside;

        string head = string.Format("毒圈 {0}/{1}   ", ZoneOverlay.ring, ZoneOverlay.rings);
        string phaseText;
        if (phaseSec < 0)
        {
            phaseText = "已是最后一圈";
        }
        else if (ZoneOverlay.phase == 1)
        {
            phaseText = "收缩中 " + MinutesSeconds(phaseSec);
        }
        else
        {
            phaseText = MinutesSeconds(phaseSec) + " 后收缩";
        }
        string aliveText = string.Format("   存活 {0}", ZoneOverlay.alive);
        string bleedText = outside
            ? string.Format("   ★圈外失血 {0}/秒", ZoneOverlay.dmgPerSec)
            : "";
        string text = string.Concat(head, phaseText, aliveText, bleedText);

        float now = Time.realtimeSinceStartup;
        float millis = (now - Mathf.Floor(now)) * 1000f;
        // arm64 0x43a394 tbnz outside -> lerp, 0x43a3cc cmp w19,#0xa / 0x43a3d0 b.hi -> idle,
        // 0x43a3dc cmp w8,#1 / 0x43a3e0 b.ne -> idle. The countdown test is an unsigned compare, so
        // the -1 "last ring" sentinel lands on the idle fill.
        Color32 fill;
        if (outside || ((uint)phaseSec <= 10u && ZoneOverlay.phase == 1))
        {
            float t = Mathf.Abs(500f - millis) / 500f;
            // Native inline words 0xff4646ff / 0xff78dcff.
            fill = Color32.Lerp(new Color32(255, 70, 70, 255), new Color32(255, 220, 120, 255), t);
        }
        else
        {
            fill = new Color32(215, 180, 235, 255);
        }
        // Unrounded centre and baseline on this row, unlike DrawRaidTimer above. Not a rounding
        // difference: official BitmapFont.DrawOutline (060000b6, arm64 0x22b00c) rounds px and py
        // itself at 0x22b19c-0x22b26c (fcvt + modf + frintp/frintm + tst #1 + fcsel = banker's
        // rounding) and takes offset = max(1, scale) at 0x22b270, byte for byte what the product
        // BitmapFont.DrawOutline does, so handing over the unrounded pair is the native behaviour.
        float width = BitmapFont.Measure(text, scale);
        float px = band.x + Mathf.Max(0f, (band.width - width) * 0.5f);
        float py = scale + scale + band.y + band.height;
        BitmapFont.DrawOutline(px, py, text, fill, scale);

        if (!outside)
        {
            return;
        }

        // arm64 0x43a4c4..0x43a830: ZoneOverlay.get_Dot (060010a6), Screen.get_width/get_height,
        // a six step edge frame under a saved/restored GUI.color, then the centred warning 17034
        // "你在安全区外，正在失血！快进圈" at max(1, round(scale + scale)).
        Texture2D dot = ZoneOverlay.Dot;
        float screenW = Screen.width;
        float screenH = Screen.height;
        float pulse = Mathf.Abs(500f - millis) * 0.25f / 500f;
        Color saved = GUI.color;
        float step = scale * 46f / 6f;
        for (int i = 1; i <= 6; i++)
        {
            GUI.color = new Color(0.85f, 0.05f, 0.15f,
                (pulse + 0.35f) * ((i - 1) / -6f + 1f) * 0.5f);
            GUI.DrawTexture(new Rect(0f, step * (i - 1), screenW, step), dot);
            GUI.DrawTexture(new Rect(0f, screenH - step * i, screenW, step), dot);
            GUI.DrawTexture(new Rect(step * (i - 1), 0f, step, screenH), dot);
            GUI.DrawTexture(new Rect(screenW - step * i, 0f, step, screenH), dot);
        }
        GUI.color = saved;

        float warnScale = Mathf.Max(1f, Mathf.Round(scale + scale));
        string warning = "你在安全区外，正在失血！快进圈";
        float warnWidth = BitmapFont.Measure(warning, warnScale);
        // Native warning channels: 0x3f800000, 0x3eb4b4b5, 0x3eb4b4b5, 0x3f800000.
        BitmapFont.DrawOutline(
            Mathf.Round((screenW - warnWidth) * 0.5f),
            Mathf.Round(screenH * 0.3f),
            warning,
            new Color(1f, 0.3529412f, 0.3529412f, 1f),
            warnScale);
    }

    // Shared tail of both marquee rows: BitmapFont.Measure (060000b4), half the leftover width
    // clamped at zero, baseline scale + scale + band.y + band.height, then DrawOutline (060000b6).
    // Only DrawRaidTimer rounds the pair first (arm64 0x439d18-0x439f24).
    private static void DrawMarqueeLine(Rect band, float scale, string text, Color32 fill)
    {
        float width = BitmapFont.Measure(text, scale);
        float px = band.x + Mathf.Max(0f, (band.width - width) * 0.5f);
        float py = scale + scale + band.y + band.height;
        BitmapFont.DrawOutline(Mathf.Round(px), Mathf.Round(py), text, fill, scale);
    }

    // Official literal 16616 "{0:00}:{1:00}", the only clock format either row uses.
    private static string MinutesSeconds(int seconds)
    {
        return string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
    }
}
}
