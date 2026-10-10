using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of the locked official ToolTarget state, modal and cursor boundary.
/// </summary>
/// <remarks>
/// This owner emits only a server intent. Binding, fused appearance, equipment and inventory are
/// never changed locally. Bag targets and the source-recovered local EquipDlg worn targets share
/// this owner so confirmation, cancellation and authoritative server settlement stay identical.
/// </remarks>
public sealed class ReferenceInventoryToolTarget : IDisposable
{
    public const int SeparateAvatarToolId = 0x3125;
    public const int UnbindToolId = 0x6311;

    public const string AvatarCursorPath =
        "res/uires/cursor/avataritemcuscor.gsa";
    public const string NormalCursorPath = "Cursor/Normal.png";
    public const string SeparateAvatarVerb = "分离变装";
    public const string UnbindVerb = "解除绑定";
    public const string UnboundSuffix = "】未绑定, 无需解绑。";
    public const string MissingAvatarSuffix = "】没有融合变装, 无需分离。";
    public const string ConfirmationPrefix = "确定对【";
    public const string NameSuffix = "】";
    public const string QuestionSuffix = "？";

    public static readonly Vector2 AvatarHotspot = new Vector2(4f, 4f);
    public static readonly Vector2 NormalHotspot = new Vector2(10f, 2f);

    private Action ensureEquipmentOpen;
    private bool disposed;

    private ReferenceInventoryToolTarget() { Ready = true; }

    public bool Ready { get; private set; }

    public bool Active => ToolTarget.Active;

    public int ToolId => ToolTarget.ToolId;

    public bool EquippedTargetSurfaceRequested => false;

    internal Texture2D AvatarCursor => CursorManager.I?.AvatarTextureForIndependentRuntime;

    internal Texture2D NormalCursor => CursorManager.I?.NormalTextureForIndependentRuntime;

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        ReferenceSystemMessageBox messageBox,
        out ReferenceInventoryToolTarget owner)
    {
        owner = new ReferenceInventoryToolTarget();
        return true;
    }

    internal void BindEquipmentOpeningForIndependentRuntime(Action open) => ensureEquipmentOpen = open;

    public bool Begin(int toolId)
    {
        ThrowIfDisposed();
        ToolTarget.BeginForIndependentRuntime(toolId, ensureEquipmentOpen);
        return true;
    }

    public void Cancel()
    {
        ThrowIfDisposed();
        ToolTarget.Cancel();
    }

    public bool TryPickBag(WorldInventoryItemState item)
    {
        ThrowIfDisposed();
        if (!Active || item == null || item.Pet != null ||
            (item.Tab != 2 && ToolId != UnbindToolId)) return false;
        ToolTarget.PickBag(item.Tab, item.BagIndex, item.DisplayName, item.Bound, item.Avatar > 0);
        return true;
    }

    public bool TryPickEquipped(WorldInventoryEquippedItemState item)
    {
        ThrowIfDisposed();
        if (!Active || item == null) return false;
        ToolTarget.PickEquip(item.Slot, item.DisplayName, item.Avatar > 0);
        return true;
    }

    // Requests now go directly through native NetClient at confirmation time.
    public bool TryConsume(out int toolId, out byte targetKind, out byte tab, out int slot)
    {
        toolId = 0; targetKind = 0; tab = 0; slot = -1;
        return false;
    }

    public static string Verb(int toolId)
    {
        return ToolTarget.ResolveVerb(toolId);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ToolTarget.Cancel();
        Ready = false;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceInventoryToolTarget));
        }
    }

}
}
