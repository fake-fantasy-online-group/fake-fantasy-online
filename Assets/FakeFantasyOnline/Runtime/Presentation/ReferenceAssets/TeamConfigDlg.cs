using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// TeamConfigDlg. The runtime binding supplies the nearby-player snapshot;
// confirmation actions read the current NetClient sender.
internal sealed class TeamConfigDlg : MonoBehaviour
{
    public static TeamConfigDlg I;
    private const float NW = 255, NH = 212;
    private const float NbBoxX = 21, NbBoxY = 116, NbBoxW = 126, NbPadV = 8, NbRowH = 20, NbBotLimit = 185;
    private static int NbFitRows => 3;
    private static float NbBoxMaxH => NbFitRows * NbRowH + NbPadV;
    private static readonly string[] OPTS = { "允许他人邀请入队", "自动同意入队", "队伍经验平分", "队伍物品轮流", "显示队友位置" };
    private bool visible, tried;
    private Texture2D bg, icoClose, icoApply, icoCancel, icoAdd;
    private string tname = string.Empty;
    private int pendingInvite;
    private GUIStyle _te;
    private readonly HashSet<int> picked = new HashSet<int>();
    private bool showNeighbor;
    private int neighborScroll;
    private Texture2D pUL, pUR, pDL, pDR, pUM, pDM, pLM, pRM, chkOff, chkOn;
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1;
    private IPartyCommands commands;
    private static readonly Func<IReadOnlyList<PartyInviteTarget>> EmptyNearbySource =
        () => Array.Empty<PartyInviteTarget>();
    private Func<IReadOnlyList<PartyInviteTarget>> nearbySource = EmptyNearbySource;
    private object borrower;

