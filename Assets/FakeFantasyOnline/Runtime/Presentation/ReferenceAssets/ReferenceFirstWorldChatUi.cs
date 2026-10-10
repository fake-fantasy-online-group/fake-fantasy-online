using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal sealed class ReferenceChatLineMenuIntent
{
    public ReferenceChatLineMenuIntent(
        Rect lineRect,
        string who,
        string fullText,
        string copyBlock,
        Vector2 anchor)
    {
        LineRect = lineRect;
        Who = who ?? string.Empty;
        FullText = fullText ?? string.Empty;
        CopyBlock = copyBlock ?? string.Empty;
        Anchor = anchor;
    }

    public Rect LineRect { get; }
    public string Who { get; }
    public string FullText { get; }
    public string CopyBlock { get; }
    public Vector2 Anchor { get; }
}

public sealed class ReferenceChatLine
{
    public ReferenceChatLine(string channel, string text, Color color, string who = "")
        : this(
            channel,
            text,
            color,
            who,
            Array.Empty<WorldChatSharedEntryState>())
    {
    }

    public ReferenceChatLine(
        string channel,
        string text,
        Color color,
        string who,
        IReadOnlyList<WorldChatSharedEntryState> shares)
    {
        Channel = channel ?? string.Empty;
        Text = text ?? string.Empty;
        Color = color;
        Who = who ?? string.Empty;
        if (shares == null)
        {
            throw new ArgumentNullException(nameof(shares));
        }

        NativeShares = ChatUI.MapIncomingShares(shares);
    }

    public string Channel { get; }
    public string Text { get; }
    public Color Color { get; }
    public string Who { get; }
    public IReadOnlyList<WorldChatSharedEntryState> Shares => NativeShares;
    internal List<Share> NativeShares { get; }

    internal ReferenceChatLine((string, string, Color, string, List<Share>) line)
    {
        Channel = line.Item1;
        Text = line.Item2;
        Color = line.Item3;
        Who = line.Item4;
        NativeShares = line.Item5;
    }
}

internal sealed class ReferenceChatEmoteCodes
{
    public const string TablePath = "tables/emote_codes.json";

    private readonly Dictionary<string, int> codes;

    private ReferenceChatEmoteCodes(Dictionary<string, int> codes)
    {
        this.codes = codes;
    }

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceChatEmoteCodes owner)
    {
        owner = null;
        if (resources == null)
        {
            return false;
        }

        byte[] bytes = resources.ReadSourceOwned(TablePath);
        if (bytes == null)
        {
            return false;
        }

        EmoteCodeTable table;
        try
        {
            table = JsonUtility.FromJson<EmoteCodeTable>(
                new UTF8Encoding(false, true).GetString(bytes));
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is DecoderFallbackException)
        {
            return false;
        }

        if (table == null || table.code == null || table.num == null ||
            table.code.Length != table.num.Length || table.code.Length != 96)
        {
            return false;
        }

        Dictionary<string, int> parsed =
            new Dictionary<string, int>(table.code.Length, StringComparer.Ordinal);
        for (int index = 0; index < table.code.Length; index++)
        {
            string code = table.code[index];
            int number = table.num[index];
            if (string.IsNullOrEmpty(code) || number < 1 || number > 96 ||
                parsed.ContainsKey(code))
            {
                return false;
            }

            for (int character = 0; character < code.Length; character++)
            {
                if (code[character] < 'a' || code[character] > 'z')
                {
                    return false;
                }
            }

            parsed.Add(code, number);
        }

        owner = new ReferenceChatEmoteCodes(parsed);
        return true;
    }

    public string Expand(string value)
    {
        if (string.IsNullOrEmpty(value) || value.IndexOf('/') < 0)
        {
            return value ?? string.Empty;
        }

        StringBuilder output = new StringBuilder(value.Length + 8);
        for (int offset = 0; offset < value.Length;)
        {
            if (value[offset] != '/' || offset + 1 >= value.Length)
            {
                output.Append(value[offset++]);
                continue;
            }

            int end = offset + 1;
            while (end < value.Length && value[end] >= 'a' && value[end] <= 'z')
            {
                end++;
            }

            if (end == offset + 1)
            {
                output.Append(value[offset++]);
                continue;
            }

            int number;
            string code = value.Substring(offset + 1, end - offset - 1);
            if (!codes.TryGetValue(code, out number))
            {
                output.Append(value[offset++]);
                continue;
            }

            output.Append("\\e");
            output.Append(number.ToString("D2"));
            offset = end;
        }

        return output.ToString();
    }

    [Serializable]
    private sealed class EmoteCodeTable
    {
        public string[] code;
        public int[] num;
    }
}

