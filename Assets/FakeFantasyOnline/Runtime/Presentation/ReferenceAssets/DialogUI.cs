using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of the normal official 1.5.8 NpcInteract/TaskClient/DialogUI open path.
/// </summary>
/// <remarks>
/// This owner opens immediately from the selected NPC snapshot, then accepts only matching
/// server-authored task-offer replacements. Offer detail is local source-recovered presentation;
/// accept/complete buttons emit typed intents and never fabricate their server result.
/// </remarks>
public sealed partial class DialogUI : MonoBehaviour
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

    private static Texture2D darkBg;

    private ReferenceNpcGreetingCatalog greetings;
    private Texture2D win;
    private Texture2D closeTex;
    private Texture2D helpTex;
    private Texture2D brushTex;
    private Texture2D selTex;
    private Texture2D pUL;
    private Texture2D pUM;
    private Texture2D pUR;
    private Texture2D pLM;
    private Texture2D pRM;
    private Texture2D pDL;
    private Texture2D pDM;
    private Texture2D pDR;
    // 0600022b stores both empty strings before allocating option storage.
    private string npcName = string.Empty;
    private string greeting = string.Empty;
    private readonly List<string> optLabels = new List<string>();
    private readonly List<Action> optActs = new List<Action>();
    private Texture2D pic;
    public static DialogUI I;
    private float bfs = 1f;
    private object hostBinding;
    private Action<string> requestTaskOffers;

    // The legacy mutation argument is retained for the borrower API; native row actions
    // dispatch through TaskClient and leave LastTaskMutationIntent for observation only.
    internal void BindTaskRequestsForIndependentRuntime(object binding,
        Action<string> offers, Action<ReferenceNpcTaskMutationIntent> mutation)
    {
        if (!ReferenceEquals(hostBinding, binding)) return;
        requestTaskOffers = offers;
    }

    // 06000210: publication is deliberately incremental. A failed load retains the
    // singleton and every field already assigned; missing loader still captures CnFont.
    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        ResLoader loader = ResLoader.I;
        if (loader != null)
        {
            win = loader.LoadGsa(WindowPath);
            closeTex = loader.LoadGsa(ClosePath);
            helpTex = loader.LoadGsa(HelpPath);
            brushTex = loader.LoadGsa(OptionBackgroundPath);
            selTex = loader.LoadGsa(SelectionPath);
            pUL = loader.LoadGsa(UpLeftPath);
            pUM = loader.LoadGsa(UpMiddlePath);
            pUR = loader.LoadGsa(UpRightPath);
            pLM = loader.LoadGsa(LeftMiddlePath);
            pRM = loader.LoadGsa(RightMiddlePath);
            pDL = loader.LoadGsa(DownLeftPath);
            pDM = loader.LoadGsa(DownMiddlePath);
            pDR = loader.LoadGsa(DownRightPath);
        }
        fallbackFont = NpcChatter.CnFont();
    }

    // 06000220/221: independent live singleton reads in both predicate and action.
    public static bool VisibleS => I != null && I.Visible;
    public static void CloseS() { if (I != null) I.Visible = false; }

    // 06000222 is a show-state predicate. Native GameHud inlines this same field read;
    // the host's geometric MouseOver below serves only its pointer-window routing.
    public bool MouseOverBox() => show;

    internal void BindHostForIndependentRuntime(object binding,
        ReferenceNpcGreetingCatalog valueGreetings,
        Action<int, bool> shop, Action<int> transport, Action warehouse,
        Action familyStash, Action<string> menuCommand)
    {
        hostBinding = binding;
        greetings = valueGreetings;
    }

    internal void DetachHostForIndependentRuntime(object binding)
    {
        if (!ReferenceEquals(hostBinding, binding)) return;
        hostBinding = null;
        requestTaskOffers = null;
    }

    // Original 06000211 receiver boundary. The existing snapshot entry below is only
    // an observation/compatibility adapter; live NPC fields are read in native order.
    public void Show(NpcInteract npc)
    {
        Open(null, npc);
        if (!LastOpenWasFurnace)
        {
            // Same existing Application writer/receipt observation while hosted; the native
            // global port remains the receiver when no independent map adapter is present.
            if (requestTaskOffers != null) requestTaskOffers(NpcName);
            else TaskClient.RequestOffers(NpcName);
        }
        GameBootstrap.I?.Window?.ObserveNpcDialogShownForIndependentRuntime(npc);
    }

    internal void ObserveNpcEntityForIndependentRuntime(int entityId) => CurrentNpcEntityId = entityId;

    public bool Ready => this != null;

    private bool show;
    public bool Visible { get => show; private set => show = value; }

    public bool LastOpenWasFurnace { get; private set; }

    public string NpcName { get => npcName; private set => npcName = value; }

    public string Greeting { get => greeting; private set => greeting = value; }

    public string PortraitPath { get; private set; } = string.Empty;

    public Texture2D Portrait => pic;

    public int CurrentNpcEntityId { get; private set; }

    private int curSell;
    public int CurrentSellList { get => curSell; private set => curSell = value; }

    private int curTrans;
    public int CurrentTransportList { get => curTrans; private set => curTrans = value; }

    private bool inSubView;
    public bool InSubView { get => inSubView; private set => inSubView = value; }

    public bool LastHelpIntent { get; private set; }

    public WorldNpcTaskOfferState LastOfferIntent { get; private set; }

    private ReferenceNpcTaskMutationIntent lastTaskMutationIntent;
    private ReferenceNpcTaskMutationKind lastTaskMutationKind;
    private int lastTaskMutationTaskId;

    // Native requests record only values. Allocate the immutable observation on an
    // explicit read, then keep its identity until another action or reset replaces it.
    public ReferenceNpcTaskMutationIntent LastTaskMutationIntent
    {
        get
        {
            if (lastTaskMutationIntent == null &&
                lastTaskMutationKind != ReferenceNpcTaskMutationKind.None)
            {
                lastTaskMutationIntent = new ReferenceNpcTaskMutationIntent(
                    lastTaskMutationKind, lastTaskMutationTaskId);
            }

            return lastTaskMutationIntent;
        }
        private set
        {
            // Existing producers can supply an already-created observation; retain
            // that exact object. Null also cancels any native values not yet read.
            lastTaskMutationIntent = value;
            lastTaskMutationKind = ReferenceNpcTaskMutationKind.None;
            lastTaskMutationTaskId = 0;
        }
    }

    private OptionView optionView;
    public IReadOnlyList<ReferenceNpcDialogOption> Options =>
        optionView ?? (optionView = new OptionView(this));

    /// <summary>
    /// Applies NpcInteract.Show: construct the local greeting/menu first, then let the caller send
    /// TaskClient.RequestOffers. No server acknowledgement is fabricated here.
    /// </summary>
    public bool Open(WorldNpcState npc) => Open(npc, null);

    private bool Open(WorldNpcState npc, NpcInteract originalReceiver)
    {
        LastOpenWasFurnace = false;
        if (!Ready || (npc == null && originalReceiver == null)) return false;

        // Official DialogUI.Show (0x06000211, arm64 0x365584) stores the name with a plain
        // null-to-empty substitution - arm64 0x3656a4-0x3656b8 loads NpcInteract+0x20 and falls
        // back to metadata_usage_10335_kind5, which the frozen atlas resolves to the empty string
        // literal - and only then compares it, so it never refuses to open: neither DialogUI.Show
        // nor NpcInteract.Show (0x06000926, arm64 0x2849d0, which only tests DialogUI.I before
        // tail-calling Show) contains an IsNullOrWhiteSpace gate. A blank or whitespace
        // DisplayName is therefore a legal name that still opens; the empty string also keeps
        // NpcName non-null for the later Contains checks in BuildMenu.
        NpcName = (originalReceiver != null ? originalReceiver.npcName : npc.DisplayName) ?? string.Empty;

        // DialogUI.Show 06000211 hides the ordinary dialog and returns before task offers.
        switch (NpcName)
        {
            case "合成炉":
                Visible = false; LastOpenWasFurnace = true; ComposeDlg.Open(0); return true;
            case "加工炉":
                Visible = false; LastOpenWasFurnace = true; ComposeDlg.Open(1); return true;
            case "镶嵌炉":
                Visible = false; LastOpenWasFurnace = true; SculpDlg.Open("镶嵌炉", "镶嵌"); return true;
            case "打孔炉":
                Visible = false; LastOpenWasFurnace = true; SculpDlg.Open("打孔炉", "打孔"); return true;
        }

        // WBD-012-R1 order proof. Official DialogUI.Show loads the portrait before any greeting
        // work and copies curSell/curTrans only after the QuestLineFor override:
        // arm64 06000211.c:99-124 guards NpcInteract+0x28 with String.IsNullOrEmpty, then builds
        // "res/dlgpic/" + pic + ".gsa" through String.Concat and ResLoader.LoadGsa and writes
        // this+0xc0; the greeting pair starts at :129 (NpcChatter.RoleOf -> OwnGreeting ->
        // GetGreeting, with the own!=null branch writing the literal 17662 directly); QuestLineFor
        // (0x06000214) overrides this+0xb8 at :149-154; only then does :155 copy the 8 bytes at
        // param_2+0x38 into this+0xdc, followed by BuildMenu at :156 and show=1 at :157.
        // x86_64 06000211.c:96-150 repeats the same four steps in the same order (pic write at
        // :119, greeting from :124, QuestLineFor at :143-148, curSell/curTrans copy at :149).
        // Keeping this order means a portrait load that throws or yields null leaves the greeting
        // and the sell/transport pair exactly as official leaves them, instead of pre-writing them.
        PortraitPath = PortraitLogicalPath(originalReceiver != null ? originalReceiver.portrait : npc.Portrait);
        pic = string.IsNullOrEmpty(PortraitPath) || ResLoader.I == null
            ? null
            : ResLoader.I.LoadGsa(PortraitPath);
        if (originalReceiver != null)
        {
            string role = NpcChatter.RoleOf(originalReceiver);
            Greeting = NpcChatter.OwnGreeting(NpcName) != null
                ? FallbackGreeting : NpcChatter.GetGreeting(NpcName, role);
            if (string.IsNullOrEmpty(Greeting)) Greeting = FallbackGreeting;
        }
        else Greeting = greetings.DialogGreeting(npc);
        string questLine = QuestLineFor(NpcName);
        if (!string.IsNullOrEmpty(questLine)) Greeting = questLine;
        CurrentNpcEntityId = npc == null ? 0 : npc.EntityId;
        CurrentSellList = originalReceiver != null ? originalReceiver.sell : npc.SellList;
        CurrentTransportList = originalReceiver != null ? originalReceiver.trans : npc.TransportList;
        InSubView = false;
        LastHelpIntent = false;
        LastOfferIntent = null;
        LastTaskMutationIntent = null;
        // No offer clear here. Official DialogUI.Show (0x06000211) ends with BuildMenu at :156 and
        // show=1 at :157 and never touches TaskClient.Offers, so the rows already published for
        // this NPC name are on screen from the first frame of the new dialog and are replaced only
        // when a later 0x8028 arrives.
        BuildMenu();
        Visible = true;
        // Official DialogUI.Show (0x06000211) overwrites the single TaskClient.OnOffersChanged
        // slot here: arm64 06000211.c:158-165 builds Action<string>(DialogUI.OnTaskOffers) with
        // metadata_usage_07294_kind3, runs the TaskClient cctor gate and stores it into
        // [TaskClient statics]+0x28 with a write barrier; x86_64 06000211.c:150-156 is the same
        // three steps. It is a plain store, so a second Show replaces the previous handler
        // instead of adding to it, and the four furnace branches above return before it, so they
        // never own the slot. Assigning - never += - means one replacement reaches exactly the
        // dialog that opened last, and a rebuilt dialog cannot receive one replacement twice.
        // Show forwards the request immediately after this returned menu/state sequence.
        TaskClient.OnOffersChanged = OnTaskOffers;
        return true;
    }

    public static string PortraitLogicalPath(string portraitName)
    {
        return string.IsNullOrEmpty(portraitName)
            ? string.Empty
            : PortraitPrefix + portraitName + PortraitSuffix;
    }

    public Rect PortraitRect(Rect windowRect, float scale)
    {
        if (!pic || !(scale > 0f) || !IsFinite(scale))
        {
            return new Rect();
        }

        float width = TexScale.W(pic) * scale;
        float height = TexScale.H(pic) * scale;
        return new Rect(
            windowRect.x + (WindowWidth * scale - width) * 0.5f,
            windowRect.y - height,
            width,
            height);
    }

    /// <summary>
    /// Feeds one decoded 0x8028 replacement to the official owner, which is
    /// <c>TaskClient</c> and not this dialog.
    /// </summary>
    /// <remarks>
    /// Official 0x8028 tail-calls <c>TaskClient.SetOffers</c> (0x06000e5e): the NPC name is
    /// stored, the offer list is cleared and refilled and <c>OnOffersChanged</c> is fired with
    /// that same name. The replacement therefore happens for every packet whether or not this
    /// dialog is open and whether or not the name matches, which is what
    /// <see cref="OnTaskOffers"/> then filters. Handing the state to
    /// <c>TaskClient.SetWorldOffers</c> keeps one owner for both the receive path and this entry,
    /// so no second cache can disagree with the official statics.
    /// </remarks>
    public bool ApplyOffers(WorldNpcTaskOffersState state)
    {
        if (!Ready || state == null)
        {
            return false;
        }

        TaskClient.SetWorldOffers(state.NpcName, state.Offers);
        return true;
    }

    /// <summary>
    /// Official DialogUI.OnTaskOffers (0x06000212, arm64 0x36779c / 88 bytes, x86_64 0x37a3a0 /
    /// 80 bytes): the whole body is one three-part condition and one call.
    /// </summary>
    /// <remarks>
    /// Both architectures read <c>this+0xa8</c> (Visible), compare the callback argument with
    /// <c>this+0xb0</c> (NpcName) through <c>System.String.op_Equality</c> and test
    /// <c>this+0xd8</c> (InSubView) before calling <c>BuildMenu</c> (0x06000215); anything else
    /// returns without touching the menu. Nothing is refilled here - the offers were already
    /// replaced by <c>TaskClient.SetOffers</c> before the delegate ran - and an open offer detail
    /// or ShowSay view is deliberately left alone, because <c>InSubView</c> is the third test and
    /// not a state this callback resets.
    /// </remarks>
    private void OnTaskOffers(string npc)
    {
        if (!Visible ||
            !string.Equals(npc, NpcName, StringComparison.Ordinal) ||
            InSubView)
        {
            return;
        }

        BuildMenu();
    }

    // The native menu passes its original mutable Offer to this overload. The DTO
    // below is only an observation; text and later task actions read the live offer.
    private void ShowOffer(TaskClient.Offer offer)
    {
        InSubView = true;
        LastOfferIntent = null;
        LastTaskMutationIntent = null;
        Greeting = offer.desc;
        if (!string.IsNullOrEmpty(offer.award))
        {
            Greeting += RewardPrefix + offer.award;
        }

        optLabels.Clear();
        optActs.Clear();
        WorldNpcTaskOfferState observed = new WorldNpcTaskOfferState(
            offer.id, offer.state, offer.title, offer.desc, offer.award);
        LastOfferIntent = observed;
        if (offer.state == 1)
        {
            Greeting += IncompleteSuffix;
        }
        else if (offer.state == 2)
        {
            Add(new ReferenceNpcDialogOption(
                TaskClient.IsTerminal(offer.id) ? ClaimRewardLabel : ContinueLabel,
                observed, false, ReferenceNpcTaskMutationKind.Complete,
                () =>
                {
                    int id = offer.id;
                    lastTaskMutationKind = ReferenceNpcTaskMutationKind.Complete;
                    lastTaskMutationTaskId = id;
                    lastTaskMutationIntent = null;
                    TaskClient.Complete(id);
                }));
        }
        else if (offer.state != 3)
        {
            Add(new ReferenceNpcDialogOption(
                AcceptLabel, observed, false, ReferenceNpcTaskMutationKind.Accept,
                () =>
                {
                    int id = offer.id;
                    lastTaskMutationKind = ReferenceNpcTaskMutationKind.Accept;
                    lastTaskMutationTaskId = id;
                    lastTaskMutationIntent = null;
                    TaskClient.Accept(id);
                    Close();
                }));
        }

        Add(new ReferenceNpcDialogOption(LeaveLabel, null, true));
    }

    /// <summary>
    /// Compatibility entry for an already immutable observation row.
    /// Original 0600021c does not gate on visibility or Unity liveness. It enters the
    /// subview before reading the offer, so a null offer throws after that state write.
    /// The method leaves visibility unchanged; its existing menu caller owns admission.
    /// </summary>
    public bool ShowOffer(WorldNpcTaskOfferState offer)
    {
        InSubView = true;
        LastOfferIntent = offer;
        LastTaskMutationIntent = null;
        Greeting = offer.Description;
        if (!string.IsNullOrEmpty(offer.Award))
        {
            Greeting += RewardPrefix + offer.Award;
        }

        optLabels.Clear();
        optActs.Clear();
        if (offer.State == 1)
        {
            Greeting += IncompleteSuffix;
        }
        else if (offer.State == 2)
        {
            Add(
                new ReferenceNpcDialogOption(
                    TaskClient.IsTerminal(offer.TaskId)
                        ? ClaimRewardLabel
                        : ContinueLabel,
                    offer,
                    false,
                    ReferenceNpcTaskMutationKind.Complete));
        }
        else if (offer.State != 3)
        {
            Add(
                new ReferenceNpcDialogOption(
                    AcceptLabel,
                    offer,
                    false,
                    ReferenceNpcTaskMutationKind.Accept));
        }

        Add(new ReferenceNpcDialogOption(LeaveLabel, null, true));
        return true;
    }

    /// <summary>
    /// Original 06000224 stores raw text and opens its one-row subview. Receipt callers
    /// retain their separate visible-dialog admission before invoking this body.
    /// </summary>
    public bool ShowSay(string text)
    {
        Greeting = text;
        InSubView = true;
        LastOfferIntent = null;
        LastTaskMutationIntent = null;
        optLabels.Clear();
        optActs.Clear();
        Add(new ReferenceNpcDialogOption(LeaveLabel, null, true));
        Visible = true;
        return true;
    }

    /// <summary>
    /// Official DialogUI.Close (0x0600021f): one store of 0 into <c>this+0xa8</c> and nothing
    /// else.
    /// </summary>
    /// <remarks>
    /// Both frozen bodies are that single byte store - arm64 rva 0x365a78 / 8 bytes and x86_64 rva
    /// 0x378670 / 16 bytes - so the official close is a hide, not a release: <c>InSubView</c>, the
    /// pic, the current greeting, the built option rows and the pending offer/mutation
    /// intents all survive it, which is why reopening the same NPC and the official
    /// <c>BackToMenu</c> behave the way they do. The product-only clearing that used to live here
    /// is no longer owned by an ordinary map borrower's release.
    /// In particular a task-mutation intent captured by a task-row click is no longer dropped by
    /// the hide that the map adapter performs after it has already queued the accept request.
    /// </remarks>
    public void Close()
    {
        Visible = false;
    }

    public Rect WindowRect(int screenWidth, int screenHeight, float scale)
    {
        float optionHeight = OptionHeight(scale);
        float width = WindowWidth * scale;
        float height = WindowHeight * scale;
        float x = (screenWidth - width) * 0.5f;
        float y = Mathf.Max(
            6f,
            Mathf.Min(
                screenHeight - height - optionHeight - 6f,
                190f * scale));
        return new Rect(x, y, width, height);
    }

    public Rect OptionRect(int screenWidth, int screenHeight, float scale)
    {
        Rect win = WindowRect(screenWidth, screenHeight, scale);
        return new Rect(
            win.x,
            win.y + WindowHeight * scale + 2f * scale,
            win.width,
            Mathf.Max(80f * scale, 8f * scale + (16f * scale) * optLabels.Count));
    }

    public bool MouseOver(
        Vector2 topOriginPointer,
        int screenWidth,
        int screenHeight,
        float scale)
    {
        if (!Visible || !(scale > 0f) || !IsFinite(scale))
        {
            return false;
        }

        return ContainsInclusive(WindowRect(screenWidth, screenHeight, scale), topOriginPointer) ||
               optLabels.Count > 0 && ContainsInclusive(
                   OptionRect(screenWidth, screenHeight, scale),
                   topOriginPointer);
    }

    /// <summary>
    /// Routes one top-origin primary-down through the same recovered option rows used by OnGUI.
    /// </summary>
    /// <remarks>
    /// This is an independent-runtime input adapter only. It preserves the exact hit geometry and
    /// calls the same option action as DrawOptions; it does not send a task request or author state.
    /// </remarks>
    internal bool HandlePrimaryDown(
        Vector2 topOriginPointer,
        int screenWidth,
        int screenHeight,
        float scale)
    {
        LastHelpIntent = false;
        LastOfferIntent = null;
        LastTaskMutationIntent = null;
        if (!Ready || !Visible || !(scale > 0f) || !IsFinite(scale))
        {
            return false;
        }

        Rect box = OptionRect(screenWidth, screenHeight, scale);
        for (int index = 0; index < optLabels.Count; index++)
        {
            Rect row = OptionRowRect(box, index, scale);
            if (ContainsInclusive(row, topOriginPointer))
            {
                return ActivateOption(index);
            }
        }

        return false;
    }

    // Complete original 0600022a. Unity is the sole GUI driver, also while a map borrows
    // the owner. Host readiness, binding and disposal never disable this callback.
    private void OnGUI()
    {
        GUI.depth = 5;
        if (!Visible || win == null) return;
        float scale = UIScale.Factor;
        int screenWidth = Screen.width;
        BuildStyles(scale);
        bfs = scale;
        BitmapFont.Measure(".", scale);
        float picWidth = pic != null ? TexScale.W(pic) * scale : 0f;
        float picHeight = pic != null ? TexScale.H(pic) * scale : 0f;
        float width = 451f * scale;
        float height = 160f * scale;
        float optionHeight = optLabels.Count > 0
            ? Mathf.Max(80f * scale, 8f * scale + (optLabels.Count * 16f) * scale) : 0f;
        float x = (screenWidth - width) * 0.5f;
        float y = Mathf.Max(6f, Mathf.Min(190f * scale,
            Screen.height - height - optionHeight - 6f));
        Rect panel = new Rect(x, y, width, height);
        GUI.DrawTexture(panel, win);
        if (pic != null)
            GUI.DrawTexture(new Rect(x + (width - picWidth) * 0.5f,
                y - picHeight, picWidth, picHeight), pic);
        if (BitmapFont.Ready)
            BitmapFont.DrawOutline(x + 28f * scale, y + 9f * scale,
                NpcName, NpcNameColor, scale);
        else
            OutLabel(L(panel, 28f, 9f, 372f, 12f, scale), NpcName,
                nameTextStyle, NpcNameColor, Mathf.Max(1f, scale));
        Rect close = L(panel, 425f, 5f, 20f, 19f, scale);
        if (closeTex != null) GUI.DrawTexture(close, closeTex);
        else GUI.Label(close, "X");
        float closeOffset = 420f * scale;
        if (GUI.Button(new Rect(x + closeOffset, y, width - closeOffset, 31f * scale),
            GUIContent.none, GUIStyle.none))
        {
            Visible = false;
            return;
        }
        Rect help = L(panel, 405f, 5f, 20f, 19f, scale);
        if (helpTex != null) GUI.DrawTexture(help, helpTex);
        else GUI.Label(help, "?");
        GUI.Button(L(panel, 398f, 0f, 22f, 31f, scale), GUIContent.none, GUIStyle.none);
        DrawWrapped(x + 18f * scale, y + 41f * scale, 394f * scale,
            102f * scale, Greeting, greetingTextStyle, 18f * scale);
        if (optLabels.Count == 0) return;
        float rowHeight = 16f * scale;
        Rect box = new Rect(x, y + height + 2f * scale, width,
            Mathf.Max(80f * scale, 8f * scale + rowHeight * optLabels.Count));
        DrawOptionFrame(box, scale);
        Vector2 pointer = Event.current.mousePosition;
        int hovered = -1;
        for (int index = 0; index < optLabels.Count; index++)
            if (OptionRowRect(box, index, scale).Contains(pointer)) hovered = index;
        for (int index = 0; index < optLabels.Count; index++)
        {
            Rect row = OptionRowRect(box, index, scale);
            if (hovered == index && selTex != null)
                GUI.DrawTexture(new Rect(row.x, row.y, row.width, rowHeight - scale), selTex);
            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                ActivateOption(index);
                break;
            }
            if (BitmapFont.Ready)
                BitmapFont.Draw(row.x + 4f * scale,
                    row.y + (rowHeight - 12f * scale) * 0.5f,
                    optLabels[index], Color.white, scale);
            else
                GUI.Label(new Rect(row.x + 4f * scale, row.y,
                    row.width - 8f * scale, row.height), optLabels[index], optionTextStyle);
        }
        if (brushTex != null)
            GUI.DrawTexture(OptionDecorationRect(box, scale), brushTex);
    }

    private bool ActivateOption(int index)
    {
        if (index < 0 || index >= optLabels.Count)
        {
            return false;
        }

        optActs[index]?.Invoke();
        return true;
    }

    // Row metadata is the target of the stored Action, not a third collection.
    internal bool ActivateOption(ReferenceNpcDialogOption option)
    {
        if (option.Leaves)
        {
            Close();
            return true;
        }

        if (option.NativeAction != null)
        {
            option.NativeAction();
            return true;
        }

        if (option.MutationKind != ReferenceNpcTaskMutationKind.None)
        {
            ReferenceNpcTaskMutationIntent intent =
                new ReferenceNpcTaskMutationIntent(
                    option.MutationKind,
                    option.Offer.TaskId);
            // Official 1.5.8 keeps DialogUI visible for Complete so its later authoritative
            // feedback and refreshed offer list remain on this surface. Both callbacks use
            // the live native task owner; the intent is observation-only.
            LastTaskMutationIntent = intent;
            if (intent.Kind == ReferenceNpcTaskMutationKind.Accept)
            {
                TaskClient.Accept(intent.TaskId);
                Close();
            }
            else if (intent.Kind == ReferenceNpcTaskMutationKind.Complete)
                TaskClient.Complete(intent.TaskId);
            return true;
        }

        if (option.Action != null)
        {
            option.Action();
            return true;
        }

        return option.Offer == null || ShowOffer(option.Offer);
    }

    // Official 1.6.0 06000221: hide first, then read the current singleton and this receiver's list.
    // A raw-null NetClient.I returns after hiding; no map owner or cached state is consulted.
    private void ShowTransferMenu()
    {
        show = false;
        NetClient.I?.OpenTeleportForIndependentRuntime(curTrans);
    }

    private void BuildMenu()
    {
        InSubView = false;
        optLabels.Clear();
        optActs.Clear();
        if (BuildTravelMenu()) return;
        if (NpcName == "飞升使者")
            Add(new ReferenceNpcDialogOption("飞升修炼(用经验学高阶技能)", ShowAscendMenu));
        if (CurrentSellList > 0)
        {
            int sellList = CurrentSellList;
            Add(new ReferenceNpcDialogOption(
                "购买物品",
                () =>
                {
                    Close();
                    ShopDlg.Open(sellList, false);
                }));
            Add(new ReferenceNpcDialogOption(
                "出售物品",
                () =>
                {
                    Close();
                    ShopDlg.Open(sellList, true);
                }));
        }
        if (curTrans > 0)
        {
            Add(new ReferenceNpcDialogOption("传送", ShowTransferMenu));
        }
        AddDungeonEntryOptions();
        // Official callbacks 06000230/231/232: family storage has its own
        // informational row; personal storage toggles the surviving native owner.
        if (NpcName.Contains("家族仓库"))
        {
            Add(new ReferenceNpcDialogOption("存取物品", () =>
            {
                Visible = false;
                ChatUI.Add("系统", string.IsNullOrEmpty(SocialClient.FamilyName)
                    ? "你还没有加入家族。" : "家族仓库暂未开放。",
                    new Color(1f, 0.47058824f, 0.47058824f, 1f));
            }));
        }
        else if (NpcName.Contains("仓库") || NpcName.Contains("仙库"))
        {
            Add(new ReferenceNpcDialogOption("存取物品", () =>
            {
                Visible = false;
                StoreDlg.Toggle();
            }));
            Add(new ReferenceNpcDialogOption("扩展仓库(购买)", () =>
            {
                Visible = false;
                SysMsgBox.Show("确定购买一页仓库扩展?\n费用会直接从身上扣除。",
                    () => NetClient.I?.BuyWarehouseForIndependentRuntime());
            }));
        }
        // Official DialogUI.BuildMenu 0x06000215 and callback 0x06000233.
        // OpenLifeLearn owns tab selTex, learning mode and the authoritative refresh.
        if (NpcName.Contains("巧匠") || NpcName.Contains("左慈"))
        {
            Add(new ReferenceNpcDialogOption("学习生活技能", () =>
            {
                Visible = false;
                Win05.OpenLifeLearn();
            }));
            if (EndgamePanel.WorldEntryAvailable)
            {
                Add(new ReferenceNpcDialogOption("星篆 · 镶嵌取出", () => OpenEndgameView(EndgameModule.Runes)));
                Add(new ReferenceNpcDialogOption("周天鼎 · 问鼎炼化", () => OpenEndgameView(EndgameModule.Cube)));
            }
        }
        // Official 06000234: this separate family-stash row follows life learning.
        if (NpcName.Contains("家族仓库"))
        {
            Add(new ReferenceNpcDialogOption("打开家族仓库", () =>
            {
                Visible = false;
                FamilyStashDlg.Open();
            }));
        }
        if (NpcName.Contains("驯兽师"))
        {
            Add(new ReferenceNpcDialogOption("宠物觉醒", () =>
            {
                Visible = false;
                PetArousalDlg.Open();
            }));
        }
        if (NpcName.Contains("虚拟财产保护") || NpcName.Contains("财产安全"))
        {
            Add(new ReferenceNpcDialogOption("修改密码", () =>
            { Visible = false; AccountSecDlg.Open(1); }));
            Add(new ReferenceNpcDialogOption(AccountSec.HasSecCode ? "修改安全码" : "设置安全码(强烈建议)", () =>
            { Visible = false; AccountSecDlg.Open(2); }));
            Add(new ReferenceNpcDialogOption("什么是财产安全锁?", () =>
            {
                Visible = false;
                ChatUI.Add("系统", "给物品加锁(Shift+右键)后, 它不能卖店/交易/摆摊/邮寄/丢弃; 解锁要输账号【安全码】。安全码同时是找回密码的唯一凭证 —— 别和登录密码设成一样, 也别告诉任何人。",
                    new Color(150f / 255f, 215f / 255f, 1f, 1f));
            }));
        }
        AddHairSalonOptions();
        AddActivityOptions();
        // Official DialogUI.BuildMenu trial branches precede ordinary task offers.
        if (NpcName.Contains("试炼小仙"))
        {
            AddTrialOption("领取试炼任务", "trialtask");
            AddTrialOption("交任务·领取试炼奖励", "trialclaim");
            if (GameBootstrap.CurMapId == 37)
                AddTrialOption("传送到妖魔结界守卫(5银)", "trialgo");
        }
        if (NpcName.Contains("妖魔结界守卫") || NpcName.Contains("结界使"))
        {
            AddTrialOption("进入妖魔结界(试炼)", "trial");
            if (EndgamePanel.WorldEntryAvailable)
                Add(new ReferenceNpcDialogOption("演劫洞天 · 查看符钥与开境", () => OpenEndgameView(EndgameModule.Rift)));
        }
        if (NpcName.Contains("试炼大仙"))
            AddTrialOption("交任务·领取试炼奖励", "trialclaim");
        if (EndgamePanel.WorldEntryAvailable &&
            (NpcName.Contains("试炼小仙") || NpcName.Contains("试炼大仙")))
            Add(new ReferenceNpcDialogOption("符钥何用？", () => ShowSay(
                "若试炼奖励中领得演劫符钥，可去寻妖魔结界守卫，选择“演劫洞天”查看符钥与开境。组队时请在守卫附近会合；能否开境、耗费与所得，以服务器确认为准。")));

        AddRiftExitOptions();
        AddDungeonExitAndHardOptions();
        AddTianshenOptions();
        // Official DialogUI.BuildMenu (0x06000215) task section, arm64 06000215.c:578-647 and
        // x86_64 in the same shape: the rows are enumerated from the TaskClient statics and only
        // after System.String.op_Equality proves TaskClient.OfferNpc (static +0x10) equals
        // this+0xb0 (NpcName). A null Offers list throws instead of being skipped, and the row
        // label is String.Concat(prefix, offer.title) with the prefix chosen by the row state -
        // 18256 "领取任务：" for a plain row, 17022 "任务进行中：" for state 1, "完成任务："/"继续
        // 任务：" (17304/17921) for state 2 depending on TaskClient.IsTerminal and 17675
        // "未解锁：" for state 3. A reply for another NPC therefore leaves this section empty
        // rather than showing the previously cached rows.
        if (string.Equals(TaskClient.OfferNpcName, NpcName, StringComparison.Ordinal))
        {
            List<TaskClient.Offer> offers = TaskClient.Offers;
            for (int index = 0; index < offers.Count; index++)
            {
                TaskClient.Offer offer = offers[index];
                WorldNpcTaskOfferState row = new WorldNpcTaskOfferState(
                    offer.id,
                    offer.state,
                    offer.title,
                    offer.desc,
                    offer.award);
                Add(new ReferenceNpcDialogOption(
                    PrefixFor(row) + row.Title,
                    row,
                    false,
                    ReferenceNpcTaskMutationKind.None,
                    () => ShowOffer(offer)));
            }
        }
        if (optLabels.Count > 0) Add(new ReferenceNpcDialogOption(LeaveLabel, null, true));
    }

    // These rows only select a view. The existing NPC talk path and server still own
    // location, trial rewards, key spending and every crafting/rift authorization.
    private void OpenEndgameView(EndgameModule module)
    {
        bool opened = module == EndgameModule.Rift ? EndgamePanel.TryOpenRiftFromWorld() :
            module == EndgameModule.Runes ? EndgamePanel.TryOpenRunesFromWorld() : EndgamePanel.TryOpenCubeFromWorld();
        if (opened) Close();
        else ShowSay("当前功能尚未开放，请进入世界并等待服务器开放后再试。");
    }

    private void AddTrialOption(string label, string command)
    {
        Add(new ReferenceNpcDialogOption(label, () =>
        {
            // The native callbacks only hide this dialog before GmCmd; they do not
            // clear the pic/offers or synthesize task/reward/transport results.
            Visible = false;
            NetClient.I?.GmCmdForIndependentRuntime(command);
        }));
    }

    private static Rect OptionRowRect(Rect box, int index, float scale)
    {
        return new Rect(
            box.x + 34f * scale,
            box.y + 4f * scale + index * (OptionRowHeight * scale),
            383f * scale,
            OptionRowHeight * scale);
    }

    internal Rect OptionDecorationRect(Rect box, float scale)
    {
        return new Rect(
            box.x + (scale + scale),
            box.y + (scale + scale),
            GsaDecoder.W(brushTex) * scale,
            GsaDecoder.H(brushTex) * scale);
    }

    internal static Rect OptionInteriorRect(Rect box, float scale)
    {
        return new Rect(
            box.x + (scale + scale),
            box.y + (scale + scale),
            box.width - 4f * scale,
            box.height - 4f * scale);
    }

    // 06000225: each resource is read/guarded only at its native draw boundary.
    private void DrawOptionFrame(Rect box, float scale)
    {
        OptionFrameFill();
        GUI.DrawTexture(OptionInteriorRect(box, scale), darkBg);
        float corner = 11f * scale;
        if (pUM != null)
            GUI.DrawTexture(new Rect(box.x + corner, box.y,
                box.width - (corner + corner), GsaDecoder.H(pUM) * scale), pUM);
        if (pDM != null)
            GUI.DrawTexture(new Rect(box.x + corner,
                box.y + box.height - GsaDecoder.H(pDM) * scale,
                box.width - (corner + corner), GsaDecoder.H(pDM) * scale), pDM);
        if (pLM != null)
            GUI.DrawTexture(new Rect(box.x, box.y + corner,
                GsaDecoder.W(pLM) * scale, box.height - (corner + corner)), pLM);
        if (pRM != null)
            GUI.DrawTexture(new Rect(box.x + box.width - GsaDecoder.W(pRM) * scale,
                box.y + corner, GsaDecoder.W(pRM) * scale,
                box.height - (corner + corner)), pRM);
        if (pUL != null)
            GUI.DrawTexture(new Rect(box.x, box.y, corner, corner), pUL);
        if (pUR != null)
            GUI.DrawTexture(new Rect(box.x + box.width - corner, box.y, corner, corner), pUR);
        if (pDL != null)
            GUI.DrawTexture(new Rect(box.x, box.y + box.height - corner, corner, corner), pDL);
        if (pDR != null)
            GUI.DrawTexture(new Rect(box.x + box.width - corner,
                box.y + box.height - corner, corner, corner), pDR);
    }

    private static Texture2D OptionFrameFill()
    {
        if (darkBg)
        {
            return darkBg;
        }

        darkBg = new Texture2D(1, 1);
        darkBg.SetPixel(0, 0, OptionFrameColor);
        darkBg.Apply();
        return darkBg;
    }

    private float OptionHeight(float scale)
    {
        return optLabels.Count < 1
            ? 0f
            : Mathf.Max(
                OptionMinimumHeight * scale,
                8f * scale + (optLabels.Count * OptionRowHeight) * scale);
    }

    private string PrefixFor(WorldNpcTaskOfferState offer)
    {
        byte state = offer.State;
        if (state == 1)
        {
            return ActivePrefix;
        }

        if (state == 2)
        {
            return TaskClient.IsTerminal(offer.TaskId) ? CompletePrefix : ContinuePrefix;
        }

        if (state == 3)
        {
            return LockedPrefix;
        }

        return AvailablePrefix;
    }

    private static Rect L(
        Rect windowRect,
        float x,
        float y,
        float width,
        float height,
        float scale)
    {
        return new Rect(
            windowRect.x + x * scale,
            windowRect.y + y * scale,
            width * scale,
            height * scale);
    }

    private static bool ContainsInclusive(Rect rect, Vector2 point)
    {
        return point.x >= rect.xMin && point.x <= rect.xMax &&
               point.y >= rect.yMin && point.y <= rect.yMax;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }


}

}
