using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class GossipDlg
{
    internal int UnreadSoundRequestCountForIndependentRuntime { get; private set; }
    internal static void ReceiveForIndependentRuntime(string from, string text, List<Share> shares)
    {
        Receive(from, text, shares);
        // Observation only, after the native decision/callback succeeds. No second sound or
        // notice path; Receive leaves these three fields at the branch it just evaluated.
        if (I.visible && I.minimized && I.peer == (from ?? string.Empty))
            I.UnreadSoundRequestCountForIndependentRuntime++;
    }
    internal static GossipDlg BindForIndependentRuntime()
    {
        Ensure();
        return I;
    }
    internal bool MinimizedForIndependentRuntime { get => minimized; set => minimized = value; }
    internal bool UnreadForIndependentRuntime { get => unread; set => unread = value; }
    internal bool HistoryForIndependentRuntime => showHistory;
    internal bool FaceForIndependentRuntime => showFace;
    internal string PeerForIndependentRuntime => peer;
    internal string MessageForIndependentRuntime => msg;
    internal float XForIndependentRuntime => winX;
    internal float YForIndependentRuntime => winY;
    internal int TabPageForIndependentRuntime => tabPage;
    internal IReadOnlyList<string> PeersForIndependentRuntime => peerOrder;
    internal IReadOnlyCollection<string> UnreadPeersForIndependentRuntime => unreadPeers;
    internal bool IsUnreadForIndependentRuntime(string name) => unreadPeers.Contains(name ?? "");
    internal IReadOnlyList<ReferenceGossipLine> LinesForIndependentRuntime()
    {
        List<string> lines = Cur;
        List<List<Share>> shares = ShrOf(peer);
        var result = new List<ReferenceGossipLine>(lines.Count);
        for (int i = 0; i < lines.Count; i++)
            result.Add(new ReferenceGossipLine(lines[i], ChatUI.ProjectIncomingShares(i < shares.Count ? shares[i] : null)));
        return result.AsReadOnly();
    }
    internal void EditForIndependentRuntime(string text) => msg = ChatUI.GuardShareEdit(msg, text, pendShr);
    internal bool SendForIndependentRuntime()
    {
        bool nonempty = (msg ?? "").Trim().Length != 0;
        Send();
        return nonempty;
    }
    internal Rect WindowForIndependentRuntime(float scale) => new Rect(winX, winY, NW * scale, (showHistory ? NH_HIST : NH) * scale);
    internal Rect BarForIndependentRuntime(float scale) => new Rect(winX - 3f * scale, winY - 3f * scale, BAR_W * scale, BAR_H * scale);
    internal Rect StripForIndependentRuntime => stripRect;
}
}
