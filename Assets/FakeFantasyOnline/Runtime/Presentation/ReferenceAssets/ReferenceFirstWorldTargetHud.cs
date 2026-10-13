using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Immutable ISelectable projection consumed by the recovered GameHud target branch.
/// </summary>
public sealed class ReferenceFirstWorldTargetHudSnapshot
{
    public ReferenceFirstWorldTargetHudSnapshot(
        int entityId,
        string displayName,
        int level,
        int hitPoints,
        int maximumHitPoints,
        bool friendly,
        bool usePlayerFrame,
        bool canOpenPlayerActionMenu,
        int localPlayerLevel,
        bool queryTargetStatus = true)
    {
        EntityId = entityId;
        StatusQueryEntityId = queryTargetStatus ? entityId : 0;
        DisplayName = displayName ?? string.Empty;
        Level = level;
        HitPoints = hitPoints;
        MaximumHitPoints = maximumHitPoints;
        Friendly = friendly;
        UsePlayerFrame = usePlayerFrame;
        CanOpenPlayerActionMenu = canOpenPlayerActionMenu;
        LocalPlayerLevel = localPlayerLevel;
    }

    public int EntityId { get; }
    public int StatusQueryEntityId { get; }
    public string DisplayName { get; }
    public int Level { get; }
    public int HitPoints { get; }
    public int MaximumHitPoints { get; }
    public bool Friendly { get; }
    public bool UsePlayerFrame { get; }
    public bool CanOpenPlayerActionMenu { get; }
    public int LocalPlayerLevel { get; }
}

/// <summary>
/// Destination intent emitted by the recovered target background.
/// </summary>
public enum ReferenceFirstWorldTargetHudAction
{
    None = 0,
    OpenPlayerActionMenu = 1
}

/// <summary>
/// Source translation of GameHud.DrawTargetBar and its target-status row.
/// </summary>
/// <remarks>
/// PlayerActionMenu remains a separate official owner. This boundary exposes exact hover text to
/// the adjacent shared UITip owner. Status icons come from the shared <see cref="GameHud.BuffIcon"/>
/// owner, the same helper the buff column uses; when it yields nothing the row draws the exact
/// red/green original fallback.
/// </remarks>
public sealed class ReferenceFirstWorldTargetHud : IDisposable
{
    public const bool ShowTargetByDefault = true;
    public const string HitPointTooltipFormat = "气血 {0}/{1}";
    public const string GoodBoxPath = "res/effect/goodexceptbox.gsa";
    public const string BadBoxPath = "res/effect/badexceptbox.gsa";
    public const int MaximumVisibleStatusRows = 6;

    private static readonly Color FriendlyNameColor =
        new Color32(77, 197, 11, 255);
    private static readonly Color GoodFallbackColor =
        new Color(0.35f, 0.75f, 0.35f, 0.75f);
    private static readonly Color BadFallbackColor =
        new Color(0.8f, 0.3f, 0.3f, 0.75f);

    private readonly ReferenceFirstWorldHudResources resources;
    private readonly ReferenceWorldBitmapFont font;
    private bool disposed;
    private GameHud hudOwner;
    internal void BindHudOwnerForIndependentRuntime(GameHud owner) => hudOwner = owner;

    private ReferenceFirstWorldTargetHud(
        ReferenceFirstWorldHudResources resources,
        ReferenceWorldBitmapFont font)
    {
        this.resources = resources;
        this.font = font;
        Ready = true;
    }

    public bool Ready { get; private set; }

    public ReferenceFirstWorldTargetHudSnapshot Snapshot { get; private set; }

    /// <summary>
    /// Exact UITip text under the current pointer, consumed by the shared owner after this branch.
    /// </summary>
    public string HoverTooltip { get; private set; } = string.Empty;

    public int LastStatusRowsDrawn { get; private set; }

    public int LastActionTargetId { get; private set; }