/// <summary>
/// Map-scoped observations and diagnostic helpers for the native ChatUI owner.
/// </summary>
/// <remarks>
/// This map borrower exposes the native owner's state and retains the existing input,
/// history and geometry compatibility helpers. ChatUI owns its actual Unity rendering.
/// </remarks>
public sealed class ReferenceFirstWorldChatUi : IDisposable
{
    public const string InputBarPath = "ui/chatbar/inputbar.png";
    public const string WindowUpPath = "ui/chatbar/winup.png";
    public const string WindowMiddlePath = "ui/chatbar/winmid.png";
    public const string WindowDownPath = "ui/chatbar/windown.png";
    public const string InputControlName = "chatinput";
    public const int InputMaximumCharacters = 120;

    private const float BarBottomOffset = 36f;
    private const float BarWidth = 418f;
    private const float BarHeight = 30f;
    private const float PanelLeft = 24f;
    private const float PanelWidth = 390f;
    private const float PanelHeight = 154f;
    private const float ContextWidth = 62f;
    private const float ContextRowHeight = 17f;

    private static readonly string[] ContextItems =
    {
        "复制",
        "粘贴",
        "剪切",
        "清空"
    };

    private static readonly Color ChannelColor =
        new Color32(180, 255, 180, 255);
    private static readonly Color ChatChannelLabelColor =
        new Color(1f, 220f / 255f, 120f / 255f, 1f);
    private static readonly Color ContextBackground =
        new Color(0.09f, 0.08f, 0.12f, 0.97f);
    private static readonly Color ContextBorder =
        new Color(0.42f, 0.36f, 0.22f, 1f);
    private static readonly Color ContextHover =
        new Color(0.55f, 0.42f, 0.18f, 0.55f);

    // Borrow the mounted native owner's actual fields. No texture or array is copied into
    // the view, including partially published resource state left by an exceptional Load.
    private Texture2D inputBar => chatOwner.InputBarForIndependentRuntime;
    private Texture2D windowUp => chatOwner.WindowUpForIndependentRuntime;
    private Texture2D windowMiddle => chatOwner.WindowMiddleForIndependentRuntime;
    private Texture2D windowDown => chatOwner.WindowDownForIndependentRuntime;
    private Texture2D[] dropButton => chatOwner.DropButtonForIndependentRuntime;
    private Texture2D[] sendButton => chatOwner.SendButtonForIndependentRuntime;
    private Texture2D[] faceButton => chatOwner.FaceButtonForIndependentRuntime;
    private Texture2D[] sizeButton => chatOwner.SizeButtonForIndependentRuntime;
    private Texture2D[][] sideButtonFrames => chatOwner.SideButtonFramesForIndependentRuntime;
    // Borrow ChatUI's one static pending list for the existing edit and observation paths.
    // View creation/binding/disposal never allocates, replaces or clears this native root.
    private List<(byte, byte, int, string)> pendingShares
        => ChatUI.PendingSharesForIndependentRuntime;

