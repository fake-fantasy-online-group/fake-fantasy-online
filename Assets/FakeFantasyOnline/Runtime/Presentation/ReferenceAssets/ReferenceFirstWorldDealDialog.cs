using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Observation and session-lifetime adapter for the automatic native DealDlg.
public sealed class ReferenceFirstWorldDealDialog : IDisposable
{
    public const string BackgroundPath = "res/uires/dealdlg/dealdlgbk.gsa";
    public const string ClosePath = "res/uires/common/miniclose.gsa";
    public const string HelpPath = "res/uires/gameui/helpicon.gsa";
    public const string LockPath = "res/uires/common/affirmbtn.gsa";
    public const string DealPath = "res/uires/dealdlg/dealokbtn.gsa";
    public const string CancelPath = "res/uires/common/closebtn.gsa";
    public const string GoldPath = "res/uires/common/gold.gsa";
    public const string SilverPath = "res/uires/common/silver.gsa";
    public const string CopperPath = "res/uires/common/copper.gsa";
    public const string FusedTradeAction = "放到交易栏";
    public const string FusedMessageDescriptionPrefix = "】上融合着 ";
    public const string FusedMessageActionPrefix = "。\n";
    public const string FusedMessageWarning =
        "会把它一起带走, 且不会自动退回背包。\n\n" +
        "(想留下融合的部分: 先用分离卷轴取下来, 神奇货架 5 彩玉)\n\n确定要";

    public const float WindowWidth = 454f;
    public const float WindowHeight = 272f;

    private ReferenceFirstWorldInventoryDialog inventory;
    private readonly DealDlg owner;
    private WorldActiveTradeState state = WorldActiveTradeState.Empty;
    private ClientSessionCoordinator observationSession;
    internal ClientSessionCoordinator ObservationSession
    {
        private get => observationSession;
        set { ThrowIfDisposed(); observationSession = value; }
    }
    private bool disposed;
    private ReferenceFirstWorldDealDialog(ReferenceFirstWorldInventoryDialog inventory)
    {
        RebindInventory(inventory);
        owner = DealDlg.BindForIndependentRuntime();
    }
    internal void RebindInventory(ReferenceFirstWorldInventoryDialog value)
    {
        ThrowIfDisposed();
        inventory = value ?? throw new ArgumentNullException(nameof(value));
    }
    public bool Ready => !disposed;
    public bool Visible => !disposed && DealDlg.Visible;
    public float WindowX => owner.XForIndependentRuntime;
    public float WindowY => owner.YForIndependentRuntime;
    // Native DealDlg reads SocialClient directly. Only an explicit diagnostic read needs
    // the immutable application snapshot; ordinary host updates bind the source without copying.
    public WorldActiveTradeState State
    {
        get
        {
            if (!disposed && ObservationSession != null)
                state = ObservationSession.ActiveTradeState ?? WorldActiveTradeState.Empty;
            return state;
        }
    }
    public IReadOnlyList<(byte Tab, int Slot, int Count)> OfferItems => ProjectOffers(owner.OfferForIndependentRuntime);
    public string HoverTooltip => string.Empty;
    public bool InitialEmptyOfferSupported
    {
        get
        {
            WorldActiveTradeState current = State;
            return current.MyItems.Count == 0 && current.OtherItems.Count == 0;
        }
    }
    public string LastFusedConfirmationMessage => owner.LastFusedMessageForIndependentRuntime;
    public bool FusedConfirmationVisible => SysMsgBox.Visible && SysMsgBox.I != null &&
        string.Equals(SysMsgBox.I.MessageForIndependentRuntime, LastFusedConfirmationMessage, StringComparison.Ordinal);
    public static bool TryCreate(ReferenceResourceLoader resources, ReferenceFirstWorldHudResources hud,
        ReferenceFirstWorldInventoryDialog inventory, ReferenceSystemMessageBox messageBox, out ReferenceFirstWorldDealDialog owner)
    {
        owner = null;
        if (resources == null || inventory == null) return false;
        owner = new ReferenceFirstWorldDealDialog(inventory); return true;
    }
    public static string BuildFusedConfirmationMessage(
        string displayName,
        string fusedDescription,
        string action)
    {
        return "【" + (displayName ?? string.Empty) +
            FusedMessageDescriptionPrefix + (fusedDescription ?? string.Empty) +
            FusedMessageActionPrefix + (action ?? string.Empty) +
            FusedMessageWarning + (action ?? string.Empty) + "吗？";
    }

    public bool ApplyState(WorldActiveTradeState next, IReadOnlyDictionary<int, WorldRemotePlayerState> remotePlayers)
    {
        ThrowIfDisposed(); if (next == null) return false;
        // Receipt owns SocialClient and Open/Hide. A world rebuild only observes this mirror.
        observationSession = null;
        state = next;
        return true;
    }
    public void Open() { ThrowIfDisposed(); DealDlg.Open(); }
    public void Hide() { ThrowIfDisposed(); DealDlg.Hide(); }
    public bool RequestCancel()
    {
        ThrowIfDisposed(); if (!Visible) return false;
        DealDlg.Toggle(); return true;
    }
    public bool MouseOver(Vector2 topOriginPointer, float scale) => !disposed && owner.ContainsPointer(topOriginPointer);
    private static List<(byte Tab, int Slot, int Count)> ProjectOffers(List<(byte, int, int)> offers)
    {
        var result = new List<(byte, int, int)>(offers.Count);
        foreach (var offer in offers) result.Add(offer);
        return result;
    }
    public void Dispose() => Dispose(detachOnly: false);

    internal void Dispose(bool detachOnly)
    {
        if (disposed) return;
        disposed = true;
        // Production world hosts only borrow this independently owned native component.
        // Their root release/purge must not hide it, clear its offers or reset trade statics.
        if (!detachOnly) owner.ReleaseForIndependentRuntime();
        inventory = null;
        observationSession = null;
        if (!detachOnly) SocialClient.ResetTrade();
    }
    private void ThrowIfDisposed() { if (disposed) throw new ObjectDisposedException(nameof(ReferenceFirstWorldDealDialog)); }
}
}