    public static bool TryCreate(
        ReferenceResourceLoader resourceLoader,
        ReferenceFirstWorldHudResources resources,
        out ReferenceFirstWorldTargetHud owner)
    {
        owner = null;
        // Native Load may publish nulls or a partial array. Construct the attached view
        // without advancing the one-shot GUI load or inspecting its future results.
        if (resourceLoader == null || resources == null || !resources.Ready)
        {
            return false;
        }

        ReferenceWorldBitmapFont font;
        if (!ReferenceWorldBitmapFont.TryCreate(resourceLoader, out font) ||
            font == null)
        {
            return false;
        }

        // Official GameHud.DrawTargetStatusRow (0x0600041e) calls GameHud.ExceptBox (0x06000448)
        // at arm64 4443308, inside its own extent [4442380, 4444456): the row box is resolved at
        // draw time, so this owner carries no construction-time box resource gate.
        owner = new ReferenceFirstWorldTargetHud(resources, font);
        return true;
    }

    /// <summary>
    /// Replaces the ISelectable projection only. The official cache drop is not a property of the
    /// incoming selection: GameHud.PollTargetStatus (0x0600041d) clears tgStatus and zeroes
    /// tgStatusOf in its target-change arm, which tests the selection against the poll slot
    /// tgPollId (GameHud static +0x10, field ordinals 11-15 of the frozen index), never against
    /// the id of the reply currently cached. That arm lives in <see cref="GameHud.PollTargetStatus"/>.
    /// </summary>
    public bool SetTarget(ReferenceFirstWorldTargetHudSnapshot snapshot)
    {
        if (!Ready || disposed)
        {
            return false;
        }

        Snapshot = snapshot;
        return true;
    }

    /// <summary>
    /// Compatibility caller: ReferenceFirstWorldTargetHudAudit still projects a reply here.
    /// It uses exactly the normal receipt setter. The old supplied timestamp is ignored;
    /// diagnostics cannot create a second clock or a per-window status owner.
    /// </summary>
    public bool ApplyTargetStatus(WorldTargetStatusState state, float receivedAt)
    {
        GameHud.SetTargetStatus(state.EntityId, ReferenceTargetStatusAdapter.ToNativeRows(state.Rows));
        return true;
    }

    public bool Draw(out ReferenceFirstWorldTargetHudAction action, bool nativeInvocation = false)
    {
        action = ReferenceFirstWorldTargetHudAction.None;
        HoverTooltip = string.Empty;
        LastStatusRowsDrawn = 0;
        if (hudOwner != null && !hudOwner.showTarget) return true;
        // The actual GameHud.OnGUI caller supplies only its original showTarget gate.
        // Explicit compatibility draws retain their own view/viewport admission.
        if (!nativeInvocation && (!Ready || disposed || Screen.height < 1))
        {
            return false;
        }

        float scale = hudOwner != null ? hudOwner.s : UIScale.Factor;
        // These are stages of one native OnGUI call: preserve incoming GUI state and
        // each reached leaf's own normal/exceptional restoration behavior.
        action = DrawAt(scale);

        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Snapshot = null;
        HoverTooltip = string.Empty;
        LastStatusRowsDrawn = 0;
        LastActionTargetId = 0;
        Ready = false;
        disposed = true;
    }

    public static Rect TargetBarRect(float scale)
    {
        return ScaleRect(72f, 108f + EndgamePanel.RiftTargetHudOffset, 128f, 40f, scale);
    }

    /// <summary>
    /// Name row of <c>GameHud.DrawTargetBar</c> (0x0600041b): every row comes from the local helper
    /// <c>GameHud.&lt;DrawTargetBar&gt;g__P|0</c> (0x0600046d, arm64 0x44617c / x86_64 0x45e1b0),
    /// which returns <c>new Rect(72*s + x*s, 108*s + y*s, w*s, h*s)</c>. The name row calls it with
    /// (28, 5, 94, 12) at arm64 0x43326c, so x is (72+28)*s, not (72+30)*s. Level (2,14,20,20),
    /// HP (24,22,102,12) and status (44 + index*13, 37, 12, 12) rows use the same helper.
    /// </summary>
    public static Rect TargetNameRect(float scale)
    {
        return ScaleRect(100f, 113f + EndgamePanel.RiftTargetHudOffset, 94f, 12f, scale);
    }

