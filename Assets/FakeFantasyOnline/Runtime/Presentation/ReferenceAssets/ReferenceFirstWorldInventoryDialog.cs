using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Map-scoped observation borrower of the independent InventoryDlg root.</summary>
public sealed class ReferenceFirstWorldInventoryDialog : IDisposable
{
    public const string EquipBindingConfirmation =
        "装备后将绑定(绑定后不可交易), 是否装备？";
    public const string CaptureMonsterRequiredMessage =
        "请先选中一个怪物, 再使用【惊喜闪电】";
    public const string BoundPetUseConfirmation =
        "此道具已绑定, 使用后宠物也将【绑定】(绑定后不可交易、不可解除绑定), 是否继续?";
    public const string BackgroundPath = "ui/waredlg05/waredlgbk50.png";
    public const string SlotPath = "ui/waredlg05/slot.png";
    public const string FocusBackgroundPath = "res/effect/focusbk0.gsa";
    public const string SplitBackgroundPath = "res/uires/gameui/numinput.gsa";
    public const string SplitSliderPath = "res/uires/gameui/smallslide.gsa";
    public const string SplitAffirmativePath = "res/uires/gameui/inputbtn.gsa";
    public const string SplitDecrementPath = "res/uires/gameui/spinleft.gsa";
    public const string SplitIncrementPath = "res/uires/gameui/spinright.gsa";
    public const string SplitMinimumPath = "res/uires/common/minbtn.gsa";
    public const string SplitMaximumPath = "res/uires/common/maxbtn.gsa";
    public const float WindowWidth = 423f;
    public const float WindowHeight = 501f;
    public const float SplitWindowWidth = 147f;
    public const float SplitWindowHeight = 41f;
    public const int SplitMaximumCount = 999;

    private static readonly Color ItemFocusBackgroundColor =
        new Color(0.92f, 0.12f, 0.10f, 0.55f);
    private const string CurrentEquipmentTooltipPrefix =
        "\u0007\u0006（当前穿戴）\n";
    internal const float CoinFlowStartX = 18f;
    internal const float CoinRowY = 479f;
    internal const float CoinIconOffsetX = 2f;
    internal const float CoinTextOffsetX = 16f;
    internal const float CoinGapPixels = 6f;
    internal const float WeightRightX = 408f;
    internal const float CaiYuX = 232f;
    internal const string WeightPrefix = "负重 ";
    internal const string CaiYuPrefix = "彩玉 ";
    internal static readonly Color OverweightColor =
        new Color(1f, 0.3137255f, 0.3137255f, 1f);
    internal static readonly Color CaiYuColor =
        new Color(0.55f, 0.92f, 0.85f, 1f);

    private readonly ReferenceInventoryToolTarget toolTarget;
    private WorldInventoryState state = WorldInventoryState.Empty;
    private bool disposed;
    private InventoryDlg Owner => InventoryDlg.I;

    private ReferenceFirstWorldInventoryDialog(ReferenceInventoryToolTarget toolTarget)
    {
        this.toolTarget = toolTarget;
        // Do not create, load, open, reposition or disable the native root on map admission.
        // Toggle / DockRightOf and their existing native callers own creation.
    }

    public bool Ready => !disposed;
    public bool Visible => !disposed && InventoryDlg.IsOpen;
    public float WindowX => Owner == null ? -1f : Owner.XForIndependentRuntime;
    public float WindowY => Owner == null ? 0f : Owner.YForIndependentRuntime;
    public int Tab => Owner == null ? 0 : Owner.TabForIndependentRuntime;
    public WorldInventoryState State => state;
    public WorldInventoryItemState HeldItem => InventoryDlg.HasHeld
        ? state.ItemAt(InventoryDlg.HeldTab, InventoryDlg.HeldSlot) : null;
    public bool HasHeld => InventoryDlg.HasHeld;
    public bool TradeSelectionEnabled => DealDlg.Visible && SocialClient.TradeActive;
    internal ReferenceInventoryToolTarget ToolTarget => toolTarget;
    internal bool SplitOpen => InventoryDlg.SplitOpen;
    internal bool SplitDragging => InventoryDlg.SplitDraggingForIndependentRuntime;
    internal int SplitMaximum => InventoryDlg.SplitMaximumForIndependentRuntime;
    internal int SplitValue => InventoryDlg.SplitValueForIndependentRuntime;
    internal string SplitText => InventoryDlg.SplitTextForIndependentRuntime;
    internal Rect SplitWindowRect => InventoryDlg.SplitRectForIndependentRuntime(1f);