    private static void Ensure()
    {
        if (I != null) return;
        I = new GameObject("TeamConfigDlg").AddComponent<TeamConfigDlg>();
        DontDestroyOnLoad(I.gameObject);
    }
    public static void Toggle() { Ensure(); I.visible = !I.visible; }
    public static bool Visible => I != null && I.visible;
    public static void Open(string defaultName, int inviteId)
    {
        Ensure(); I.tname = defaultName ?? string.Empty; I.pendingInvite = inviteId;
        I.picked.Clear(); if (inviteId != 0) I.picked.Add(inviteId);
        I.showNeighbor = false; I.neighborScroll = 0; I.visible = true;
    }
    // Official 1.6.0 06000d56: visibility and scale precede the independent input reads.
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * .5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * .5f : I.winY;
        Vector2 pointer = new Vector2(mx, my);
        if (new Rect(x, y, NW * scale, NH * scale).Contains(pointer)) return true;
        if (!I.showNeighbor) return false;
        float maxHeight = NbBoxMaxH;
        return new Rect(x + NbBoxX * scale, y + NbBoxY * scale,
            NbBoxW * scale, maxHeight * scale).Contains(pointer);
    }
    internal bool ContainsPointer(Vector2 pointer)
    {
        if (!visible) return false;
        float scale = UIScale.Factor;
        float x = float.IsNaN(winX) ? (Screen.width - NW * scale) * .5f : winX;
        float y = float.IsNaN(winX) ? (Screen.height - NH * scale) * .5f : winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(pointer) ||
            (showNeighbor && new Rect(x + NbBoxX * scale, y + NbBoxY * scale, NbBoxW * scale, NbBoxMaxH * scale).Contains(pointer));
    }
    private GUIStyle TEdit()
    {
        if (_te == null)
        {
            _te = new GUIStyle(GUIStyle.none) { font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleLeft };
            _te.normal.textColor = Color.white;
        }
        _te.fontSize = Mathf.RoundToInt(12 * s); return _te;
    }
    private void Load()
    {
        tried = true; ResLoader r = ResLoader.I;
        if (r == null) { tried = false; return; }
        bg = r.LoadGsa("res/uires/teamdlg/teamconfigdlg.gsa");
        icoClose = r.LoadGsa("res/uires/common/miniclose.gsa");
        icoApply = r.LoadGsa("res/uires/common/affirmbtn.gsa");
        icoCancel = r.LoadGsa("res/uires/common/cancelbtn.gsa");
        icoAdd = r.LoadGsa("res/uires/common/addbtn.gsa");
        pUL = r.LoadGsa("res/uires/patch/patchupleft.gsa");
        pUR = r.LoadGsa("res/uires/patch/patchupright.gsa");
        pDL = r.LoadGsa("res/uires/patch/patchdownleft.gsa");
        pDR = r.LoadGsa("res/uires/patch/patchdownright.gsa");
        pUM = r.LoadGsa("res/uires/patch/patchupmiddle.gsa");
        pDM = r.LoadGsa("res/uires/patch/patchdownmiddle.gsa");
        pLM = r.LoadGsa("res/uires/patch/patchleftmiddle.gsa");
        pRM = r.LoadGsa("res/uires/patch/patchrightmiddle.gsa");
        chkOff = r.LoadGsaFrame("res/uires/common/checkbox.gsa", 0);
        chkOn = r.LoadGsaFrame("res/uires/common/checkbox.gsa", 1);
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private void Lbl(float x, float y, string text, Color color)
    { if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, color, s); }
    private static void Fill(Rect r, Color color)
    { GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
    private void Draw9(Rect b)
    {
        Fill(b, new Color(26f / 255, 27f / 255, 22f / 255, 230f / 255));
        float corner = 11 * s, edge = 3 * s;
        D(pUM, new Rect(b.x + corner, b.y, b.width - corner * 2, edge));
        D(pDM, new Rect(b.x + corner, b.yMax - edge, b.width - corner * 2, edge));
        D(pLM, new Rect(b.x, b.y + corner, edge, b.height - corner * 2));
        D(pRM, new Rect(b.xMax - edge, b.y + corner, edge, b.height - corner * 2));
        D(pUL, new Rect(b.x, b.y, corner, corner));
        D(pUR, new Rect(b.xMax - corner, b.y, corner, corner));
        D(pDL, new Rect(b.x, b.yMax - corner, corner, corner));
        D(pDR, new Rect(b.xMax - corner, b.yMax - corner, corner, corner));
    }
    private static void D(Texture2D image, Rect rect) { if (image != null) GUI.DrawTexture(rect, image); }
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        var frame = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
        Vector2 saved = Event.current.mousePosition;
        if (frame.Item2) Event.current.mousePosition = new Vector2(-9999, -9999);
        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen)) { winX = (Screen.width - NW * s) * .5f; winY = (Screen.height - NH * s) * .5f; }
            Vector2 pointer = Event.current.mousePosition; GUI.depth = frame.Item1;
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else
            {
                GUI.color = new Color(.1f, .09f, .13f, .98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            Rect edit = L(23, 53, 216, 12); Fill(edit, new Color(0, 0, 0, .3f));
            GUI.SetNextControlName("tc_n");
            tname = GUI.TextField(new Rect(edit.x + 3, edit.y, edit.width - 6, edit.height), tname ?? string.Empty, 16, TEdit());
            IReadOnlyList<PartyInviteTarget> nearby = nearbySource();
            Fill(L(76, 78, 140, 90), new Color(0, 0, 0, .18f));
            if (BitmapFont.Ready)
            {
                int y = 82;
                foreach (int id in picked)
                {
                    string name = null;
                    for (int i = 0; i < nearby.Count; i++) if (nearby[i].EntityId == id) { name = nearby[i].Name; break; }
                    string label = string.IsNullOrEmpty(name) ? "玩家#" + id : name;
                    BitmapFont.DrawOutline(winX + 80 * s, winY + y * s, label, new Color(.6f, .9f, 1, 1), s);
                    y += 14; if (y == 166) break;
                }
                if (picked.Count == 0) BitmapFont.DrawOutline(winX + 80 * s, winY + 86 * s,
                    "点【添加】选附近的人", new Color(.6f, .6f, .55f, 1), s);
            }
            if (SocUI.Btn(L(21, 93, 44, 22), icoAdd, "添加", pointer, s)) showNeighbor = !showNeighbor;
            if (showNeighbor) DrawNearby(nearby, pointer);
            if (SocUI.Btn(L(38, 185, 70, 22), icoApply, "确定", pointer, s) && NetClient.I != null)
            {
                NetClient.I.PartyCreateForIndependentRuntime(string.IsNullOrWhiteSpace(tname) ? string.Empty : tname.Trim());
                foreach (int id in picked) NetClient.I.PartyInviteForIndependentRuntime(id, string.Empty);
                pendingInvite = 0; picked.Clear(); showNeighbor = false; visible = false;
            }
            if (SocUI.Btn(L(155, 185, 70, 22), icoCancel, "取消", pointer, s)) visible = false;
            Rect close = L(230, 6, 20, 19); D(icoClose, close);
            if (SocUI.Click(close, pointer)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 28), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(pointer) && !SysMsgBox.Visible) Event.current.Use();
        }
        finally { if (frame.Item2) Event.current.mousePosition = saved; }
    }
    private void DrawNearby(IReadOnlyList<PartyInviteTarget> nearby, Vector2 pointer)
    {
        // Official OnGUI 06000d5b carries this count and panel origin through the popup.
        int count = nearby.Count;
        int rows = Mathf.Clamp(count, 1, NbFitRows), max = Mathf.Max(0, count - rows);
        neighborScroll = Mathf.Clamp(neighborScroll, 0, max);
        Rect box = L(NbBoxX, NbBoxY, NbBoxW, rows * NbRowH + NbPadV);
        Draw9(box);
        if (Event.current.type == EventType.ScrollWheel && box.Contains(pointer) && max > 0)
        { neighborScroll = Mathf.Clamp(neighborScroll + (int)Mathf.Sign(Event.current.delta.y), 0, max); Event.current.Use(); }
        if (count == 0)
        {
            if (BitmapFont.Ready) BitmapFont.DrawOutline(box.x + 8 * s, box.y + 8 * s,
                "附近无玩家", new Color(.6f, .6f, .55f, 1), s);
        }
        else for (int i = 0; i < rows && i + neighborScroll < count; i++)
        {
            PartyInviteTarget item = nearby[i + neighborScroll];
            float rowScale = s;
            bool selected = picked.Contains(item.EntityId);
            Rect row = new Rect(box.x + 5 * rowScale, box.y + (NbPadV * .5f + i * NbRowH) * rowScale,
                (max > 0 ? 104 : 116) * rowScale, NbRowH * rowScale);
            if (row.Contains(pointer)) Fill(row, new Color(1, 1, 1, .12f));
            Texture2D image = selected ? chkOn : chkOff;
            float checkScale = s;
            Rect check = new Rect(row.x + 2 * checkScale, row.y + 3.5f * checkScale, 14 * checkScale, 13 * checkScale);
            if (image != null) GUI.DrawTexture(check, image);
            else Fill(check, selected ? new Color(.4f, .8f, 1, 1) : new Color(.3f, .3f, .3f, 1));
            if (BitmapFont.Ready) BitmapFont.DrawOutline(row.x + 20 * s, row.y + 4 * s, item.Name, Color.white, s);
            if (SocUI.Click(row, pointer)) { if (selected) picked.Remove(item.EntityId); else picked.Add(item.EntityId); }
        }
        if (max > 0) NativeScroll.BarRows(this, new Rect(box.xMax - 15 * s, box.y + 4 * s, 12 * s, rows * 20 * s), rows, count, ref neighborScroll);
    }
    internal static TeamConfigDlg BindForIndependentRuntime(object borrower, IPartyCommands commands,
        Func<IReadOnlyList<PartyInviteTarget>> nearbySource)
    {
        if (borrower == null) throw new ArgumentNullException(nameof(borrower));
        if (commands == null) throw new ArgumentNullException(nameof(commands));
        if (nearbySource == null) throw new ArgumentNullException(nameof(nearbySource));
        Ensure();
        I.borrower = borrower;
        I.commands = commands;
        I.nearbySource = nearbySource;
        return I;
    }
    internal bool IsBoundForIndependentRuntime(object bound) => ReferenceEquals(borrower, bound);
    internal int PendingInviteForIndependentRuntime => pendingInvite;
    internal IReadOnlyCollection<int> PickedForIndependentRuntime => picked;
    internal void DetachForIndependentRuntime(object bound)
    {
        if (!ReferenceEquals(borrower, bound)) return;
        borrower = null;
        nearbySource = EmptyNearbySource;
        // Only the map's nearby supplier is borrowed. Keep the draft, picks, placement,
        // resources and session commands while the native root drives its own OnGUI.
    }
}
}