    public static Rect TargetLevelRect(float scale)
    {
        return ScaleRect(74f, 122f + EndgamePanel.RiftTargetHudOffset, 20f, 20f, scale);
    }

    public static Rect TargetHitPointRect(float scale)
    {
        return ScaleRect(96f, 130f + EndgamePanel.RiftTargetHudOffset, 102f, 12f, scale);
    }

    public static Rect TargetStatusRect(int visibleIndex, float scale)
    {
        if (visibleIndex < 0 || visibleIndex >= MaximumVisibleStatusRows)
        {
            throw new ArgumentOutOfRangeException(nameof(visibleIndex));
        }

        return ScaleRect(
            72f + 44f + visibleIndex * 13f,
            108f + 37f + EndgamePanel.RiftTargetHudOffset,
            12f,
            12f,
            scale);
    }

    /// <summary>
    /// Icon rect for one status row. The official row only reaches this inset after its frame
    /// texture was drawn and only while the scale is positive, so callers start from the outer
    /// <see cref="TargetStatusRect"/> and narrow on that arm.
    /// </summary>
    public static Rect TargetStatusIconRect(int visibleIndex, float scale)
    {
        Rect box = TargetStatusRect(visibleIndex, scale);
        return new Rect(
            box.x + scale,
            box.y + scale,
            box.width - 2f * scale,
            box.height - 2f * scale);
    }

    public static Color TargetNameColor(bool friendly, int targetLevel, int localPlayerLevel)
    {
        if (friendly)
            return new Color(0.3019607961177826f, 0.772549033164978f, 0.04313725605607033f, 1f);
        if (targetLevel <= 0)
            return new Color(0.9254902005195618f, 0.843137264251709f, 0.12941177189350128f, 1f);
        int difference = unchecked(targetLevel - localPlayerLevel);
        if (difference > 9)
            return new Color(0.9647058844566345f, 0.07058823853731155f, 0.7843137383460999f, 1f);
        if (difference > 2)
            return new Color(0.9921568632125854f, 0.35686275362968445f, 0.0784313753247261f, 1f);
        if (difference >= 0)
            return new Color(0.9254902005195618f, 0.843137264251709f, 0.12941177189350128f, 1f);
        if (difference > -10)
            return new Color(0.3019607961177826f, 0.772549033164978f, 0.04313725605607033f, 1f);
        return new Color(0.7411764860153198f, 0.7019608020782471f, 0.7019608020782471f, 1f);
    }

    public static int HitPointFrame(int current, int maximum)
    {
        return ReferenceFirstWorldPlayerStatusHud.ProgressFrame(current, maximum);
    }

    public static float HitPointFraction(int current, int maximum)
    {
        return ReferenceFirstWorldPlayerStatusHud.ProgressFraction(current, maximum);
    }

    public static string HitPointTooltip(int current, int maximum)
    {
        return string.Format(
            CultureInfo.CurrentCulture,
            HitPointTooltipFormat,
            current,
            maximum);
    }

    public static int RemainingMilliseconds(
        WorldTargetStatusRow row,
        int elapsedMilliseconds)
    {
        return RemainingMilliseconds(ReferenceTargetStatusAdapter.ToNativeRow(row), elapsedMilliseconds);
    }

    private static int RemainingMilliseconds(GameHud.TgSt row, int elapsedMilliseconds)
    {
        if (row.remainMs < 0)
        {
            return int.MaxValue;
        }

        return unchecked(row.remainMs - elapsedMilliseconds);
    }

    public static string StatusTooltip(
        WorldTargetStatusRow row,
        int remainingMilliseconds)
    {
        return StatusTooltip(ReferenceTargetStatusAdapter.ToNativeRow(row), remainingMilliseconds);
    }

    private static string StatusTooltip(GameHud.TgSt row, int remainingMilliseconds)
    {
        string value = row.name;
        if (!string.IsNullOrEmpty(row.desc))
        {
            value += "\n" + row.desc;
        }

        if (row.remainMs >= 0)
        {
            int seconds = Math.Max(remainingMilliseconds / 1000, 1);
            string duration = remainingMilliseconds < 60_000
                ? string.Format(CultureInfo.CurrentCulture, "{0}秒", seconds)
                : string.Format(
                    CultureInfo.CurrentCulture,
                    "{0}分{1}秒",
                    seconds / 60,
                    seconds % 60);
            value += "\n剩余 " + duration;
        }

        return value;
    }

