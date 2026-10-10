using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of the normal-map GameHud.DrawBuffBar and DrawPermBuffBox branches.
/// </summary>
public sealed class ReferenceFirstWorldBuffHud : IDisposable
{
    public const int GhostWeakenSkillId = 12005;
    public const int WeakenIconId = 7069;
    public const string StatusIconPathFormat = "item/item/item{0}.gso";

    // Official GameHud.DrawCounterChargeIcon (0x0600044d) literals, all taken from the frozen
    // metadata of the same body - metadata_usage 1441 is CharData (its static slot +0xd0 is
    // counterSkillId), 2686 is Win05, 17170 and 17171 are the two string literals.
    public const int CounterFallbackSkillId = 10203;
    public const string CounterFallbackName = "反击";
    public const string CounterTooltipFormat =
        "反击势 ×{0}\n闪避攒下的反击, 下一次【主动出手命中】时一次性打出(每层追加一次反击伤害)。\n剩余 {1}秒(这段时间内没有出手就会作废)";

    private readonly ReferenceResourceLoader resourceLoader;
    private WorldBuffState state = WorldBuffState.Empty;
    private ClientSessionCoordinator observationSession;
    private ReferenceFirstWorldWindowOwner observationOwner;
    private bool disposed;

    private ReferenceFirstWorldBuffHud(ReferenceResourceLoader resourceLoader)
    {
        this.resourceLoader = resourceLoader;
        Ready = true;
    }

    public bool Ready { get; private set; }

    public WorldBuffState State
    {
        get
        {
            if (observationSession != null)
                state = observationSession.BuffState ?? WorldBuffState.Empty;
            return state;
        }
    }

    internal bool BindObservationForIndependentRuntime(ClientSessionCoordinator source, float elapsedSeconds,
        ReferenceFirstWorldWindowOwner owner)
    {
        if (!Ready || disposed || source == null || !IsFiniteNonnegative(elapsedSeconds)) return false;
        observationSession = source;
        observationOwner = owner;
        return true;
    }

    internal void UnbindObservationForIndependentRuntime(ReferenceFirstWorldWindowOwner owner)
    {
        if (!ReferenceEquals(observationOwner, owner)) return;
        observationSession = null;
        observationOwner = null;
    }

    public string HoverTooltip { get; private set; } = string.Empty;

    public int LastFiniteRowsDrawn { get; private set; }

    public int LastPermanentRowsDrawn { get; private set; }

    public static bool TryCreate(
        ReferenceResourceLoader resourceLoader,
        out ReferenceFirstWorldBuffHud owner)
    {
        owner = null;
        if (resourceLoader == null)
        {
            return false;
        }

        ReferenceWorldBitmapFont font;
        if (!ReferenceWorldBitmapFont.TryCreate(resourceLoader, out font) ||
            font == null)
        {
            return false;
        }

        // The official 0x06000449 / 0x0600044e / 0x0600044d bodies ask GameHud.ExceptBox
        // (0x06000448) for the frame at draw time; they carry no construction-time box resource
        // gate, so a missing or undecodable box leaves the HUD standing with null boxes.
        owner = new ReferenceFirstWorldBuffHud(resourceLoader);
        return true;
    }

    /// <summary>
    /// The published Buff snapshot seam. It still carries the whole projection the 0x8017 read
    /// walked, but it is not the row source of either column now: both read the <c>CharData</c>
    /// statics list, whose rows carry the per-row stamp, so <c>elapsedSeconds</c> stays only
    /// the frame-time gate it always was.
    /// </summary>
    public bool ApplyState(WorldBuffState next, float elapsedSeconds)
    {
        if (!Ready || disposed || next == null || !IsFiniteNonnegative(elapsedSeconds))
        {
            return false;
        }

        state = next;
        observationSession = null;
        observationOwner = null;
        return true;
    }

    public bool Draw(float elapsedSeconds)
        => DrawForIndependentRuntime(elapsedSeconds, UIScale.Factor);

