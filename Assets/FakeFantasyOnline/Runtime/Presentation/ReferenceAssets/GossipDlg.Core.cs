using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class GossipDlg : MonoBehaviour
{
    public static GossipDlg I;
    private const float NW = 237f, NH = 305f, NH_HIST = 401f, BAR_W = 243f, BAR_H = 51f;
    private const float TabW = 54f, TabH = 22f, StripH = 26f;
    private static readonly int[] FLASH = { 0, 0, 0, 1, 0, 1, 0, 0 };
    private bool visible, tried;
    private string msg = string.Empty;
    private GUIStyle _e;
    private Texture2D bg, bar0, bar1, icoClose, icoMin, icoVoice, icoRecord, icoFace, icoSend;
    private float s, winX = float.NaN, winY, listScroll;
    private int _wdGen = -1;
    private bool followBottom = true;
    private string peer = string.Empty;
    private bool minimized, showHistory, unread, showFace;
    private readonly Dictionary<string, List<string>> convos = new Dictionary<string, List<string>>();
    private readonly List<string> _scratch = new List<string>();
    private readonly List<string> peerOrder = new List<string>();
    private readonly HashSet<string> unreadPeers = new HashSet<string>();
    private int tabPage;
    private Rect stripRect;
    private readonly Dictionary<string, List<List<Share>>> convoShr = new Dictionary<string, List<List<Share>>>();
    private readonly List<List<Share>> _scratchShr = new List<List<Share>>();
    private readonly List<(byte, byte, int, string)> pendShr = new List<(byte, byte, int, string)>();

    // Official instance storage; a nullable share-list reference is retained per native line.
    private List<string> Cur => Lines(peer);
    private int TabsPerPage => 3;
    private List<string> Lines(string name)
    {
        name ??= string.Empty;
        if (name.Length == 0) return _scratch;
        if (!convos.TryGetValue(name, out List<string> lines)) convos[name] = lines = new List<string>();
        return lines;
    }
    private List<List<Share>> ShrOf(string name)
    {
        name ??= string.Empty;
        if (name.Length == 0) return _scratchShr;
        if (!convoShr.TryGetValue(name, out List<List<Share>> shares))
            convoShr[name] = shares = new List<List<Share>>();
        return shares;
    }
    private void TouchPeer(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        peerOrder.Remove(name);
        peerOrder.Insert(0, name);
    }
    public static void Ensure()
    {
        if (I != null) return;
        var root = new GameObject("GossipDlg");
        I = root.AddComponent<GossipDlg>();
        DontDestroyOnLoad(root);
    }
    public static void Toggle() { Ensure(); I.visible = !I.visible; }
    public static bool Visible => I != null && I.visible;
    public static bool InputFocused => GUI.GetNameOfFocusedControl() == "gossip_in";
    public static bool Typing => I != null && I.visible && !I.minimized && InputFocused;
    public static void Open(string name)
    {
        Ensure();
        I.peer = name ?? string.Empty;
        I.visible = true; I.minimized = false; I.unread = false;
        I.Lines(I.peer);
        I.listScroll = 0f; I.followBottom = true;
        I.TouchPeer(I.peer);
        I.unreadPeers.Remove(I.peer);
        I.tabPage = 0;
        SystemInfoBar.ClearGossipNotice(name ?? string.Empty);
    }
    public static void Receive(string from, string text, List<Share> shares)
    {
        Ensure();
        from ??= string.Empty;
        if (I.peer.Length == 0) I.peer = from;
        I.Lines(from).Add(from + ": " + text);
        I.ShrOf(from).Add(shares);
        I.TouchPeer(from);
        if (I.visible && I.peer == from)
        {
            if (I.minimized) { I.unread = true; SoundManager.Play(1009); }
            return;
        }
        I.unreadPeers.Add(from);
        SystemInfoBar.AddGossipNotice(from);
    }
    private void Send()
    {
        string text = (msg ?? string.Empty).Trim();
        if (text.Length == 0) return;
        if (string.IsNullOrEmpty(peer))
            Lines(peer).Add("(请先在玩家身上右键选「交谈」, 或点顶部感叹号选择对话人)");
        else if (peer == CharData.name)
            Lines(peer).Add("(不能和自己交谈)");
        else
        {
            string sender = string.IsNullOrEmpty(CharData.name) ? "我" : CharData.name;
            Lines(peer).Add(sender + ": " + text);
            List<Share> shares = null;
            if (pendShr.Count > 0)
            {
                ChatUI.TakeOutgoing(text, pendShr);
                shares = BuildLocalShares(ChatUI.OutgoingShares);
            }
            ShrOf(peer).Add(shares);
            ChatUI.OnSend?.Invoke("交谈:" + peer, text);
        }
        msg = string.Empty;
    }
    public static bool InsertShare(byte kind, byte tab, int index, string name)
    {
        Ensure();
        if (I == null) return false;
        if (I.pendShr.Count >= 4)
        {
            I.Lines(I.peer).Add("(一句话最多分享4件)");
            I.ShrOf(I.peer).Add(null);
            return false;
        }
        I.msg = I.msg + "【" + name + "】";
        I.pendShr.Add((kind, tab, index, name));
        GUI.FocusControl("gossip_in");
        return true;
    }
    private Rect ComputeStripRect()
    {
        float y = winY < (StripH + 2f) * s
            ? winY + (showHistory ? NH_HIST + 2f : NH + 2f) * s
            : winY - (StripH + 2f) * s;
        return new Rect(winX, y, NW * s, StripH * s);
    }
    private float CurH => minimized ? BAR_H : showHistory ? NH_HIST : NH;
    private float CurW => minimized ? BAR_W : NW;
    private Rect L(float x, float y, float width, float height) => new Rect(winX + x * s, winY + y * s, width * s, height * s);
}
}