    private ReferenceFirstWorldTargetHudAction DrawAt(float scale)
    {
        // 0600041b captures the original receiver once. Snapshot is retained only for
        // existing projection/Editor callers; it cannot replace these live getter reads.
        ISelectable target = TargetSelect.Current;
        if (target == null || target.SelTransform == null)
            return ReferenceFirstWorldTargetHudAction.None;

        float left = 72f * scale;
        float top = (108f + EndgamePanel.RiftTargetHudOffset) * scale;
        Rect P(float x, float y, float width, float height) =>
            new Rect(left + x * scale, top + y * scale, width * scale, height * scale);

        bool playerFrame = target.SelOwnerId >= 0
            ? target.SelOwnerId == 0 || target.SelOwnerId == NetClient.SelfEntityId ||
                SocialClient.InParty(target.SelOwnerId)
            : target.SelFriendly;
        Texture2D backgroundTexture = (playerFrame ? resources.TargetPlayer : resources.TargetMonster)
            ?? resources.TargetMonster ?? resources.TargetPlayer;
        Rect background = P(0f, 0f, 128f, 40f);
        if (backgroundTexture) GUI.DrawTexture(background, backgroundTexture);

        ReferenceFirstWorldTargetHudAction action = ReferenceFirstWorldTargetHudAction.None;
        // Original RemotePlayer type/field arm, before labels, HP and status polling.
        // Open uses its own pointer source; do not replace it with the late host snapshot.
        if (target is RemotePlayer remote && background.Contains(Event.current.mousePosition) &&
            Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            PlayerActionMenu.Open(remote.id, remote.pname);
            Event.current.Use();
            action = ReferenceFirstWorldTargetHudAction.OpenPlayerActionMenu;
            LastActionTargetId = remote.id;
        }

        Color nameColor = GameHud.TargetNameColor(target);
        font.Measure(".", scale);
        if (font.Ready)
        {
            font.CenterOutline(P(28f, 5f, 94f, 12f), target.SelName ?? string.Empty, nameColor, scale);
            if (target.SelLevel > 0)
                font.CenterOutline(P(2f, 14f, 20f, 20f), target.SelLevel.ToString(), nameColor, scale);
        }

        if (target.SelHp >= 0 && target.SelMaxHp > 0)
        {
            Rect hitPoints = P(24f, 22f, 102f, 12f);
            int frameIndex = HitPointFrame(target.SelHp, target.SelMaxHp);
            Texture2D fill = resources.TargetHpFillFrames != null &&
                resources.TargetHpFillFrames[frameIndex] != null
                    ? resources.TargetHpFillFrames[frameIndex] : resources.TargetHpFill;
            // Reuse the recovered cover-only ProgBar body, with its later live vitals reads.
            float fraction = HitPointFraction(target.SelHp, target.SelMaxHp);
            if (fill && fraction > 0f)
                GUI.DrawTextureWithTexCoords(
                    new Rect(hitPoints.x, hitPoints.y, hitPoints.width * fraction, hitPoints.height),
                    fill, new Rect(0f, 0f, fraction, 1f), false);

            if (hitPoints.Contains(Event.current.mousePosition))
            {
                HoverTooltip = HitPointTooltip(target.SelHp, target.SelMaxHp);
                UITip.Show(HoverTooltip, scale);
            }
        }

        int statusId = target is RemotePlayer player ? player.id :
            target is MonsterEntity monster ? monster.id : 0;
        GameHud.PollTargetStatus(statusId);
        DrawStatusRows(scale, P, statusId);
        return action;
    }

