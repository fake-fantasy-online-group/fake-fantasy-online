using System;
using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 TeamDlg, tokens 06000ea1–06000eba. The host connects existing
// command and child-window lifetimes; all page, selection and action decisions live here.
internal sealed partial class TeamDlg : MonoBehaviour
{
    public static TeamDlg I;
    private const float NW = 187f, NH = 374f;
    private static readonly Color GOLD = new Color(1f, .92f, .66f, 1f);
    private static readonly Color GRAY = new Color(.66f, .64f, .55f, 1f);
    private sealed class Tab
    {
        public string label, button, title;
        public (string, float, float)[] actions;
        public Tab(string label, string button, string title, params (string, float, float)[] actions)
        { this.label = label; this.button = button; this.title = title; this.actions = actions; }
    }
    private static readonly Tab[] TABS =
    {
        new Tab("好友", "friendbtn", "friendtitle", ("chatbtn", 2, 26), ("delbtn", 60, 26)),
        new Tab("队伍", "teambtn", "teamtitle", ("followbtn", 2, 2), ("createbtn", 2, 26), ("leavebtn", 60, 26)),
        new Tab("家族", "unionbtn", "uniontitle", ("chatbtn", 2, 26)),
        new Tab("师徒", "masterbtn", "mastertitle", ("chatbtn", 2, 26), ("thankbtn", 60, 26)),
        new Tab("屏蔽", "filterbtn", "filtertitle", ("delbtn", 60, 26))
    };
    private bool visible, tried;
    private int tab = 1, masterSub;
    private string addName = string.Empty;
    private int selFriend = -1, selParty = -1, friendScroll, selBlock = -1, blockScroll;
    private Texture2D pUp, pMid, pDn, icoClose, icoConfig, icoAdd;
    private readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1;
    private GUIStyle _edit;
    private TeamDialogRuntime runtime;
    private TeamDialogRuntime borrower;