    internal bool DrawForIndependentRuntime(float elapsedSeconds, float scale,
        int? viewportWidth = null, GameHud hudOwner = null, bool nativeInvocation = false)
    {
        HoverTooltip = string.Empty;
        LastFiniteRowsDrawn = 0;
        LastPermanentRowsDrawn = 0;
        // Original OnGUI reaches finite/permanent bodies without these compatibility gates.
        // Each original body keeps its own list/charge/clock/mouse admission below.
        if (!nativeInvocation && (!Ready || disposed || (viewportWidth ?? Screen.width) < 1 ||
            Screen.height < 1 || !IsFiniteNonnegative(elapsedSeconds)))
        {
            return false;
        }

        // These are stages of one native OnGUI call: preserve incoming GUI state and
        // each reached leaf's own normal/exceptional restoration behavior.
        // The official 0x06000449 body samples its own clock; elapsedSeconds applies
        // only to the explicit compatibility draw admission above.
        // 06000414 invokes these distinct bodies in this order. Each owns its
        // live row read and early return; a finite-column return cannot skip 44e.
        DrawBuffBar(hudOwner != null ? hudOwner.s : scale, viewportWidth ?? Screen.width);
        DrawPermBuffBox(hudOwner != null ? hudOwner.s : scale, viewportWidth ?? Screen.width);

        return true;
    }

    public static int FiniteRowsPerColumn(float scale, float screenHeight)
    {
        float size = 30f * scale;
        float gap = 4f * scale;
        float available = (screenHeight - size) + -6f * scale - 180f * scale;
        return Math.Max(1, unchecked((int)(available / (size + gap)) + 1));
    }

    public static Rect FiniteStatusRect(
        int visibleIndex,
        float scale,
        float screenWidth,
        float screenHeight)
    {
        if (visibleIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(visibleIndex));
        }