    private GUIStyle editStyle
    {
        get => chatOwner != null ? chatOwner.EditStyleForIndependentRuntime : detachedEditStyle;
        set
        {
            if (chatOwner != null) chatOwner.EditStyleForIndependentRuntime = value;
            else detachedEditStyle = value;
        }
    }

    private int channelIndex
    {
        get => chatOwner != null ? chatOwner.ChannelIndexForIndependentRuntime : detachedChannelIndex;
        set
        {
            if (chatOwner != null) chatOwner.ChannelIndexForIndependentRuntime = value;
            else detachedChannelIndex = value;
        }
    }

    private bool chatFade
    {
        get => chatOwner != null ? chatOwner.ChatFadeForIndependentRuntime : detachedChatFade;
        set
        {
            if (chatOwner != null) chatOwner.ChatFadeForIndependentRuntime = value;
            else detachedChatFade = value;
        }
    }

    // The official ChatUI owner mounted on the recovered world root. Once bound, the two
    // properties below read and write that owner's official slots +0x6b (faceOpen) and +0x6c
    // (chanOpen), so ChatUI.ToggleFacePanel (0x060000e0), ChatUI.ToggleChannelList
    // (0x060000e1) and this bar's own face/channel buttons all drive one and the same flag.
    // The detached halves exist only for a carrier that was built without an official owner
    // (the Editor audits call TryCreate directly); they are never both live at once.
    private ChatUI chatOwner;
    private bool detachedFaceOpen;
    private bool detachedChannelOpen;

    // Detached halves for three more official slots, kept only for a carrier built without an
    // official owner. ChatUI.panelsVisible (+0x69) and ChatUI.whisperTarget (+0x88) are
    // instance slots of the mounted owner, while ChatUI._pendingFocus (+0x58) is the official
    // static slot, so a bound carrier writes the owner's slot and a detached one writes these.
    private bool detachedPanelsVisible;
    private string detachedWhisperTarget = string.Empty;
    private string detachedInputText = string.Empty;
    private string detachedChannel = "附近";
    private bool detachedPendingFocus;
    private GUIStyle detachedEditStyle;
    private int detachedChannelIndex;
    private bool detachedChatFade;
    private int detachedSysSeqSeen;
    private float detachedSysShownTime = -999f;

    private bool faceOpen
    {
        get => chatOwner != null ? chatOwner.FaceOpenForIndependentRuntime : detachedFaceOpen;
        set
        {
            if (chatOwner != null)
            {
                chatOwner.FaceOpenForIndependentRuntime = value;
            }
            else
            {
                detachedFaceOpen = value;
            }
        }
    }

    private bool channelOpen
    {
        get => chatOwner != null ? chatOwner.ChanOpenForIndependentRuntime : detachedChannelOpen;
        set
        {
            if (chatOwner != null)
            {
                chatOwner.ChanOpenForIndependentRuntime = value;
            }
            else
            {
                detachedChannelOpen = value;
            }
        }
    }

    // The official focus request slot. It is a static of the official ChatUI owner, so every
    // bound carrier shares the one flag StartWhisper sets and the one OnGUI pass consumes; a
    // detached carrier keeps its own half instead.
    private bool pendingFocus
    {
        get => chatOwner != null
            ? ChatUI.PendingFocusForIndependentRuntime
            : detachedPendingFocus;
        set
        {
            if (chatOwner != null)
            {
                ChatUI.PendingFocusForIndependentRuntime = value;
            }
            else
            {
                detachedPendingFocus = value;
            }
        }
    }

    private int sysSeqSeen
    {
        get => chatOwner != null ? chatOwner.SystemSequenceSeenForIndependentRuntime : detachedSysSeqSeen;
        set
        {
            if (chatOwner != null) chatOwner.SystemSequenceSeenForIndependentRuntime = value;
            else detachedSysSeqSeen = value;
        }
    }