    public static void Ensure()
    {
        if (I != null) return;
        I = new GameObject("TeamDlg").AddComponent<TeamDlg>();
        DontDestroyOnLoad(I.gameObject);
    }
    public static void Toggle()
    {
        Ensure();
        I.visible = !I.visible;
        if (I.visible) { I.tab = 1; NetClient.I?.ApprenticeListForIndependentRuntime(); }
    }
    public static void OpenParty() { Ensure(); I.visible = true; I.tab = 1; }
    public static bool Visible => I != null && I.visible;
    private static bool ChildModalOpen => SysMsgBox.Visible || TeamConfigDlg.Visible || FriendConfigDlg.Visible;
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * .5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * .5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(pointer);
    }
    internal bool ContainsPointer(Vector2 pointer)
    {
        if (!visible) return false;
        float scale = UIScale.Factor;
        float x = float.IsNaN(winX) ? (Screen.width - NW * scale) * .5f : winX;
        float y = float.IsNaN(winX) ? (Screen.height - NH * scale) * .5f : winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(pointer);
    }
    internal static TeamDlg BindForIndependentRuntime(TeamDialogRuntime bound)
    {
        if (bound == null) throw new ArgumentNullException(nameof(bound));
        Ensure();
        I.runtime = bound;
        I.borrower = bound;
        // Unity remains the sole OnGUI driver, including the interval between map hosts.
        return I;
    }
    internal bool IsBoundForIndependentRuntime(TeamDialogRuntime bound) => ReferenceEquals(borrower, bound);
    internal void DetachForIndependentRuntime(TeamDialogRuntime bound)
    {
        if (!ReferenceEquals(borrower, bound)) return;
        borrower = null;
        // Commands and native singleton endpoints are session-scoped. Keep them for pending
        // confirmations and automatic callbacks; no callback here captures the disposed map.
        // Native060003f3/060003f7 owns final root destruction, with Unity-null recreation.
    }
    private static string Elide(string name, string suffix, float maxBaseW)
    {
        name = name ?? string.Empty; suffix = suffix ?? string.Empty;
        if (BitmapFont.Measure(name + suffix, 1f) <= maxBaseW) return name + suffix;
        while (name.Length > 1 && BitmapFont.Measure(name + "…" + suffix, 1f) > maxBaseW)
            name = name.Substring(0, name.Length - 1);
        return name + "…" + suffix;
    }
    private Texture2D TA(string path, int frame)
    {
        string key = path + "#" + frame;
        if (tex.TryGetValue(key, out Texture2D result)) return result;
        result = ResLoader.I?.LoadGsaFrameA(path, frame); tex[key] = result; return result;
    }
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        pUp = loader.LoadGsa("res/uires/teamdlg/teamdlgup.gsa");
        pMid = loader.LoadGsa("res/uires/teamdlg/teamdlgmid.gsa");
        pDn = loader.LoadGsa("res/uires/teamdlg/teamdlgdn.gsa");
        icoClose = loader.LoadGsaFrame("res/uires/common/miniclose.gsa", 0);
        icoConfig = loader.LoadGsaFrame("res/uires/teamdlg/configbtn.gsa", 0);
        icoAdd = loader.LoadGsaFrame("res/uires/common/addbtn.gsa", 0);
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private GUIStyle Edit()
    {
        if (_edit == null)
        {
            _edit = new GUIStyle(GUIStyle.none) { font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleLeft };
            _edit.normal.textColor = Color.white;
        }
        _edit.fontSize = Mathf.RoundToInt(12f * s); return _edit;
    }
    private static void Fill(Rect r, Color c)
    { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
    private void Center(Rect r, string text, Color c)
    { if (BitmapFont.Ready) BitmapFont.CenterOutline(r, text, c, s); }
    private void Text(float x, float y, string text, Color c)
    { if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, c, s); }
    private static bool Click(Rect r, Vector2 pointer) => ClickButton(r, pointer, 0);
    private static bool RClick(Rect r, Vector2 pointer) => ClickButton(r, pointer, 1);
    private static bool ClickButton(Rect r, Vector2 pointer, int button)
    {
        if (ChildModalOpen || !r.Contains(pointer) || Event.current.type != EventType.MouseDown || Event.current.button != button) return false;
        Event.current.Use(); return true;
    }
    private bool BtnImg(Rect r, Texture2D image, string text, Vector2 pointer)
    {
        bool hover = r.Contains(pointer) && !ChildModalOpen;
        if (image != null)
        { GUI.color = hover ? new Color(1.25f, 1.25f, 1.25f, 1f) : Color.white; GUI.DrawTexture(r, image); GUI.color = Color.white; }
        else
        { Fill(r, hover ? new Color(.55f, .42f, .18f, .95f) : new Color(.3f, .26f, .16f, .88f)); Center(r, text, GOLD); }
        if (hover && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        { Event.current.Use(); return true; }
        return false;
    }
    private void Highlight(Rect row, bool selected, Vector2 pointer)
    {
        if (selected) Fill(row, new Color(.4f, .55f, .9f, .35f));
        else if (row.Contains(pointer)) Fill(row, new Color(1f, 1f, 1f, .1f));
    }
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        var frame = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
        Vector2 saved = Event.current.mousePosition;
        if (frame.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen))
            { winX = (Screen.width - NW * s) * .5f; winY = (Screen.height - NH * s) * .5f; }
            Vector2 pointer = Event.current.mousePosition;
            GUI.depth = frame.Item1;
            if (pUp != null && pMid != null && pDn != null)
            {
                float up = TexScale.H(pUp), down = TexScale.H(pDn);
                GUI.DrawTexture(L(0, 0, NW, up), pUp);
                GUI.DrawTexture(L(0, up, NW, NH - up - down), pMid);
                GUI.DrawTexture(L(0, NH - down, NW, down), pDn);
            }
            else { Fill(L(0, 0, NW, NH), new Color(.08f, .07f, .11f, .96f)); Fill(L(0, 0, NW, 2), new Color(.4f, .34f, .2f, 1)); }
            Center(L(58, 21, 75, 12), "交友", GOLD);
            for (int i = 0; i < TABS.Length; i++)
            {
                Rect r = L(13 + 33 * i, 39, 33, 21);
                Texture2D image = TA("res/uires/teamdlg/" + TABS[i].button + ".gsa", i == tab ? 1 : 0);
                if (image != null) GUI.DrawTexture(r, image);
                else { Fill(r, i == tab ? new Color(.55f, .42f, .18f, .95f) : new Color(.25f, .22f, .15f, .85f)); Center(r, TABS[i].label, GOLD); }
                if (Click(r, pointer)) tab = i;
            }
            if (tab == 3) DrawMaster(pointer);
            else
            {
                Texture2D title = TA("res/uires/teamdlg/" + TABS[tab].title + ".gsa", 0);
                if (title != null) GUI.DrawTexture(L(16, 62, 145, 20), title);
                float top = tab == 1 ? 96 : 84, height = tab == 1 ? 194 : 206;
                Rect content = L(16, top, 146, height);
                if (tab == 1) Center(L(16, 84, 146, 12), SocialClient.Party.Count == 0 ? "未组队" : string.Format("我的队伍 ({0}人)", SocialClient.Party.Count), SocialClient.Party.Count == 0 ? GRAY : GOLD);
                Fill(content, new Color(0, 0, 0, .22f));
                if (tab == 1 && SocialClient.Party.Count > 0) DrawParty(pointer, top);
                else if (tab == 0) DrawFriends(pointer, content, top, height);
                else if (tab == 4) DrawBlocks(pointer, content, top, height);
                else if (tab == 2) Center(new Rect(content.x, content.center.y - 7 * s, content.width, 14 * s), "暂无家族", GRAY);
            }
            DrawActions(pointer);
            if (tab == 0)
            {
                Rect r = L(19, 299, 112, 12); Fill(r, new Color(0, 0, 0, .3f));
                GUI.SetNextControlName("team_add");
                addName = GUI.TextField(new Rect(r.x + 3, r.y, r.width - 6, r.height), addName ?? string.Empty, 12, Edit());
                if (BtnImg(L(135, 293, 44, 22), icoAdd, "加", pointer) && NetClient.I != null && !string.IsNullOrWhiteSpace(addName))
                { NetClient.I.FriendReqForIndependentRuntime(addName.Trim()); addName = string.Empty; }
            }
            if (BtnImg(L(8, 321, 20, 49), icoConfig, "设", pointer))
            { if (tab == 0) FriendConfigDlg.ToggleForIndependentRuntime(); else runtime?.ToggleTeamConfig(); }
            Rect close = L(162, 15, 20, 19);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (Click(close, pointer)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 28), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(pointer) && !ChildModalOpen) Event.current.Use();
        }
        finally { if (frame.Item2) Event.current.mousePosition = saved; }
    }
}

// Adapts independently owned subsystems only. This carries no gameplay decisions.
internal sealed class TeamDialogRuntime
{
    public IPartyCommands PartyCommands;
    public IFriendCommands FriendCommands;
    public IBlockCommands BlockCommands;
    public IApprenticeCommands ApprenticeCommands;
    public Action ToggleTeamConfig;
    public Action<string, int> OpenTeamConfig;
    public Action<string> OpenGossip;
    public Func<string> LocalName;
    public Func<int, bool> SelectPlayer;
}
}
