using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// A map borrows the original singleton. It owns no textures, rows, visibility or callback slot.
public sealed partial class ReferenceFirstWorldNpcDialog : IDisposable
{
    public const string PortraitPrefix = "res/dlgpic/";
    public const string PortraitSuffix = ".gsa";
    public const string WindowPath = "res/uires/sysdlg/npcdlgbk.gsa";
    public const string ClosePath = "res/uires/common/miniclose.gsa";
    public const string HelpPath = "res/uires/gameui/helpicon.gsa";
    public const string OptionBackgroundPath = "res/uires/sysdlg/optionbk.gsa";
    public const string SelectionPath = "res/uires/sysdlg/selbk.gsa";
    public const string UpLeftPath = "res/uires/patch/patchupleft.gsa";
    public const string UpMiddlePath = "res/uires/patch/patchupmiddle.gsa";
    public const string UpRightPath = "res/uires/patch/patchupright.gsa";
    public const string LeftMiddlePath = "res/uires/patch/patchleftmiddle.gsa";
    public const string RightMiddlePath = "res/uires/patch/patchrightmiddle.gsa";
    public const string DownLeftPath = "res/uires/patch/patchdownleft.gsa";
    public const string DownMiddlePath = "res/uires/patch/patchdownmiddle.gsa";
    public const string DownRightPath = "res/uires/patch/patchdownright.gsa";
    public const string FallbackGreeting = "有什么可以帮你的吗？";
    public const string LeaveLabel = "离开";
    public const string AvailablePrefix = "领取任务：";
    public const string ActivePrefix = "任务进行中：";
    public const string CompletePrefix = "完成任务：";
    public const string ContinuePrefix = "继续任务：";
    public const string LockedPrefix = "未解锁：";
    public const string AcceptLabel = "接受任务";
    public const string RewardPrefix = "\n奖励：";
    public const string ContinueLabel = "继续";
    public const string IncompleteSuffix = "\n（任务尚未完成）";
    public const string ClaimRewardLabel = "领取奖励";
    public const float WindowWidth = 451f;
    public const float WindowHeight = 160f;
    public const float OptionMinimumHeight = 80f;
    public const float OptionRowHeight = 16f;
    private readonly DialogUI owner;
    private ReferenceFirstWorldNpcDialog(DialogUI value) { owner = value; }
    public bool Ready => owner.Ready;
    public bool Visible => owner.Visible;
    public bool LastOpenWasFurnace => owner.LastOpenWasFurnace;
    public string NpcName => owner.NpcName;
    public string Greeting => owner.Greeting;
    public string PortraitPath => owner.PortraitPath;
    public int CurrentNpcEntityId => owner.CurrentNpcEntityId;
    public int CurrentSellList => owner.CurrentSellList;
    public int CurrentTransportList => owner.CurrentTransportList;
    public bool InSubView => owner.InSubView;
    public bool LastHelpIntent => owner.LastHelpIntent;
    public WorldNpcTaskOfferState LastOfferIntent => owner.LastOfferIntent;
    public ReferenceNpcTaskMutationIntent LastTaskMutationIntent => owner.LastTaskMutationIntent;
    public Texture2D Portrait => owner.Portrait;
    public IReadOnlyList<ReferenceNpcDialogOption> Options => owner.Options;
    public static bool TryCreate(ReferenceResourceLoader resources, out ReferenceFirstWorldNpcDialog owner) =>
        TryCreate(resources, null, null, null, null, out owner);
    public static bool TryCreate(ReferenceResourceLoader resources, Action<int, bool> shop,
        Action<int> transport, Action warehouse, out ReferenceFirstWorldNpcDialog owner,
        Action<string> sendMenuCommand = null) =>
        TryCreate(resources, shop, transport, warehouse, null, out owner, sendMenuCommand);
    public static bool TryCreate(ReferenceResourceLoader resources, Action<int, bool> shop,
        Action<int> transport, Action warehouse, Action familyStash,
        out ReferenceFirstWorldNpcDialog owner, Action<string> sendMenuCommand = null)
    {
        owner = null;
        if (resources == null || DialogUI.I == null) return false;
        ReferenceNpcGreetingCatalog greetings;
        if (!ReferenceNpcGreetingCatalog.TryCreate(resources, out greetings)) return false;
        owner = new ReferenceFirstWorldNpcDialog(DialogUI.I);
        DialogUI.I.BindHostForIndependentRuntime(owner, greetings,
            shop, transport, warehouse, familyStash, sendMenuCommand);
        return true;
    }
    internal void BindTaskRequestsForIndependentRuntime(Action<string> offers,
        Action<ReferenceNpcTaskMutationIntent> mutation) =>
        owner.BindTaskRequestsForIndependentRuntime(this, offers, mutation);
    public bool Open(WorldNpcState npc) => owner.Open(npc);
    public bool ApplyOffers(WorldNpcTaskOffersState state) => owner.ApplyOffers(state);
    // Compatibility observation only. Receipts already publish TaskClient.Active; the native
    // menu/detail reads that live authority, including while this map borrower is detached.
    public bool ApplyTaskLog(WorldTaskLogState state) => Ready && state != null;
    public bool ShowOffer(WorldNpcTaskOfferState offer) => owner.ShowOffer(offer);
    public bool ShowSay(string text) => owner.ShowSay(text);
    public void Close() => owner.Close();
    // Compatibility-only host method. Unity drives the independent owner exactly once.
    public bool Draw(float scale) => true;
    public void Dispose() { if (owner != null) owner.DetachHostForIndependentRuntime(this); }
    public static string PortraitLogicalPath(string name) => DialogUI.PortraitLogicalPath(name);
    public Rect PortraitRect(Rect window, float scale) => owner.PortraitRect(window, scale);
    public Rect WindowRect(int width, int height, float scale) => owner.WindowRect(width, height, scale);
    public Rect OptionRect(int width, int height, float scale) => owner.OptionRect(width, height, scale);
    public bool MouseOver(Vector2 pointer, int width, int height, float scale) => owner.MouseOver(pointer, width, height, scale);
    internal bool HandlePrimaryDown(Vector2 pointer, int width, int height, float scale) => owner.HandlePrimaryDown(pointer, width, height, scale);
    internal Rect OptionDecorationRect(Rect box, float scale) => owner.OptionDecorationRect(box, scale);
    internal static Rect OptionInteriorRect(Rect box, float scale) => DialogUI.OptionInteriorRect(box, scale);
}