    // Historical diagnostics only. Gameplay reads the current native CombatWorld / CharData.
    public int CaptureTargetEntityId { get; set; }
    public bool CurrentPetBound { get; set; }

    public static bool TryCreate(ReferenceResourceLoader resources, ReferenceSystemMessageBox messageBox,
        ReferenceGameHudItemCooldowns itemCooldowns, IReadOnlyList<Texture2D> cooldownFrames,
        out ReferenceFirstWorldInventoryDialog owner)
    {
        owner = null;
        if (resources == null) return false;
        ReferenceInventoryToolTarget tool;
        if (!ReferenceInventoryToolTarget.TryCreate(resources, messageBox, out tool)) return false;
        owner = new ReferenceFirstWorldInventoryDialog(tool);
        return true;
    }

    // Receipt projection is owned by WindowOwner's existing publication/sync boundary.
    // This snapshot supports diagnostics and the existing Mail bridge, never native painting.
    public bool ApplyState(WorldInventoryState next)
    {
        ThrowIfDisposed();
        if (next == null) return false;
        state = next;
        return true;
    }

    public void Open() { ThrowIfDisposed(); if (!InventoryDlg.IsOpen) InventoryDlg.Toggle(); }
    public void Toggle() { ThrowIfDisposed(); InventoryDlg.Toggle(); }
    public void ClearHeld()
    { ThrowIfDisposed(); InventoryDlg.BindHeldIdentityForIndependentRuntime(0, -1, null); }
    public bool MouseOver() => !disposed && Owner != null && Owner.MouseOver();

    // Preserve the established merged-host destination dispatch. It passes the original
    // Event to the original held tail; Use is shared with the automatic window callback.
    internal void DispatchHeldTargetClickForIndependentRuntime(Event ev, float scale)
    {
        if (!Ready || !Visible || ev == null || ev.type != EventType.MouseDown ||
            (!InventoryDlg.HasHeld && !InventoryDlg.HeldFromWarehouse)) return;
        bool target = GameHud.PosOverHotbar(ev.mousePosition) || StoreDlg.MouseOver() || ComposeDlg.MouseOver() ||
            MailDlg.ContainsPointer(ev.mousePosition, scale) || Win05.NativeBagTargetHitForIndependentRuntime() ||
            (InventoryDlg.HeldPetForIndependentRuntime && Win05.HitPetSlot(ev.mousePosition) >= 0);
        if (!target) return;
        Rect window = new Rect(WindowX, WindowY, WindowWidth * scale, WindowHeight * scale);
        if (!window.Contains(ev.mousePosition)) Owner.HeldClickForIndependentRuntime(window, ev);
    }