    private float sysShownTime
    {
        get => chatOwner != null ? chatOwner.SystemShownTimeForIndependentRuntime : detachedSysShownTime;
        set
        {
            if (chatOwner != null) chatOwner.SystemShownTimeForIndependentRuntime = value;
            else detachedSysShownTime = value;
        }
    }
    private bool contextOpen;
    private Vector2 contextAnchor;
    private bool disposed;

    private ReferenceFirstWorldChatUi(ReferenceResourceLoader resources)
    {
        PanelsVisible = true;
        Channel = ChatUI.Channels[0];
        InputText = string.Empty;
        WhisperTarget = string.Empty;
        // Construct only the auxiliary view. The actual native consumers own their resource
        // reads and readiness decisions; creating a map borrower must not load or gate them.
        Ready = true;
    }

    // Attaches the official ChatUI owner that ReferenceFirstWorldWindowOwner mounts on the
    // world root. The reference is captured by instance, never read back out of the static
    // ChatUI.I, so disposing an older carrier can only touch the owner it was bound to and can
    // never clear the binding a newer carrier has already taken on re-login.
    internal void BindChatOwnerForIndependentRuntime(ChatUI owner)
    {
        chatOwner = owner;
    }

    // Auxiliary view construction only; never a native resource-load success/readiness flag.
    public bool Ready { get; private set; }

    internal int VersionForIndependentRuntime => ChatUI.Ver;
    internal int SystemSequenceForIndependentRuntime => ChatUI.SystemSequenceForIndependentRuntime;

    // Official ChatUI instance slot +0x69 (panelsVisible), which ChatUI..ctor (0x060000fb)
    // initialises to true and 0x060000e5 sets to true again when a whisper target is opened.
    public bool PanelsVisible
    {
        get => chatOwner != null
            ? chatOwner.PanelsVisibleForIndependentRuntime
            : detachedPanelsVisible;
        private set
        {
            if (chatOwner != null)
            {
                chatOwner.PanelsVisibleForIndependentRuntime = value;
            }
            else
            {
                detachedPanelsVisible = value;
            }
        }
    }

    public bool ChatFade => chatFade;

    public bool ChannelOpen => channelOpen;

    public bool FaceOpen => faceOpen;

    public bool HistoryVisible => ChatCopyOverlay.Visible;

    // The menu outlives this map borrower. Route from the same owner name that
    // UiInput.Draw uses, including before a replacement view's first observation.
    public bool InputMenuOpen => chatOwner != null
        ? string.Equals(UiInput.MenuOwnerForIndependentRuntime, InputControlName, StringComparison.Ordinal)
        : contextOpen;

    public bool HasPendingShares => ChatUI.HasPendingShares;

    // The automatic native owner publishes this state even between map borrowers.
    public bool TypingNow => chatOwner != null && ChatUI.TypingNow;

    // A mounted view edits the exact slots SendNow reads. Detached construction retains its
    // existing diagnostic state only; no click-time snapshot or second mounted value exists.
    public string InputText
    {
        get => chatOwner != null ? chatOwner.InputTextForIndependentRuntime : detachedInputText;
        private set
        {
            if (chatOwner != null) chatOwner.InputTextForIndependentRuntime = value;
            else detachedInputText = value;
        }
    }

    public string Channel
    {
        get => chatOwner != null ? chatOwner.ChannelForIndependentRuntime : detachedChannel;
        private set
        {
            if (chatOwner != null) chatOwner.ChannelForIndependentRuntime = value;
            else detachedChannel = value;
        }
    }

    // Official ChatUI instance slot +0x88 (whisperTarget), the only slot 0x060000e5 writes
    // before it asks for focus and prints its system line.
    public string WhisperTarget
    {
        get => chatOwner != null
            ? chatOwner.WhisperTargetForIndependentRuntime
            : detachedWhisperTarget;
        private set
        {
            if (chatOwner != null)
            {
                chatOwner.WhisperTargetForIndependentRuntime = value;
            }
            else
            {
                detachedWhisperTarget = value;
            }
        }
    }

