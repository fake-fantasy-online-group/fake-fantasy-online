using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class DealDlg
{
    private const float NW = 454, NH = 272;
    private bool visible, tried;
    private Texture2D bg, icoClose, icoHelp, icoAgree, icoDeal, icoCancel, icoGold, icoSilver, icoCopper, selfHead;
    private float winX = float.NaN, winY;
    private int _wdGen = -1;
    private readonly List<(byte, int, int)> myOffer = new List<(byte, int, int)>();
    private string gold = "0", silver = "0", copper = "0";
    private GUIStyle _edit;
    private float moneyWarnUntil;

    private static void Ensure()
    {
        if (I != null) return;
        GameObject owner = new GameObject("DealDlg");
        I = owner.AddComponent<DealDlg>(); DontDestroyOnLoad(owner);
    }
    public static void Open()
    {
        Ensure(); I.visible = true; I.offerSession++; I.myOffer.Clear(); I.gold = I.silver = I.copper = "0";
        I.selfHead = null; I.AttachMarks();
        // Original dependency: opening a trade opens the existing native inventory owner,
        // including a receipt before a world adapter exists. Its automatic OnGUI remains
        // the sole inventory drawer across receipt delivery and map-host handover.
        if (!InventoryDlg.IsOpen) InventoryDlg.Toggle();
    }
    public static void Hide() { if (I == null) return; I.visible = false; DealingMark.Clear(); }
    public static void Toggle()
    {
        Ensure(); if (!I.visible) { Open(); return; }
        I.visible = false; DealingMark.Clear();
        if (SocialClient.TradeActive && NetClient.I != null) NetClient.I.TradeCancelForIndependentRuntime();
    }
    public static bool Visible => I != null && I.visible;
    // Official 06000f76: the parameters are Int32, and the row's first element is narrowed to a
    // byte for the comparison exactly as the native body does.
    public static bool IsOffered(int tab, int bagIdx)
    {
        if (I == null || !I.visible || !SocialClient.TradeActive) return false;
        foreach (var offer in I.myOffer) if ((byte)offer.Item1 == tab && offer.Item2 == bagIdx) return true;
        return false;
    }
    public static bool MouseOver()
    {
        // 06000f77 gates before scale/input. Geometry rereads the native singleton after
        // those engine calls; the parameterized host helper has a different receiver contract.
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
    private void AttachMarks()
    {
        if (PlayerController.I != null) DealingMark.Attach(PlayerController.I.transform, PlayerController.I.HeadU);
        RemotePlayer other = CombatWorld.FindPlayer(SocialClient.OtherId);
        if (other != null) DealingMark.Attach(other.transform, other.HeadU);
    }
    private void Update() { if (visible) AttachMarks(); else DealingMark.Clear(); }
    private void Load()
    {
        tried = true; ResLoader r = ResLoader.I; if (r == null) { tried = false; return; }
        bg = r.LoadGsa("res/uires/dealdlg/dealdlgbk.gsa");
        icoClose = r.LoadGsa("res/uires/common/miniclose.gsa");
        icoHelp = r.LoadGsa("res/uires/gameui/helpicon.gsa");
        icoAgree = r.LoadGsa("res/uires/common/affirmbtn.gsa");
        icoDeal = r.LoadGsa("res/uires/dealdlg/dealokbtn.gsa");
        icoCancel = r.LoadGsa("res/uires/common/closebtn.gsa");
        icoGold = r.LoadGsa("res/uires/common/gold.gsa");
        icoSilver = r.LoadGsa("res/uires/common/silver.gsa");
        icoCopper = r.LoadGsa("res/uires/common/copper.gsa");
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private void Lbl(float x, float y, string text, Color color)
    { if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, color, s); }
    private long RawMoneyInput()
    {
        long.TryParse(gold, out long g); long.TryParse(silver, out long a); long.TryParse(copper, out long c);
        return unchecked(Math.Max(0, g) * 1000000L + Math.Max(0, a) * 1000L + Math.Max(0, c));
    }
    private long MyMoneyInput() => Math.Min(RawMoneyInput(), Math.Max(0L, InventoryDlg.Money));
    // Official 1.6.0 06000f89: the Unity null gate is followed by a fresh receiver read,
    // then MyMoneyInput and the live offer list. Preserve that evaluation order and the
    // independent runtime command bridge; do not introduce a copy or a second null gate.
    private void SendOffer()
    {
        if (NetClient.I == null) return;
        NetClient.I.TradeOfferForIndependentRuntime(MyMoneyInput(), myOffer);
    }
    private GUIStyle Edit()
    {
        if (_edit == null) { _edit = new GUIStyle(GUIStyle.none) { font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleLeft }; _edit.normal.textColor = Color.white; }
        _edit.fontSize = Mathf.RoundToInt(12 * s); return _edit;
    }
    private string MoneyField(string name, Rect r, string value, bool editable)
    {
        Fill(r, new Color(0, 0, 0, .3f));
        if (!editable) { Lbl((r.x - winX) / s + 2, (r.y - winY) / s, value ?? "0", TradeUI.GOLD); return value; }
        GUI.SetNextControlName(name);
        return GUI.TextField(new Rect(r.x + 2, r.y, r.width - 4, r.height), value ?? "0", 8, Edit());
    }
    private void Coin(float x, float y, Texture2D image, long value)
    { if (image != null) GUI.DrawTexture(L(x, y, 11, 11), image); Lbl(x + 13, y, value.ToString(), TradeUI.GOLD); }
    private static string MoneyText(long value)
    {
        var text = new StringBuilder();
        if (value >= 1000000) text.Append(value / 1000000).Append('金');
        if (value % 1000000 >= 1000) text.Append(value % 1000000 / 1000).Append('银');
        if (value % 1000 > 0 || text.Length == 0) text.Append(value % 1000).Append('铜');
        return text.ToString();
    }
    private static void Fill(Rect r, Color color) { GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
    private void OnGUI()
    {
        if (!visible) return; if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen)) { winX = (Screen.width - NW * s) * .5f; winY = (Screen.height - NH * s) * .5f; }
        var frame = WinZ.Frame(this, L(0, 0, NW, NH), 10);
        Vector2 saved = Event.current.mousePosition;
        if (frame.Item2) Event.current.mousePosition = new Vector2(-9999, -9999);
        try
        {
            Vector2 mouse = Event.current.mousePosition; GUI.depth = frame.Item1;
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg); else Fill(L(0, 0, NW, NH), new Color(.1f, .09f, .13f, .98f));
            if (selfHead == null && ResLoader.I != null) selfHead = TeamBar.Head(CharData.gender, CharData.head, CharData.hair);
            if (selfHead != null) GUI.DrawTexture(L(6, 6, 48, 48), selfHead, ScaleMode.ScaleToFit);
            else Fill(L(6, 6, 48, 48), new Color(0, 0, 0, .25f));
            RemotePlayer player = CombatWorld.FindPlayer(SocialClient.OtherId);
            Texture2D other = player != null ? TeamBar.Head(player.Gender, player.HairId, player.HairColor) : null;
            if (other != null) GUI.DrawTexture(L(211, 6, 48, 48), other, ScaleMode.ScaleToFit);
            else Fill(L(211, 6, 48, 48), new Color(0, 0, 0, .25f));
            Lbl(67, 37, CharData.name ?? "我", TradeUI.GOLD); Lbl(271, 37, SocialClient.OtherName, new Color(.8f, .9f, 1, 1));
            bool editable = !SocialClient.MyLocked;
            string g = MoneyField("d_g", L(42, 64, 42, 12), gold, editable);
            string a = MoneyField("d_s", L(106, 64, 42, 12), silver, editable);
            string c = MoneyField("d_c", L(170, 64, 42, 12), copper, editable);
            if (editable && (g != gold || a != silver || c != copper))
            {
                gold = g; silver = a; copper = c;
                long raw = RawMoneyInput(), maximum = Math.Max(0L, InventoryDlg.Money);
                if (raw > maximum)
                {
                    gold = (maximum / 1000000).ToString(); silver = (maximum % 1000000 / 1000).ToString(); copper = (maximum % 1000).ToString();
                    moneyWarnUntil = Time.time + 4; GUIUtility.keyboardControl = 0;
                }
                if (SocialClient.TradeActive) SendOffer();
            }
            if (Time.time < moneyWarnUntil) Lbl(42, 76, "身上只有 " + MoneyText(Math.Max(0L, InventoryDlg.Money)) + ", 已按上限填入", new Color(1, .55f, .45f, 1));
            long otherMoney = SocialClient.OtherMoney;
            Coin(250, 60, icoGold, otherMoney / 1000000);
            Coin(314, 60, icoSilver, otherMoney % 1000000 / 1000);
            Coin(378, 60, icoCopper, otherMoney % 1000);
            float itemScale = s, itemX = winX, itemY = winY;
            float itemTop = itemY + 84 * itemScale, itemWidth = 196 * itemScale, itemHeight = 160 * itemScale;
            DrawItems(new Rect(itemX + 21 * itemScale, itemTop, itemWidth, itemHeight), SocialClient.MyItems, true, mouse);
            DrawItems(new Rect(itemX + 232 * itemScale, itemTop, itemWidth, itemHeight), SocialClient.OtherItems, false, mouse);
            if (SocialClient.MyLocked) Lbl(24, 70, "✔已锁定", new Color(.5f, 1, .5f, 1));
            if (SocialClient.OtherLocked) Lbl(235, 70, "✔已锁定", new Color(.5f, 1, .5f, 1));
            if (!SocialClient.MyLocked) Lbl(21, 232, MobileClientPresentation.Enabled ? "背包选物 → 放入交易 / 撤回交易" : InventoryDlg.HasHeld ? "← 点左格放入交易" : "打开背包(I)拿物→点左格", InventoryDlg.HasHeld ? TradeUI.GOLD : TradeUI.GRAY);
            if (TradeUI.Btn(L(146, 246, 70, 22), icoAgree, SocialClient.MyLocked ? "已确认" : "确认", mouse, s) && !SocialClient.MyLocked)
            {
                NetClient client = NetClient.I;
                if (client != null) { SendOffer(); client.TradeLockForIndependentRuntime(true); }
            }
            bool canConfirm = SocialClient.MyLocked && SocialClient.OtherLocked && !SocialClient.MyConfirmed;
            if (TradeUI.Btn(L(234, 246, 70, 22), icoDeal, SocialClient.MyConfirmed ? "等待对方" : "成交", mouse, s) && canConfirm && NetClient.I != null)
                NetClient.I.TradeConfirmForIndependentRuntime();
            if (TradeUI.Btn(L(357, 246, 70, 22), icoCancel, "取消", mouse, s)) { if (NetClient.I != null) NetClient.I.TradeCancelForIndependentRuntime(); Hide(); }
            if (icoHelp != null) GUI.DrawTexture(L(408, 32, 21, 19), icoHelp);
            if (icoClose != null) GUI.DrawTexture(L(428, 32, 21, 19), icoClose);
            if (TradeUI.Click(L(428, 32, 21, 19), mouse)) { if (NetClient.I != null) NetClient.I.TradeCancelForIndependentRuntime(); Hide(); }
            WinDrag.Handle(this, L(0, 0, NW, 28), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mouse) && !SysMsgBox.Visible) Event.current.Use();
        }
        finally { if (frame.Item2) Event.current.mousePosition = saved; }
    }
    // The map borrows the original DDOL owner. Unity retains its Update/OnGUI scheduling;
    // adoption does not change an existing component's enabled state.
    internal static DealDlg BindForIndependentRuntime()
    { Ensure(); return I; }
    internal float XForIndependentRuntime => winX;
    internal float YForIndependentRuntime => winY;
    internal void ReleaseForIndependentRuntime()
    { Hide(); enabled = false; myOffer.Clear(); if (I == this) I = null; Destroy(gameObject); }
}
}