    // Compatibility signatures have no queues and cannot redraw or resend native actions.
    public bool Draw(float scale) { ThrowIfDisposed(); return true; }
    public bool TryConsumeSort(out byte tab) { tab = 0; return false; }
    public bool TryConsumeEquip(out int slot) { slot = -1; return false; }
    public bool TryConsumeUse(out int id, out byte tab, out int slot)
    { id = 0; tab = 0; slot = -1; return false; }
    public bool TryConsumeCaptureUse(out int target, out int id)
    { target = 0; id = 0; return false; }
    public bool TryConsumeToolUse(out int id, out byte kind, out byte tab, out int slot)
    { id = 0; kind = 0; tab = 0; slot = -1; return false; }
    public bool TryConsumeSplit(out byte tab, out int slot, out int count)
    { tab = 0; slot = -1; count = 0; return false; }
    public string ConsumeCaptureFeedback() => string.Empty;

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        state = WorldInventoryState.Empty;
        // Only the borrower ends. No native close/held/split/tool reset, no root destruction,
        // no resource replacement and no change of Unity's automatic callback ownership.
    }

    // Existing explicit source-fixture entry points use the same original fields/helpers.
    // They are not normal input routes and do not change the live window's selected tab.
    private InventoryDlg AuditOwner => InventoryDlg.EnsureOwnerForIndependentRuntime();
    private bool ActivateForAudit(byte itemTab, int bagIndex)
    {
        ThrowIfDisposed();
        InventoryDlg.Item item = InventoryDlg.ItemAt(itemTab, bagIndex);
        if (!InventoryDlg.Items[itemTab].Exists(it => it.bagIdx == bagIndex)) return false;
        int selectedTab = itemTab;
        AuditOwner.ActivateOrdinaryItemForIndependentRuntime(ref selectedTab, item);
        ClearHeld();
        return true;
    }
    internal bool BeginEquipForAudit(byte itemTab, int bagIndex) => ActivateForAudit(itemTab, bagIndex);
    internal bool BeginOrdinaryUseForAudit(byte itemTab, int bagIndex, float now) => ActivateForAudit(itemTab, bagIndex);
    internal bool BeginCaptureUseForAudit(byte itemTab, int bagIndex, float now) => ActivateForAudit(itemTab, bagIndex);
    internal bool BeginPetBindUseForAudit(byte itemTab, int bagIndex, float now) => ActivateForAudit(itemTab, bagIndex);
    internal bool BeginToolTargetForAudit(byte itemTab, int bagIndex) => ActivateForAudit(itemTab, bagIndex);
    internal bool PickToolBagForAudit(byte itemTab, int bagIndex) => toolTarget.TryPickBag(state.ItemAt(itemTab, bagIndex));
    internal bool PickToolEquippedForAudit(WorldInventoryEquippedItemState item) => toolTarget.TryPickEquipped(item);
    internal void CancelToolTargetForAudit() => toolTarget.Cancel();
    internal bool BeginSplitForAudit(byte itemTab, int bagIndex, Vector2 pointer, float scale,
        int viewportWidth, int viewportHeight)
    {
        InventoryDlg.Item item = InventoryDlg.ItemAt(itemTab, bagIndex);
        if (item.count < 2 || item.pet.HasValue) return false;
        AuditOwner.OpenSplitForIndependentRuntime(itemTab, bagIndex, item.count, item.id, item.icon, pointer, scale);
        return true;
    }
    internal void SetSplitValueForAudit(int value) => AuditOwner.SetSplitValueForIndependentRuntime(value);
    internal void SetSplitTextForAudit(string value) => InventoryDlg.SetSplitTextForIndependentRuntime(value);
    internal void SetSplitFromXForAudit(float x, float scale) => AuditOwner.SetSplitFromXForIndependentRuntime(x, scale);
    internal bool CommitSplitForAudit()
    {
        if (!InventoryDlg.SplitOpen) return false;
        InventoryDlg.CommitSplitForIndependentRuntime();
        return true;
    }
    internal void CloseSplitForAudit() => InventoryDlg.CloseSplit();
    internal bool EnsureOnScreenForAudit(float scale, int width, int height) =>
        AuditOwner.ApplyScreenBounds(WindowX, WindowY, WindowWidth * scale, WindowHeight * scale, width, height);
    internal void SetWindowPositionForAudit(float x, float y)
    {
        InventoryDlg owner = AuditOwner;
        owner.XForIndependentRuntime = x;
        owner.YForIndependentRuntime = y;
        owner.PlacedForIndependentRuntime = true;
    }
    internal static bool IsActivationGestureForAudit(EventType type, int button, int clickCount, bool shift, bool command) =>
        type == EventType.MouseDown && (button == 1 || (button == 0 && clickCount == 2));
    internal static float AdvanceCoinFlow(float x, float width) => x + width + CoinTextOffsetX + CoinGapPixels;
    internal static string WeightText(int weight, int maximum) => WeightPrefix + weight + "/" + maximum;
    internal static string CaiYuText(long value) => CaiYuPrefix + value;
    internal static Color WeightColor(int weight, int maximum) =>
        maximum < 1 || weight <= maximum ? Color.white : OverweightColor;
    internal static string FormatTooltipForAudit(string name, byte quality, string description) =>
        TooltipContent.Fmt(TooltipContent.QualityName(name, quality), description);
    internal static Color ItemFocusBackgroundColorForAudit => ItemFocusBackgroundColor;
    internal string ComparisonTooltipForAudit(byte itemTab, int bagIndex)
    {
        InventoryDlg.Item item = InventoryDlg.ItemAt(itemTab, bagIndex);
        if (item.slot < 0 || item.cosmetic) return string.Empty;
        foreach (EquipDlg.EquippedItem equipped in EquipDlg.Equipped)
        {
            if (equipped.id <= 0 || (equipped.slot != item.slot &&
                !((item.slot == 4 || item.slot == 13) && (equipped.slot == 4 || equipped.slot == 13)))) continue;
            return CurrentEquipmentTooltipPrefix + TooltipContent.Fmt(
                TooltipContent.QualityName(equipped.name, equipped.quality), equipped.desc);
        }
        return string.Empty;
    }
    private void ThrowIfDisposed()
    { if (disposed) throw new ObjectDisposedException(nameof(ReferenceFirstWorldInventoryDialog)); }
}
}
