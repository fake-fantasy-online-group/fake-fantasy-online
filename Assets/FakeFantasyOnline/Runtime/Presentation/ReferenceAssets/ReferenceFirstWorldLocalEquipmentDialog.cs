using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Map-scoped observation/compatibility borrower of the independent EquipDlg root.</summary>
public sealed class ReferenceFirstWorldLocalEquipmentDialog : IDisposable
{
    public const string BackgroundPath = "res/uires/equipdlg/equipdlgbk.gsa";
    public const string AttributeBackgroundPath = "res/uires/equipdlg/secattrbk.gsa";
    public const string VerticalTabNormalPath = "ui/equipdlg05/vtab_f0.png";
    public const string VerticalTabHoverPath = "ui/equipdlg05/vtab_f1.png";
    public const string VerticalTabBluePath = "ui/equipdlg05/vtabblue.png";
    public const string TitleLinePath = "ui/equipdlg05/titleline.png";
    public const string TitleSelectionPath = "ui/equipdlg05/itemsel.png";
    public const string TitleRadioPathBase = "ui/equipdlg05/radiobtn_f";
    public const string TitleOkPathBase = "ui/equipdlg05/okbtn_f";
    public const string TitleDeletePathBase = "ui/equipdlg05/delbtn_f";
    public const string TitleButtonPathBase = "ui/equipdlg05/titlesetbtn_f";
    public const string AssignButtonPathBase = "ui/equipdlg05/assignbtn_f";
    public const string AffirmButtonPathBase = "ui/equipdlg05/affirmbtn_f";
    public const string RestoreButtonPathBase = "ui/equipdlg05/restorebtn_f";
    public const string PlusButtonPathBase = "ui/equipdlg05/plusbtn_f";
    public const string MinusButtonPathBase = "ui/equipdlg05/minusbtn_f";
    public const string EquipmentEffectCheckboxPath = "res/uires/common/checkbox.gsa";
    public const string PointFirePathBase = "ui/equipdlg05/pointfire_f";
    public const string HelpPathBase = "ui/equipdlg05/helpicon_f";
    public const string ClosePathBase = "ui/equipdlg05/miniclose_f";
    public const string UnequipHint = "\n右键卸下";
    public const string MagicUnequipHint = "\n左键拖法宝技到快捷栏 · 右键卸下";
    public const string ToolTargetHint = "\n点击对此装备";
    public const float WindowWidth = 257f;
    public const float WindowHeight = 506f;
    public const int GuiDepth = 10;


    private WorldEntryRecord character;
    private WorldInventoryState inventory = WorldInventoryState.Empty;
    private WorldPlayerStatusState status;
    private bool disposed;
    private EquipDlg Owner => EquipDlg.I;

    private ReferenceFirstWorldLocalEquipmentDialog(WorldEntryRecord character)
    {
        this.character = character;
        status = WorldPlayerStatusState.FromEntry(character);
        GameBootstrap.AcquireEquipmentForIndependentRuntime();
    }

    public bool Ready => !disposed;
    public bool Visible => !disposed && EquipDlg.Opened;
    public int Tab => Owner == null ? 0 : Owner.TabForIndependentRuntime;
    public float WindowX => Owner == null ? -1f : Owner.XForIndependentRuntime;
    public float WindowY => Owner == null ? 0f : Owner.YForIndependentRuntime;
    public WorldInventoryState Inventory => inventory;
    public WorldPlayerStatusState Status => status;
    public WorldInventoryEquippedItemState HoverItem => null;
    public string HoverTooltip => string.Empty; // Native UITip.Show owns publication at the hit.
    public int PendingUnequipCount => 0;
    public bool TitleSubOpen => !disposed && EquipDlg.TitleSubOpen;
    public WorldCharacterTitleState Titles => CharData.CaptureTitleStateForIndependentRuntime();
    public static IReadOnlyList<int> EquipmentSlotParts => EquipDlg.SlotPartsForIndependentRuntime;
    public static IReadOnlyList<Vector2> EquipmentSlotPositions => EquipDlg.SlotPositionsForIndependentRuntime();
    internal static float ResolveInitialX(bool fromEnsureOpen,float scale,float screenWidth) =>
        (screenWidth - WindowWidth * scale) * 0.5f;

    public static bool TryCreate(ReferenceResourceLoader resources,WorldEntryRecord character,
        ReferenceInventoryToolTarget toolTarget,out ReferenceFirstWorldLocalEquipmentDialog owner)
    {
        owner = character == null ? null : new ReferenceFirstWorldLocalEquipmentDialog(character);
        return owner != null;
    }