    private void DrawStatusRows(float scale, Func<float, float, float, float, Rect> position, int statusId)
    {
        if (statusId < 1 || GameHud.tgStatusOf != statusId || GameHud.tgStatus.Count == 0)
            return;

        // 0600041e samples clock/receive time, then mouse, then color. The reached
        // null Event branch (ARM 43d0cc) throws; it is not an empty-row return.
        float elapsedWindow = Time.realtimeSinceStartup - GameHud.tgStatusRecv;
        Vector2 mouse = Event.current.mousePosition;
        Color previousColor = GUI.color;
        // The official clock is one truncating cast of (realtimeSinceStartup - tgStatusRecv) *
        // 1000f. arm64 0x43cae4-0x43cafc (fmul, fcvtzs, fcmp against 0x7f800000, csel to
        // int.MinValue) and x86_64 0x45474e-0x45475d (mulss by 1000f then cvttss2si) carry no
        // floor at zero and no ceiling at int.MaxValue.
        elapsedWindow *= 1000f;
        int elapsedMilliseconds = elapsedWindow == float.PositiveInfinity
            ? int.MinValue
            : (int)elapsedWindow;
        int visibleIndex = 0;
        for (int sourceIndex = 0;
             sourceIndex < GameHud.tgStatus.Count &&
             visibleIndex < MaximumVisibleStatusRows;
             sourceIndex++)
        {
            GameHud.TgSt row = GameHud.tgStatus[sourceIndex];
            int remaining = RemainingMilliseconds(row, elapsedMilliseconds);
            if (row.remainMs >= 0 && remaining <= 0)
            {
                continue;
            }

            Rect box = position(44f + visibleIndex * 13f, 37f, 12f, 12f);
            Texture2D boxTexture = GameHud.ExceptBox(row.good);
            // The icon keeps the whole outer rect until a frame texture is really drawn, and the
            // inset is applied only while the scale is positive: arm64 0x43ccdc (null frame skips
            // the draw), 0x43cd18/0x43cd2c (fcmp s,0.0 then b.le), 0x43cd40-0x43cd54 (x + s,
            // y + s, w - 2s, h - 2s) and x86_64 0x454a11, 0x454a4b/0x454a55, 0x454a73-0x454a81.
            Rect iconRect = box;
            if (boxTexture)
            {
                GUI.DrawTexture(box, boxTexture);
                if (scale > 0f)
                {
                    iconRect = new Rect(box.x + scale, box.y + scale,
                        box.width - 2f * scale, box.height - 2f * scale);
                }
            }

            // The official row asks the shared GameHud.BuffIcon (0x06000447) owner, exactly like
            // the buff column: arm64 0x43cd68 and x86_64 0x454ab2 hand it the raw TgSt.icon and
            // its "iconId < 1 returns null" gate lives inside that helper (arm64 0x43d55c).
            Texture2D icon = GameHud.BuffIcon(row.icon);
            if (icon)
            {
                GUI.DrawTexture(iconRect, icon);
            }
            else
            {
                GUI.color = row.good
                    ? GoodFallbackColor
                    : BadFallbackColor;
                GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
                GUI.color = previousColor;
            }

            // Outer frame hit test, inclusive on the low edge and exclusive on the high edge on
            // both axes: arm64 0x43cf00/0x43cf04 (mouse.x >= box.x), 0x43cf34-0x43cf44 (mouse.x <
            // box.xMax and mouse.y >= box.y), 0x43cf70-0x43cf80 (mouse.y < box.yMax) and x86_64
            // 0x454c10/0x454c13, 0x454c30-0x454c3b, 0x454cbc-0x454ccf, 0x454cf1-0x454d0b. The row
            // has no EventType test and consumes nothing, so it stays a tooltip-only arm.
            if (box.x <= mouse.x && mouse.x < box.x + box.width &&
                box.y <= mouse.y && mouse.y < box.y + box.height)
            {
                HoverTooltip = StatusTooltip(row, remaining);
                UITip.Show(HoverTooltip, scale);
            }

            visibleIndex++;
        }

        GUI.color = previousColor;
        LastStatusRowsDrawn = visibleIndex;
    }

    private static Rect ScaleRect(
        float x,
        float y,
        float width,
        float height,
        float scale)
    {
        return new Rect(
            x * scale,
            y * scale,
            width * scale,
            height * scale);
    }

}

}