public sealed class ReferenceNpcDialogOption
{
    private DialogUI owner;

    // Keep the existing observation row as the real Action target. This bridge is
    // bound only while building a row, never during DialogUI construction.
    internal Action BindTo(DialogUI value)
    {
        owner = value;
        return Dispatch;
    }

    private void Dispatch() => owner.ActivateOption(this);

    public ReferenceNpcDialogOption(
        string label,
        WorldNpcTaskOfferState offer,
        bool leaves,
        ReferenceNpcTaskMutationKind mutationKind = ReferenceNpcTaskMutationKind.None)
    {
        Label = label ?? string.Empty;
        Offer = offer;
        Leaves = leaves;
        MutationKind = mutationKind;
    }

    public ReferenceNpcDialogOption(string label, Action action)
    {
        Label = label ?? string.Empty;
        Action = action;
    }

    // The snapshot belongs to the readonly observation surface. Native task rows
    // execute their captured mutable Offer through this separate, internal action.
    internal ReferenceNpcDialogOption(
        string label,
        WorldNpcTaskOfferState offer,
        bool leaves,
        ReferenceNpcTaskMutationKind mutationKind,
        Action nativeAction)
        : this(label, offer, leaves, mutationKind)
    {
        NativeAction = nativeAction;
    }

    internal Action NativeAction { get; }

    public string Label { get; }

    public WorldNpcTaskOfferState Offer { get; }

    public bool Leaves { get; }

    public ReferenceNpcTaskMutationKind MutationKind { get; }

    public Action Action { get; }

}

public enum ReferenceNpcTaskMutationKind
{
    None,
    Accept,
    Complete
}

public sealed class ReferenceNpcTaskMutationIntent
{
    public ReferenceNpcTaskMutationIntent(
        ReferenceNpcTaskMutationKind kind,
        int taskId)
    {
        if (kind == ReferenceNpcTaskMutationKind.None)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        // Minimal consumer adaptation to the restored DTO: official TaskClient.Accept /
        // TaskClient.Complete (0x06000e6d / 0x06000e6f) forward the row id they were given with
        // no positive test, and the row id reaches this intent unchanged from the Buf.I32 the
        // 0x8028 case read. Validating it here would reject the click of a row the official
        // client both renders and sends. The server remains the sole authority on the result.
        Kind = kind;
        TaskId = taskId;
    }

    public ReferenceNpcTaskMutationKind Kind { get; }

    public int TaskId { get; }
}

}