    // These snapshots support old diagnostics and host ingress accounting only. Native OnGUI
    // reads live CharData/equipped/ToolTarget; no borrow publishes stale data back into them.
    public bool ApplyInventory(WorldInventoryState next)
    { ThrowIfDisposed(); if (next == null) return false; inventory = next; return true; }
    public bool ApplyStatus(WorldPlayerStatusState next)
    { ThrowIfDisposed(); if (next == null) return false; status = next; return true; }
    public bool ApplyAppearance(WorldEntryRecord next)
    { ThrowIfDisposed(); if (!CanApplyAppearance(next)) return false; character = next; return true; }
    public bool CanApplyAppearance(WorldEntryRecord next)
    { ThrowIfDisposed(); return next != null && next.EntityId == character.EntityId; }
    public bool ApplyTitles(WorldCharacterTitleState next)
    { ThrowIfDisposed(); return next != null; }
    public bool ApplySkillBook(WorldSkillBookState next)
    { ThrowIfDisposed(); return next != null; }
    internal bool ApplySkillBookObservation(WorldSkillBookObservation next)
    { ThrowIfDisposed(); return next.HasValue; }

    public void Toggle() { ThrowIfDisposed(); EquipDlg.Toggle(); }
    public void EnsureOpen() { ThrowIfDisposed(); EquipDlg.EnsureOpen(); }
    internal void OpenPanelForIndependentRuntime() { ThrowIfDisposed(); EquipDlg.OpenPanel(); }
    public void Close() { ThrowIfDisposed(); if (EquipDlg.Opened) EquipDlg.Toggle(); }
    public void CloseTitleSubwindow()
    {
        ThrowIfDisposed();
        if (Owner == null) return;
        Owner.TitleOpenForIndependentRuntime = false;
        Owner.TaskOpenForIndependentRuntime = false;
    }
    // Retained ABI for older inspection callers, never a second OnGUI driver.
    public bool Draw(float scale) { ThrowIfDisposed(); return true; }
    public int At(int index) => Owner.At(index);
    internal string[] AttributePageValues() => Owner.AttributePageValuesForIndependentRuntime();
    internal string CriticalPercentTextForAudit(int index,int fallback) => Owner.CritStr(index,fallback);
    internal int PendingPoint(int index) => Owner.PendingPointForIndependentRuntime(index);
    internal int RemainingPoints() => Owner.RemainingPointsForIndependentRuntime();
    internal void AutoAssignPointsForAudit()
    { ThrowIfDisposed(); Owner.AutoAssignPointsForIndependentRuntime(); }
    internal void ConfirmPointAllocationForAudit()
    { ThrowIfDisposed(); Owner.ConfirmPointsForIndependentRuntime(); }
    internal bool EnsureOnScreenForAudit(float scale,int width,int height) =>
        Owner.ApplyScreenBounds(WindowX,WindowY,WindowWidth * scale,WindowHeight * scale,width,height);
    internal void SetWindowPositionForAudit(float x,float y)
    { ThrowIfDisposed(); Owner.XForIndependentRuntime = x; Owner.YForIndependentRuntime = y; Owner.PlacedForIndependentRuntime = true; }
    internal bool ActOnSlotForAudit(int slot,int button)
    { ThrowIfDisposed(); return Owner.ActOnSlotForIndependentRuntime(slot,button); }

    // Native actions reach writers in the click arm. Old queues are intentionally absent;
    // these compatibility reads cannot replay a send through a second host pump.
    public bool TryConsumeUnequip(out int slot) { slot = 0; return false; }
    public bool TryConsumePointAllocation(out int[] points) { points = Array.Empty<int>(); return false; }
    public bool TryConsumeTitle(out string title) { title = string.Empty; return false; }
    public bool TryConsumeEquipmentEffectMask(out int mask) { mask = 0; return false; }
    public bool TryConsumeTreasureSkill(out int id,out Texture2D icon) { id = 0; icon = null; return false; }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        character = null;
        inventory = WorldInventoryState.Empty;
        status = null;
        // Never closes, clears, disables, reloads, destroys or retargets the native owner.
    }
    private void ThrowIfDisposed()
    { if (disposed) throw new ObjectDisposedException(nameof(ReferenceFirstWorldLocalEquipmentDialog)); }
}
}
