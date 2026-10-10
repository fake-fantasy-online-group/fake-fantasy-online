using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal sealed class ReferenceGossipPlayerMenuIntent
{
    public ReferenceGossipPlayerMenuIntent(
        string targetName,
        string copyLine,
        string copyBlock,
        Vector2 anchor)
    {
        TargetName = targetName ?? string.Empty;
        CopyLine = copyLine;
        CopyBlock = copyBlock;
        Anchor = anchor;
    }

    public string TargetName { get; }
    public string CopyLine { get; }
    public string CopyBlock { get; }
    public Vector2 Anchor { get; }
}

public sealed class ReferenceGossipLine
{
    public ReferenceGossipLine(string text, IReadOnlyList<WorldChatSharedEntryState> shares)
    { Text = text; Shares = shares; }
    public string Text { get; }
    public IReadOnlyList<WorldChatSharedEntryState> Shares { get; }
}

// Borrowed native owner and immutable observations; Unity drives GossipDlg and owns its lifetime.
public sealed class ReferenceFirstWorldGossipDialog : IDisposable
{
    public const string BackgroundPath = "ui/gossipdlg/gossipdlg.png";
    public const string BackgroundFallbackPath = "res/uires/gossipdlg/gossipdlg.gsa";
    public const string BarPath = "res/uires/gossipdlg/gossipbar.gsa";
    public const string ClosePath = "res/uires/common/miniclose.gsa";
    public const string MinimizePath = "res/uires/common/minimizebtn.gsa";
    public const string VoicePath = "res/uires/gameui/fetchphonebtn.gsa";
    public const string HistoryPath = "res/uires/gossipdlg/recordbtn.gsa";
    public const string FacePath = "res/uires/gossipdlg/facebtn.gsa";
    public const string SendPath = "res/uires/gossipdlg/sendbtn.gsa";
    public const string InputControlName = "gossip_in";

    public const float WindowWidth = 237f;
    public const float WindowHeight = 305f;
    public const float HistoryWindowHeight = 401f;
    public const float MinimizedWidth = 243f;
    public const float MinimizedHeight = 51f;
    public const int TabsPerPage = 3;
    public const int MaximumShares = 4;


    private readonly GossipDlg owner;
    private bool disposed;
    private ReferenceFirstWorldGossipDialog() { owner = GossipDlg.BindForIndependentRuntime(); }
    public bool Ready => !disposed && owner != null;
    public bool Visible => Ready && GossipDlg.Visible;
    public bool Minimized => Ready && owner.MinimizedForIndependentRuntime;
    public bool ShowHistory => Ready && owner.HistoryForIndependentRuntime;
    public bool ShowFace => Ready && owner.FaceForIndependentRuntime;
    public bool Unread => Ready && owner.UnreadForIndependentRuntime;
    public bool Typing => GossipDlg.Typing;
    public bool InputFocused => GossipDlg.InputFocused;
    public string Peer => owner.PeerForIndependentRuntime;
    public string Message => owner.MessageForIndependentRuntime;
    public string HoverTooltip => string.Empty;
    public string LastSystemChatMessage => string.Empty;
    public int UnreadSoundRequestCount => owner.UnreadSoundRequestCountForIndependentRuntime;
    public float WindowX => owner.XForIndependentRuntime;
    public float WindowY => owner.YForIndependentRuntime;
    public int TabPage => owner.TabPageForIndependentRuntime;
    public IReadOnlyList<string> PeerOrder => owner.PeersForIndependentRuntime;
    public IReadOnlyCollection<string> UnreadPeers => owner.UnreadPeersForIndependentRuntime;
    public IReadOnlyList<ReferenceGossipLine> CurrentLines => owner.LinesForIndependentRuntime();
    // Native GameBootstrap.OnSend now owns transport and PlayerActionMenu owns its own callbacks.
    public WorldChatSendRequest LastSendRequest => null;
    internal ReferenceGossipPlayerMenuIntent LastPlayerMenuIntent => null;
    public bool IsPeerUnread(string name) => owner.IsUnreadForIndependentRuntime(name);
    public static bool TryCreate(ReferenceResourceLoader resources, out ReferenceFirstWorldGossipDialog owner)
    {
        owner = resources == null ? null : new ReferenceFirstWorldGossipDialog();
        return owner != null;
    }
    // Share builders read the original CharData owner. Retiring this observation adapter
    // must not clear pet slots or change the native component's state or callback lifetime.

    public bool Open(string name) { ThrowIfDisposed(); GossipDlg.Open(name); return true; }
    public bool Toggle() { ThrowIfDisposed(); GossipDlg.Toggle(); return true; }
    public bool Minimize()
    { ThrowIfDisposed(); if (!Visible) return false; owner.MinimizedForIndependentRuntime = true; return true; }
    public bool Restore()
    { ThrowIfDisposed(); if (!Visible) return false; owner.MinimizedForIndependentRuntime = false; owner.UnreadForIndependentRuntime = false; return true; }
    public bool Receive(WorldPrivateChatState state) => state != null && Receive(state.SenderName, state.Text, state.Shares);
    public bool Receive(string from, string text, IReadOnlyList<WorldChatSharedEntryState> shares)
    {
        ThrowIfDisposed();
        GossipDlg.ReceiveForIndependentRuntime(from, text, ChatUI.MapIncomingShares(shares));
        return true;
    }
    public bool SetMessage(string text) { ThrowIfDisposed(); owner.EditForIndependentRuntime(text); return true; }
    public bool InsertShare(byte kind, byte tab, int slot, string name)
    { ThrowIfDisposed(); return GossipDlg.InsertShare(kind, tab, slot, name); }
    public bool RequestSend(string localPlayerName)
    { ThrowIfDisposed(); return owner.SendForIndependentRuntime(); }
    public WorldChatSendRequest ConsumeSendRequest() => null;
    public string ConsumeSystemChatMessage() => string.Empty;
    internal ReferenceGossipPlayerMenuIntent ConsumePlayerMenuIntent() => null;
    public Rect WindowRect(float scale) => owner.WindowForIndependentRuntime(scale);
    public Rect MinimizedRect(float scale) => owner.BarForIndependentRuntime(scale);
    public Rect SessionStripRect(float scale) => owner.StripForIndependentRuntime;
    public bool MouseOver(Vector2 pointer, float scale) => Ready && GossipDlg.MouseOver();
    public bool Draw(float scale, string localPlayerName)
    { ThrowIfDisposed(); return owner != null; }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
    }
    private void ThrowIfDisposed()
    { if (disposed) throw new ObjectDisposedException(nameof(ReferenceFirstWorldGossipDialog)); }
}
}