        float size = 30f * scale;
        float step = size + 4f * scale;
        float firstX = screenWidth + -87f * scale + size * -0.5f;
        int rows = FiniteRowsPerColumn(scale, screenHeight);
        int column = visibleIndex / rows;
        int row = visibleIndex - column * rows;
        return new Rect(
            firstX - step * column,
            180f * scale + step * row,
            size,
            size);
    }

    public static float PermanentStartY(int count, float scale, float screenHeight)
    {
        if (count < 1)
        {
            return 200f * scale;
        }

        float size = 32f * scale;
        float gap = 5f * scale;
        float padding = 4f * scale;
        float height = (padding + padding) + (size * count + gap * (count - 1));
        float centered = screenHeight * 0.5f - height * 0.5f;
        float minimum = 200f * scale;
        return minimum > centered ? minimum : centered;
    }

    public static Rect PermanentStatusRect(
        int visibleIndex,
        int count,
        float scale,
        float screenWidth,
        float screenHeight)
    {
        if (visibleIndex < 0 || visibleIndex >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(visibleIndex));
        }

        float size = 32f * scale;
        float gap = 5f * scale;
        float padding = 4f * scale;
        float x = (screenWidth - (size + (padding + padding))) + -6f * scale;
        x = padding + x;
        float y = padding + PermanentStartY(count, scale, screenHeight);
        return new Rect(
            x,
            y + (size + gap) * visibleIndex,
            size,
            size);
    }

    /// <summary>
    /// Official 0x06000449 countdown: <c>remainSec - FloorToInt(now - recvTime)</c>, one
    /// <c>Time.realtimeSinceStartup</c> read per body. arm64 0x438cf4 / 0x438d24 / 0x438d28
    /// ("fcvtms"; metadata 02013 = System.Math) and x86_64 0x4500ae / 0x4500fd ("roundss",9).
    /// No clamp: a future row stamp makes the difference negative and the subtraction then adds.
    /// </summary>
    public static int RemainingSeconds(
        int remainSec,
        float recvTime,
        float realtimeSeconds)
    {
        return unchecked(remainSec - Mathf.FloorToInt(realtimeSeconds - recvTime));
    }

    public static float FinalSecondsAlpha(int remainingSeconds, float realtimeSeconds)
    {
        if (remainingSeconds >= 11)
        {
            return 1f;
        }

        float fraction = realtimeSeconds - Mathf.Floor(realtimeSeconds);
        return Mathf.Abs(fraction * -1000f + 500f) / 500f;
    }

    /// <summary>
    /// Official hover text of both columns: the row display name (+0x20), then "\n" plus the row
    /// description (+0x28) when it is not empty (metadata 10376), then "\n(永久)" (10393) for a
    /// negative <paramref name="wireRemainingSeconds"/> or "\n剩余 " (10414) plus "{0}秒"
    /// (16706) / "{0}分{1}秒" (16705) otherwise. 0x0600044e only ever uses the first two and
    /// 10393, because it draws no row with a non-negative remainSec.
    /// </summary>
    public static string Tooltip(
        string displayName,
        string description,
        int wireRemainingSeconds,
        int remainingSeconds)
    {
        string value = displayName ?? string.Empty;
        if (!string.IsNullOrEmpty(description))
        {
            value += "\n" + description;
        }

        if (wireRemainingSeconds < 0)
        {
            return value + "\n(永久)";
        }

        string duration = remainingSeconds < 60
            ? string.Format(CultureInfo.CurrentCulture, "{0}秒", remainingSeconds)
            : string.Format(
                CultureInfo.CurrentCulture,
                "{0}分{1}秒",
                remainingSeconds / 60,
                remainingSeconds % 60);
        return value + "\n剩余 " + duration;
    }

    internal Texture2D LoadStatusIcon(int iconId)
    {
        return GameHud.BuffIcon(iconId);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        state = WorldBuffState.Empty;
        observationSession = null;
        observationOwner = null;
        HoverTooltip = string.Empty;
        LastFiniteRowsDrawn = 0;
        LastPermanentRowsDrawn = 0;
        Ready = false;
        disposed = true;
    }

    // 06000449 and 0600044e keep separate list/clock/event lifetimes. Their existing
    // geometry, row formatting, icon and counter helpers are shared unchanged.
    private void DrawBuffBar(float scale, float screenWidth)
    {
        // 0x06000449 gate: only a NULL or EMPTY list enters it (arm64 0x438a08 / 0x438a10, x86_64
        // 0x44fd71 / 0x44fd78). Below one charge the body leaves at arm64 0x4391c8 / x86_64
        // 0x4505e8 - before the clock, the mouse and the icon. A NULL list then becomes
        // GameHud.EmptyBuffs; an EMPTY non-null list keeps its own list (arm64 0x438a44-0x438a68,
        // x86_64 0x44fda7-0x44fdce), so null and empty stay two different states.
        List<CharData.BuffView> finiteRows = CharData.buffs;
        if (finiteRows == null || finiteRows.Count == 0)
        {
            if (CharData.CounterChargesNow() < 1)
            {
                // Product observation only. The caller still invokes the permanent owner.
                LastFiniteRowsDrawn = 0;
                return;
            }

            if (finiteRows == null)
            {
                finiteRows = GameHud.EmptyBuffs;
            }
        }

        // The clock is read after that gate (arm64 0x438a70 / x86_64 0x44fdd7). Non-null on every
        // path reaching here, so the count is read directly with no second null test.
        float now = Time.realtimeSinceStartup;

        // 0x06000449 icons: IC = 30 * s and y0 = 180 * s (arm64 0x438a78-0x438a8c), then
        // colX = SW - 87 * s - IC * 0.5 (0x438b20-0x438b40), then Event.current and its
        // mousePosition (arm64 0x438b1c / 0x438b48, x86_64 0x44fe9a / 0x44fed1), then GUI.color
        // and the 0x0600044d call whose return value becomes the first visible finite index
        // (0x438ba8). An empty list still reaches this call when charges > 0, so the icon keeps
        // its event supply and the mouse read stays before it on every path.
        float counterSize = 30f * scale;
        float counterY = 180f * scale;
        // Native 449 reads height here, after its list/charge gate and own clock.
        float screenHeight = Screen.height;
        float counterX = screenWidth + scale * -87f + counterSize * -0.5f;
        Vector2 finiteMouse = ReadMousePosition();
        int finiteIndex = DrawCounterChargeIcon(
            scale,
            counterX,
            counterY,
            counterSize,
            finiteMouse,
            GUI.color);

        // The row loop is skipped, not the icon: arm64 0x438b9c-0x438ba4 / x86_64 the matching
        // Count < 1 test sits after the counter-icon call and before the loop.
        for (int index = 0; index < finiteRows.Count; index++)
        {
            CharData.BuffView row = finiteRows[index];
            if (row.remainSec < 0)
            {
                continue;
            }

            // An expired finite buff is skipped, never deleted: arm64 0x438d48 / 0x438d4c and
            // x86_64 0x45010a / 0x45010d are the only gate, so it keeps no cell, reaches no name
            // fallback and gets no tooltip; the next 0x8017 replacement is what removes the row.
            int remaining = RemainingSeconds(row.remainSec, row.recvTime, now);
            if (remaining <= 0)
            {
                continue;
            }

            Rect box = FiniteStatusRect(
                finiteIndex,
                scale,
                screenWidth,
                screenHeight);
            // Blink: ABS((now - Floor(now)) * -1000 + 500) / 500 below 11 seconds, else 1.0. The
            // official body computes it once outside the loop (arm64 0x438bb4); FinalSecondsAlpha
            // works the same value out per row from the one now, which cannot differ.
            float alpha = FinalSecondsAlpha(remaining, now);
            DrawStatus(row, box, scale, alpha, LoadFiniteStatusIcon(row));
            // 0x06000449 hover: the outer 30 * scale cell, half-open on both high edges, from the
            // same one mouse read. No EventType test in the extent, so no click action is added.
            if (box.x <= finiteMouse.x && finiteMouse.x < box.xMax &&
                box.y <= finiteMouse.y && finiteMouse.y < box.yMax)
            {
                HoverTooltip = Tooltip(row.name, row.desc, row.remainSec, remaining);
                UITip.Show(HoverTooltip, scale);
            }

            finiteIndex++;
        }

        LastFiniteRowsDrawn = finiteIndex;
    }

    private void DrawPermBuffBox(float scale, float screenWidth)
    {
        List<CharData.BuffView> rows = CharData.buffs;
        if (rows == null || rows.Count == 0) return;
        int permanentCount = 0;
        for (int index = 0; index < rows.Count; index++)
        {
            if (rows[index].remainSec < 0)
            {
                permanentCount++;
            }
        }

        // 0x0600044e returns through its own epilogue when the pre-scan found no permanent row -
        // arm64 0x439340 "cbz w21,#0x439798" / x86_64 0x45074a "test r12d,r12d" + 0x45074d
        // "je 0x450c20" - and that is BEFORE its Event.current read (arm64 0x4393fc, x86_64
        // 0x4507a9), so the mouse is read inside this arm only.
        int permanentIndex = 0;
        if (permanentCount > 0)
        {
            // Native 44e reads height only after the completed pre-scan found a row.
            float screenHeight = Screen.height;
            Vector2 permanentMouse = ReadMousePosition();
            for (int index = 0; index < rows.Count; index++)
            {
                CharData.BuffView row = rows[index];
                if (row.remainSec >= 0)
                {
                    continue;
                }

                Rect box = PermanentStatusRect(
                    permanentIndex,
                    permanentCount,
                    scale,
                    screenWidth,
                    screenHeight);
                DrawStatus(row, box, scale, 1f);
                // 0x0600044e hover: the outer 32 * scale box, half-open on both high edges, from
                // that one mouse read (arm64 0x439408 / x86_64 0x4507ba). No EventType test
                // anywhere in the extent, so a permanent row has no click action.
                if (box.x <= permanentMouse.x && permanentMouse.x < box.xMax &&
                    box.y <= permanentMouse.y && permanentMouse.y < box.yMax)
                {
                    HoverTooltip = Tooltip(row.name, row.desc, row.remainSec, int.MaxValue);
                    UITip.Show(HoverTooltip, scale);
                }

                permanentIndex++;
            }
        }

        LastPermanentRowsDrawn = permanentIndex;
    }

    /// <summary>
    /// One <c>Event.current.mousePosition</c> read, at the point where the official body takes
    /// it. Both 0x06000449 (arm64 0x438b1c / 0x438b48, x86_64 0x44fe9a / 0x44fed1) and
    /// 0x0600044e (arm64 0x439400 / 0x439408, x86_64 0x4507ac / 0x4507ba) test
    /// <c>Event.current</c> for null and jump to a non-returning raise helper (arm64 0x4391f8 /
    /// 0x4397c8 "bl #0x177a74", x86_64 0x4505fa / 0x450c32 "call 0x16c1b0"), so this reads the
    /// property directly instead of substituting a coordinate that fails every comparison.
    /// </summary>
    private static Vector2 ReadMousePosition()
    {
        return Event.current.mousePosition;
    }

    /// <summary>
    /// Source translation of the official <c>GameHud.DrawCounterChargeIcon</c> (0x0600044d,
    /// arm64 rva 0x441bac / x86_64 rva 0x459a70), the counter-charge icon of the finite buff
    /// column. Returns 0 when nothing was drawn and 1 when the icon was drawn, which is exactly
    /// the value the official DrawBuffBar uses as the first visible finite index.
    /// </summary>
    private int DrawCounterChargeIcon(
        float s,
        float colX,
        float y0,
        float ic,
        Vector2 mp,
        Color prev)
    {
        int charges = CharData.CounterChargesNow();
        if (charges < 1)
        {
            return 0;
        }

        int remainSec = CharData.CounterRemainSec();
        float realtime = Time.realtimeSinceStartup;
        // Only a window with fewer than four seconds left blinks - arm64 0x441c6c "b.le 0x441eb4"
        // and 0x441eb0 "b.gt 0x441c70" are the two duplicated forms of the same "remain < 4" test
        // the compiler copied into both cctor paths; the x86_64 body holds the same pair and the
        // decompiled text of both architectures reads "if (iVar2 < 4)". The blink is
        // |1 - 2 * frac(realtime)| taken from this one realtime read, never from a second sample.
        float alpha = remainSec < 4
            ? Mathf.Abs((realtime - (int)realtime) * -1000f + 500f) / 500f
            : 1f;

        int iconId = Win05.SkillIconId(CharData.counterSkillId);
        if (iconId < 1)
        {
            iconId = Win05.SkillIconId(CounterFallbackSkillId);
        }
        Texture2D icon = GameHud.BuffIcon(iconId);

        GUI.color = new Color(1f, 1f, 1f, alpha);
        // Official GameHud.DrawCounterChargeIcon (0x0600044d) asks GameHud.ExceptBox(true)
        // (0x06000448) for the frame at draw time, so the good box is the official one-shot
        // resource and not a texture this owner loaded.
        Texture2D box = GameHud.ExceptBox(true);
        float x = colX;
        float y = y0;
        float size = ic;
        if (box)
        {
            GUI.DrawTexture(new Rect(x, y, size, size), box);
            float inset = s * 3f;
            if (0f < inset)
            {
                x += inset;
                y += inset;
                size -= inset + inset;
            }
        }

        if (icon)
        {
            GUI.DrawTexture(new Rect(x, y, size, size), icon);
        }
        else
        {
            DrawOfficialNameAsIcon(new Rect(x, y, size, size), CounterFallbackName, s, alpha);
        }

        GUI.color = prev;

        string count = charges.ToString();
        float fontScale = 1f;
        if (1f <= s)
        {
            fontScale = s;
        }
        float measured = BitmapFont.Measure(count, fontScale);
        float textX = Mathf.Round(colX + ic - measured - (s + s));
        float textY = Mathf.Round(y0 + ic - fontScale * 13f);
        float textAlpha = alpha <= 1f ? alpha : 1f;
        // Official quantises the alpha to 1/255 steps before handing it to BitmapFont; for an
        // alpha inside [0,1] that truncation is the same as a floor.
        BitmapFont.DrawOutline(
            textX,
            textY,
            count,
            new Color(1f, 236f / 255f, 150f / 255f, Mathf.Floor(textAlpha * 255f) / 255f),
            fontScale);

        // The official hover test is the outer box and is inclusive on the low edge, exclusive on
        // the high edge - arm64 0x4421ac/0x4421f8/0x442208/0x442220 (body extent 0x441bac..
        // 0x442284) and x86_64 0x45a0e2/0x45a10e/0x45a11b/0x45a144 (body extent 0x459a70..
        // 0x45a1c0). Both bodies end the hovered arm with UITip.Show only (arm64 0x44227c,
        // x86_64 0x45a19a): there is no EventType test anywhere in the extent, so the icon has no
        // click action and none is added here.
        if (colX <= mp.x)
        {
            if (colX + ic <= mp.x)
            {
                return 1;
            }
            if (mp.y < y0)
            {
                return 1;
            }
            if (mp.y < y0 + ic)
            {
                UITip.Show(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        CounterTooltipFormat,
                        charges,
                        remainSec),
                    s);
            }
        }

        return 1;
    }

    /// <summary>
    /// Source translation of the official <c>GameHud.DrawNameAsIcon</c> (0x0600044c, arm64 rva
    /// 0x442284 size 1476 - body extent 0x442284..0x442848; x86_64 rva 0x45a1c0 size 1568 - body
    /// extent 0x45a1c0..0x45a7e0). It is the single missing-texture fallback of the finite buff
    /// column, the permanent buff column and the counter-charge icon.
    /// </summary>
    /// <remarks>
    /// One official body, three native callers, one recovery subject. The frozen 1.5.8 atlas
    /// (<c>index.sqlite3</c> table <c>calls</c>) lists exactly three native callers of 0x0600044c
    /// and no other caller in the whole assembly: <c>GameHud.DrawBuffBar</c> (0x06000449) at arm64
    /// 0x439030, <c>GameHud.DrawPermBuffBox</c> (0x0600044e) at arm64 0x439690 and
    /// <c>GameHud.DrawCounterChargeIcon</c> (0x0600044d) at arm64 0x441fa4. All three pass
    /// (rect, s, alpha, name): 449 and 44e pass the row box inset by 3 * s (a 30 * s - 6 * s square)
    /// with the row display name read from +0x20, 449 passing that row's blink alpha and 44e the
    /// literal 1.0; 44d passes the counter box inset by 3 * s with the literal name "反击"
    /// (metadata_usage 17170) and its own blink alpha. WBB-087's note that the buff-column fallback
    /// is "a different body" is therefore wrong, and the unofficial helper it left in place (no
    /// plate, no edges, no scale shrink, white, drawn through
    /// <c>ReferenceWorldBitmapFont.CenterOutline</c>) has been deleted so that both callers share
    /// this one official body.
    /// Body of the official member, both architectures: <c>BitmapFont.Ready</c> gate, the " Lv" /
    /// '(' / '（' cuts plus <c>Trim</c>, a font scale that starts at <c>(int)s</c> and shrinks by 1
    /// while the measure does not fit <c>ir.width - 2</c> or <c>fontScale * 12</c> does not fit
    /// <c>ir.height - 2</c>, a <c>ZoneOverlay.Dot</c> plate and four <c>ZoneOverlay.Dot</c> edges
    /// under two literal colours, and a centred <c>BitmapFont.DrawOutline</c> in
    /// <c>(1, 232/255, 160/255)</c>.
    /// </remarks>
    private static void DrawOfficialNameAsIcon(Rect ir, string rawName, float s, float alpha)
    {
        if (!BitmapFont.Ready || string.IsNullOrEmpty(rawName))
        {
            return;
        }

        string value = rawName;
        int cut = value.IndexOf(" Lv", StringComparison.Ordinal);
        if (cut > 0)
        {
            value = value.Substring(0, cut);
        }
        cut = value.IndexOf('(');
        if (cut > 0)
        {
            value = value.Substring(0, cut);
        }
        cut = value.IndexOf('（');
        if (cut > 0)
        {
            value = value.Substring(0, cut);
        }
        value = value.Trim();
        if (value.Length == 0)
        {
            return;
        }

        string first = value.Substring(0, 1);
        float fontScale = (int)s;
        if (fontScale < 1f)
        {
            fontScale = 1f;
        }
        if (1f < fontScale)
        {
            while (true)
            {
                float width = BitmapFont.Measure(first, fontScale);
                if (width <= ir.width - 2f)
                {
                    if (fontScale * 12f <= ir.height - 2f)
                    {
                        break;
                    }
                }
                fontScale -= 1f;
                if (!(1f < fontScale))
                {
                    break;
                }
            }
        }

        Color previous = GUI.color;
        GUI.color = new Color(0.17f, 0.14f, 0.21f, alpha * 0.92f);
        GUI.DrawTexture(new Rect(ir.x, ir.y, ir.width, ir.height), ZoneOverlay.Dot);
        GUI.color = new Color(183f / 255f, 155f / 255f, 98f / 255f, alpha * 0.45f);
        float edge = (int)(s * 0.5f);
        if (edge < 1f)
        {
            edge = 1f;
        }
        GUI.DrawTexture(new Rect(ir.x, ir.y, ir.width, edge), ZoneOverlay.Dot);
        GUI.DrawTexture(new Rect(ir.x, ir.y + ir.height - edge, ir.width, edge), ZoneOverlay.Dot);
        GUI.DrawTexture(new Rect(ir.x, ir.y, edge, ir.height), ZoneOverlay.Dot);
        GUI.DrawTexture(new Rect(ir.x + ir.width - edge, ir.y, edge, ir.height), ZoneOverlay.Dot);
        GUI.color = previous;

        float textWidth = BitmapFont.Measure(first, fontScale);
        float textAlpha = 0f;
        if (0f <= alpha)
        {
            textAlpha = alpha;
            if (1f < alpha)
            {
                textAlpha = 1f;
            }
        }
        float textX = Mathf.Round(ir.x + (ir.width - textWidth) * 0.5f);
        float textY = Mathf.Round(ir.y + (ir.height - fontScale * 12f) * 0.5f);
        BitmapFont.DrawOutline(
            textX,
            textY,
            first,
            new Color(1f, 232f / 255f, 160f / 255f, Mathf.Floor(textAlpha * 255f) / 255f),
            fontScale);
    }

    /// <summary>
    /// Official GameHud.DrawBuffBar (0x06000449) resolves a finite row's icon in two steps:
    /// <c>GameHud.BuffIcon(row.iconId)</c> first and, only when that yields nothing and the row
    /// carries a positive skill id, <c>GameHud.BuffIcon(Win05.SkillIconId(row.skillId))</c>. arm64
    /// 0x438d98 is the first lookup and 0x438dc4 its null test; 0x438e18 is
    /// <c>Win05.SkillIconId</c> (0x0600102f, arm64 rva 0x3b284c) and 0x438e40 the second
    /// <c>GameHud.BuffIcon</c> (0x06000447, arm64 rva 0x43d4d8); x86_64 0x45017c / 0x4501a3 and
    /// 0x4501d3 / 0x4501f6. GameHud.DrawPermBuffBox (0x0600044e) has no such second lookup, so
    /// the permanent column deliberately stays on the single-step path.
    /// </summary>
    private Texture2D LoadFiniteStatusIcon(CharData.BuffView row)
    {
        Texture2D icon = LoadStatusIcon(row.iconId);
        if (!icon && row.skillId > 0)
        {
            icon = LoadStatusIcon(Win05.SkillIconId(row.skillId));
        }

        return icon;
    }

    /// <summary>
    /// The permanent column's single-step resolution: official GameHud.DrawPermBuffBox
    /// (0x0600044e) asks for the row icon id once and has no second lookup, which is why this
    /// overload and not <see cref="LoadFiniteStatusIcon"/> feeds it.
    /// </summary>
    private void DrawStatus(CharData.BuffView row, Rect box, float scale, float alpha)
    {
        DrawStatus(row, box, scale, alpha, LoadStatusIcon(row.iconId));
    }

    /// <summary>
    /// The icon is always resolved by the caller, so this body never re-reads it and a resolved
    /// null can never trigger a third lookup.
    /// </summary>
    private void DrawStatus(
        CharData.BuffView row,
        Rect box,
        float scale,
        float alpha,
        Texture2D icon)
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        Texture2D boxTexture = GameHud.ExceptBox(row.good);
        if (boxTexture)
        {
            GUI.DrawTexture(box, boxTexture);
        }

        Rect iconRect = boxTexture
            ? new Rect(
                box.x + 3f * scale,
                box.y + 3f * scale,
                box.width - 6f * scale,
                box.height - 6f * scale)
            : box;
        if (icon)
        {
            GUI.DrawTexture(iconRect, icon);
        }
        else
        {
            // Official GameHud.DrawBuffBar (0x06000449) arm64 0x439030 and GameHud.DrawPermBuffBox
            // (0x0600044e) arm64 0x439690 both hand the box inset by 3 * scale, the scale, the row
            // alpha and the row display name to the one official GameHud.DrawNameAsIcon
            // (0x0600044c); there is no second fallback body.
            DrawOfficialNameAsIcon(iconRect, row.name, scale, alpha);
        }

        GUI.color = previousColor;
    }

    private static bool IsFiniteNonnegative(float value)
    {
        return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

}