    public string HoverTooltip { get; private set; } = string.Empty;

    internal ReferenceChatLineMenuIntent LastLineMenuIntent { get; private set; }

    internal string CopyOverlayText => ChatCopyOverlay.TextForIndependentRuntime;

    internal string CopyOverlayTitle => ChatCopyOverlay.TitleForIndependentRuntime;

    public IReadOnlyList<ReferenceChatLine> Lines => Snapshot();

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceFirstWorldChatUi owner)
    {
        owner = null;
        if (resources == null)
        {
            return false;
        }

        owner = new ReferenceFirstWorldChatUi(resources);
        return owner.Ready;
    }

    public bool StartWhisper(string targetName)
    {
        ThrowIfDisposed();
        if (!Ready)
        {
            return false;
        }

        // 0x060000e5 ChatUI.StartWhisper is the official owner of this transition, so a bound
        // carrier hands the raw menu name straight to it and touches nothing else: it writes
        // ChatUI.whisperTarget (+0x88), ChatUI.panelsVisible (+0x69) and the static
        // ChatUI._pendingFocus (+0x58) in that order and then prints one system line, and it is
        // a silent no-op while ChatUI.I is null. The detached branch below exists only for a
        // carrier built without an official owner and repeats the same four steps on the
        // detached halves.
        if (chatOwner != null)
        {
            ChatUI.StartWhisper(targetName);
            return true;
        }

        string target = targetName ?? string.Empty;
        WhisperTarget = target;
        PanelsVisible = true;
        pendingFocus = true;
        AddLine(
            "系统",
            "已对 " + target + " 开启密语, 输入内容回车发送",
            ChatUI.WhisperSystemLineColor,
            string.Empty);
        return true;
    }

    public bool SetInputText(string value)
    {
        ThrowIfDisposed();
        if (!Ready)
        {
            return false;
        }

        InputText = ChatUI.GuardShareEdit(
            InputText,
            Clamp(value, InputMaximumCharacters),
            pendingShares);
        return true;
    }

    // Existing compatibility entry: the current native ChatUI owns insertion decisions.
    public bool InsertShare(byte kind, byte tab, int slot, string name)
        => ChatUI.InsertShare(kind, tab, slot, name);

    public bool OpenHistory()
    {
        ThrowIfDisposed();
        if (!Ready)
        {
            return false;
        }

        Win05.Open("historybar");
        return true;
    }

    public bool CloseInputMenu()
    {
        ThrowIfDisposed();
        if (!contextOpen)
        {
            return false;
        }

        contextOpen = false;
        return true;
    }

    public bool CloseHistory()
    {
        ThrowIfDisposed();
        if (!ChatCopyOverlay.Visible)
        {
            return false;
        }

        ChatCopyOverlay.Hide();
        return true;
    }

    /// <summary>
    /// Receives a local system notice emitted by another source-owned UI owner.
    /// </summary>
    internal bool AddSystemMessage(string text, Color color)
    {
        ThrowIfDisposed();
        if (!Ready || text == null)
        {
            return false;
        }

        AddLine("系统", text, color, string.Empty);
        return true;
    }

    internal bool ApplyPlayerActionCopyDestination(
        ReferencePlayerActionMenuAction action,
        string targetName,
        string copyLine,
        string copyBlock,
        ReferenceFirstWorldUiTip uiTip)
    {
        ThrowIfDisposed();
        if (!Ready || uiTip == null || !uiTip.Ready)
        {
            return false;
        }

        if (action == ReferencePlayerActionMenuAction.CopyLine)
        {
            if (string.IsNullOrEmpty(copyLine))
            {
                return false;
            }

            GUIUtility.systemCopyBuffer = copyLine;
            return uiTip.Request("已复制这一行", 1.2f);
        }

        if (action == ReferencePlayerActionMenuAction.CopyBlock)
        {
            if (string.IsNullOrEmpty(copyBlock))
            {
                return false;
            }

            GUIUtility.systemCopyBuffer = copyBlock;
            int rows = 1;
            for (int index = 0; index < copyBlock.Length; index++)
            {
                if (copyBlock[index] == '\n')
                {
                    rows++;
                }
            }

            return uiTip.Request("已复制 " + rows + " 行聊天记录", 1.2f);
        }

        if (action == ReferencePlayerActionMenuAction.SelectCopy)
        {
            if (string.IsNullOrEmpty(copyBlock))
            {
                return false;
            }

            string caption = string.IsNullOrEmpty(targetName)
                ? ReferenceChatCopyOverlay.HistoryTitle
                : ReferenceChatCopyOverlay.WithPlayerTitlePrefix + targetName +
                  ReferenceChatCopyOverlay.WithPlayerTitleSuffix;
            ChatCopyOverlay.OpenForIndependentRuntime(copyBlock, caption);
            return true;
        }

        return false;
    }

    public string RecentText(int count) => ChatUI.RecentText(count);

    internal IReadOnlyList<ReferenceChatLine> Snapshot()
    {
        var snapshot = ChatUI.Snapshot();
        var result = new List<ReferenceChatLine>(snapshot.Count);
        foreach (var line in snapshot) result.Add(new ReferenceChatLine(line));
        return result.AsReadOnly();
    }

    public bool SelectChannel(int index)
    {
        ThrowIfDisposed();
        if (index < 0 || index >= ChatUI.Channels.Length)
        {
            return false;
        }

        channelIndex = index;
        Channel = ChatUI.Channels[index];
        WhisperTarget = string.Empty;
        channelOpen = false;
        return true;
    }

    public bool ToggleFacePanel()
    {
        ThrowIfDisposed();
        if (!Ready)
        {
            return false;
        }

        faceOpen = !faceOpen;
        return true;
    }

    // A bound compatibility view uses the official 1.6.0 public ToggleChannelList
    // entry, including its filter-close transition. Detached diagnostic state is unchanged.
    public bool ToggleChannelList()
    {
        ThrowIfDisposed();
        if (!Ready)
        {
            return false;
        }

        if (chatOwner != null) chatOwner.ToggleChannelList();
        else channelOpen = !channelOpen;
        return true;
    }

    internal bool ApplyEmotePick(int emoteId)
    {
        // OnGUI tests only a positive picker result. Append to the live input after Draw;
        // the source has no second range/readiness gate, share guard or length clamp here.
        if (emoteId < 1)
        {
            return false;
        }

        InputText = InputText + "\\e" + emoteId.ToString("D2");
        faceOpen = false;
        return true;
    }

    /// <summary>
    /// Appends one authoritative channel-chat row using the exact reference channel mapping.
    /// </summary>
    public bool AddChannelChat(WorldChannelChatState state)
    {
        ThrowIfDisposed();
        if (!Ready || state == null)
        {
            return false;
        }

        ChatUI.ReceiveChannelForIndependentRuntime(state.ChannelCode, state.SenderName, state.Text,
            state.BroadcastTier, state.BroadcastSkin, ChatUI.MapIncomingShares(state.Shares));
        return true;
    }

    // Official instance s (+0x94), shared with the real MouseOver consumer.
    private float s
    {
        get => chatOwner.DrawScaleForIndependentRuntime;
        set => chatOwner.DrawScaleForIndependentRuntime = value;
    }

    // The copy overlay retains its separate existing host path. ChatUI draws itself through
    // its own Unity OnGUI message and does not use this map borrower as a frame driver.
    internal void DrawCopyOverlayForIndependentRuntime(float scale)
    {
        ThrowIfDisposed();
        if (Ready && IsPositiveFinite(scale) && ChatCopyOverlay.Visible)
        {
            ChatCopyOverlay.DrawForIndependentRuntime();
        }
    }

    internal bool CopyOverlayMouseOverForIndependentRuntime(Vector2 point, float scale) =>
        ChatCopyOverlay.MouseOver();

    internal bool InputMenuMouseOverForIndependentRuntime(Vector2 point, float scale) =>
        UiInput.MouseOver();

    internal bool MouseOverForIndependentRuntime()
    {
        float scale = s > 0f ? s : UIScale.Factor;
        Vector2 point = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return MouseOverAt(point, Screen.height, scale);
    }

    public bool MouseOver(Vector2 topOriginPointer, int viewportHeight, float scale)
    {
        if (!(scale > 0f)) scale = UIScale.Factor;
        return MouseOverAt(topOriginPointer, viewportHeight, scale);
    }

    private bool MouseOverAt(Vector2 topOriginPointer, int viewportHeight, float scale)
    {
        Rect bar = BarRect(viewportHeight, scale);
        if (bar.Contains(topOriginPointer)) return true;
        if (PanelsVisible && new Rect(0f, bar.y - 120f * scale, 24f * scale, 120f * scale).Contains(topOriginPointer)) return true;
        if (faceOpen)
        {
            float width = ReferenceChatEmotePicker.PanelWidth(scale);
            float height = ReferenceChatEmotePicker.PanelHeight(scale);
            if (new Rect(412f * scale - width, bar.y - height - 3f * scale, width, height).Contains(topOriginPointer)) return true;
        }
        if (channelOpen && ChatUI.ChannelPanelRect(bar.y, scale).Contains(topOriginPointer)) return true;
        return chatOwner != null &&
            chatOwner.FilterMouseOverForIndependentRuntime(topOriginPointer, bar.y, scale);
    }

    public static Rect BarRect(int viewportHeight, float scale)
    {
        return new Rect(
            0f,
            (viewportHeight - BarBottomOffset * scale) - GameHud.BottomHotbarHeight(scale, Screen.width),
            BarWidth * scale,
            BarHeight * scale);
    }

    public static Rect PanelRect(int viewportHeight, float scale)
    {
        float barY = BarRect(viewportHeight, scale).y;
        return new Rect(
            PanelLeft * scale,
            barY - PanelHeight * scale,
            PanelWidth * scale,
            PanelHeight * scale);
    }

    public static Rect InputRect(int viewportHeight, float scale)
    {
        float barY = BarRect(viewportHeight, scale).y;
        return new Rect(
            82f * scale,
            barY + 9f * scale,
            296f * scale,
            17f * scale);
    }

    public static Rect FacePanelRect(int viewportHeight, float scale)
    {
        float barY = BarRect(viewportHeight, scale).y;
        float width = ReferenceChatEmotePicker.PanelWidth(scale);
        float height = ReferenceChatEmotePicker.PanelHeight(scale);
        return new Rect(
            412f * scale - width,
            barY - height - 3f * scale,
            width,
            height);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Ready = false;
        // This view borrows the unparented ChatUI created by GameHud.Load. Ordinary map
        // disposal does not destroy that native owner, and ChatUI has no reset callback.
        // Detach before clearing view-only fields so a later borrower keeps input, panels,
        // whisper/channel, style/timer state and the shared pending-focus request intact.
        chatOwner = null;
        PanelsVisible = false;
        // Releasing this auxiliary view is not construction/destruction of its ChatUI.
        // Do not turn its old view-only cleanup into a new native owner reset.
        detachedChatFade = false;
        channelOpen = false;
        faceOpen = false;
        contextOpen = false;
        pendingFocus = false;
        // Official ChatUI has no lifecycle member at all (its 38 members contain no OnEnable,
        // OnDisable, OnDestroy or Start; only Awake 0x060000e8), and GameBootstrap.TeardownWorldUi
        // (0x060003f3) never touches the ChatUI static block. The only official writers of static
        // slot +0x68 are OnGUI (0x060000f6) and set_TypingNow (0x060000e7), so discarding this
        // carrier must not publish a value the native owner never publishes.
        InputText = string.Empty;
        Channel = string.Empty;
        WhisperTarget = string.Empty;
        HoverTooltip = string.Empty;
        LastLineMenuIntent = null;
        detachedEditStyle = null;

    }

    internal static bool TryCreateLineMenuIntent(
        Rect lineRect,
        string who,
        string fullText,
        string copyBlock,
        EventType eventType,
        int button,
        Vector2 pointer,
        out ReferenceChatLineMenuIntent intent)
    {
        intent = null;
        if (eventType != EventType.MouseDown || button != 1 ||
            lineRect.width < 0f || lineRect.height < 0f ||
            !ContainsInclusive(lineRect, pointer))
        {
            return false;
        }

        intent = new ReferenceChatLineMenuIntent(
            lineRect,
            who,
            fullText,
            copyBlock,
            pointer);
        return true;
    }

    private Action<int, string, string, string, Vector2> openHistoryMenu;
    internal void BindHistoryMenuForIndependentRuntime(Action<int, string, string, string, Vector2> open)
        => openHistoryMenu = open;
    internal void OpenHistoryRowMenu(int id, string who, string fullText)
        => OpenLineMenuForIndependentRuntime(id, who, fullText, RecentText(30));
    private void OpenLineMenuForIndependentRuntime(int id, string who, string fullText, string copyBlock)
        => openHistoryMenu(id, who, fullText, copyBlock,
            new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));

    internal void DrawShareRowForHistory(float x, float y, string value, Color color,
        List<Share> shares, float scale, bool outline)
    {
        ChatUI.DrawShareRow(x, y, value, color, shares, scale, outline);
    }

    internal void ShareHoverForHistory(float x, float y, string value,
        List<Share> shares, float scale)
    {
        ChatUI.ShareHover(x, y, value, shares, scale);
    }

    private void AddLine(string channel, string text, Color color, string who)
    {
        AddLine(
            channel,
            text,
            color,
            who,
            Array.Empty<WorldChatSharedEntryState>());
    }

    private void AddLine(
        string channel,
        string text,
        Color color,
        string who,
        IReadOnlyList<WorldChatSharedEntryState> shares)
    {
        ChatUI.Add(channel, text, color, who, ChatUI.MapIncomingShares(shares));
    }

    private void ApplyContextAction(int index)
    {
        if (index == 0)
        {
            GUIUtility.systemCopyBuffer = InputText ?? string.Empty;
        }
        else if (index == 1)
        {
            InputText = ChatUI.GuardShareEdit(
                InputText,
                Clamp(
                    (InputText ?? string.Empty) +
                    (GUIUtility.systemCopyBuffer ?? string.Empty),
                    InputMaximumCharacters),
                pendingShares);
        }
        else if (index == 2)
        {
            GUIUtility.systemCopyBuffer = InputText ?? string.Empty;
            InputText = ChatUI.GuardShareEdit(InputText, string.Empty, pendingShares);
        }
        else if (index == 3)
        {
            InputText = ChatUI.GuardShareEdit(InputText, string.Empty, pendingShares);
        }

        GUI.FocusControl(InputControlName);
    }

    private static Rect ContextRect(Vector2 anchor, float scale)
    {
        return new Rect(
            anchor.x,
            anchor.y,
            ContextWidth * scale,
            ContextRowHeight * ContextItems.Length * scale);
    }

    private static bool ContainsInclusive(Rect rect, Vector2 point)
    {
        return point.x >= rect.xMin && point.x <= rect.xMax &&
               point.y >= rect.yMin && point.y <= rect.yMax;
    }

    private static string Clamp(string value, int maximum)
    {
        string text = value ?? string.Empty;
        return maximum > 0 && text.Length > maximum
            ? text.Substring(0, maximum)
            : text;
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceFirstWorldChatUi));
        }
    }

}

}
