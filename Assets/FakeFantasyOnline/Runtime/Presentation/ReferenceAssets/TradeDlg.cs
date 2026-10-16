using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 TradeDlg type 0x0200027A (MonoBehaviour, static singleton I at static field
// offset 0). This file restores the window's lifecycle, state owner and - in WBB-024 - its
// remaining official window body:
//
//   TradeDlg.Ensure       0x06000F5D  static void Ensure()
//   TradeDlg.Open         0x06000F5E  static void Open()
//   TradeDlg.Hide         0x06000F5F  static void Hide()
//   TradeDlg.Toggle       0x06000F60  static void Toggle()
//   TradeDlg.get_Visible  0x06000F61  static bool get_Visible()
//   TradeDlg.MouseOver    0x06000F62  static bool MouseOver()
//   TradeDlg.Load         0x06000F63  instance void Load()
//   TradeDlg.L            0x06000F64  instance Rect L(float, float, float, float)
//   TradeDlg.Lbl          0x06000F65  instance void Lbl(float, float, string, Color)
//   TradeDlg.MyMoneyInput 0x06000F66  instance long MyMoneyInput()
//   TradeDlg.SendOffer    0x06000F67  instance void SendOffer()
//   TradeDlg.OnGUI        0x06000F68  instance void OnGUI()
//   TradeDlg.DrawItems    0x06000F69  instance void DrawItems(Rect, List<(int,int,string)>, bool, Vector2)
//   TradeDlg.Edit         0x06000F6A  instance GUIStyle Edit()
//   TradeDlg.MoneyField   0x06000F6B  instance string MoneyField(string, Rect, string)
//   TradeDlg.Coin         0x06000F6C  instance void Coin(float, float, Texture2D, long)
//   TradeDlg..ctor        0x06000F6D  instance void .ctor()
//
// Evidence: the frozen arm64 + x86_64 GameAssembly exports in
// Build/ReferenceReverseGhidra-1.5.8/queries/wbb023-tradedlg-{arm64,x86_64}. Both bodies of every
// member above agree field-for-field; every string literal below was read out of
// Build/ReferenceReverseIndex-1.5.8/usages-arm64.tsv by its exact metadata_usage_*_kind5 symbol.
//
// Identity note: 0x06000F62 is TradeDlg.MouseOver, NOT get_Visible - the atlas names the
// visibility accessor 0x06000F61 and MouseOver 0x06000F62. TradeDlg (0x0200027A, NW 569 / NH 426)
// is a separate official type from DealDlg (0x0200027C, NW 454 / NH 272); the two must not be
// merged even though DealDlg already exists in the product.
//
// NW/NH are official const (literal) floats: arm64 inlines -569.0 / 569.0 / 426.0 in
// TradeDlg.MouseOver (0x06000F62) and x86_64 reads the same magnitudes from its float pool at
// 0x111A740 = -569.0f, 0x111A73C = 569.0f, 0x1117D50 = 426.0f. TradeDlg has no .cctor, which is
// what a const - rather than a static readonly - produces.
//
// Wiring boundary: no official member reaches TradeDlg.Open or TradeDlg.Hide in the 1.5.8 call
// graph (the dispatch-driven trade window is DealDlg: NetClient.Dispatch 0x0600077F calls
// DealDlg.Open 0x06000F72 and DealDlg.Hide 0x06000F73), so no product entry point opens this
// window yet. Its two official consumers are wired: GameHud.MouseOverHud (0x06000417 ->
// MouseOver 0x06000F62) via HudWindowSlot.Trade, and UiEsc.CloseTop (0x06000FCA ->
// get_Visible/Toggle) as the ordinary-world escape destination.
//
// The three protocol commands the window emits are the official NetClient members
//   NetClient.TradeOffer    0x06000751  (long money, List<(byte,int,int)> items)
//   NetClient.TradeLock     0x06000752  (bool locked)
//   NetClient.TradeConfirm  0x06000753  ()
//   NetClient.TradeCancel   0x06000754  ()
// which are owned by NetClient.cs in this same assembly and follow the established Presentation
// direct-writer shape: the member allocates nothing and forwards to a writer delegate that
// ReferenceCharacterSkillUsePort binds to the Application trade commands. The Product still never
// sends real trade traffic on its own - the writer is null until a host binds it.
internal sealed partial class TradeDlg : MonoBehaviour
{
    public static TradeDlg I;

    private const float NW = 569f, NH = 426f;

    // 0x06000F6D .ctor: field-initializer order is winX (+100 = float.NaN), _wdGen (+0x6C = -1),
    // myOffer (+0x70 = new List), gold (+0x78 = "0"), silver (+0x80 = "0"), copper (+0x88 = "0"),
    // then UnityEngine.MonoBehaviour..ctor. winY (+0x68) and s (+0x60) take the default 0.
    private bool visible, tried;
    private Texture2D bg, icoClose, icoTrade, icoClear, icoGold, icoSilver, icoCopper;
    private float s;
    private float winX = float.NaN, winY;
    private int _wdGen = -1;
    private readonly List<(byte, int, int)> myOffer = new List<(byte, int, int)>();
    private string gold = "0", silver = "0", copper = "0";
    private GUIStyle _edit;

    // 0x06000F61 get_Visible: the interception term UiEsc.CloseTop (0x06000FCA) reads.
    public static bool Visible => I != null && I.visible;

    // 0x06000F5D Ensure: the official body builds the owner only when the static singleton is
    // still null, and marks the new GameObject DontDestroyOnLoad inside that same branch - the
    // DontDestroyOnLoad call is not reachable on the already-created path.
    private static void Ensure()
    {
        if (I != null) return;
        I = new GameObject("TradeDlg").AddComponent<TradeDlg>();
        DontDestroyOnLoad(I.gameObject);
    }

    // 0x06000F5E Open: static, and it mutates the singleton's instance state. The native store
    // order is visible (+0x20), myOffer.Clear() (_size = 0 then _version++), then the three money
    // strings at +0x88 (copper), +0x80 (silver), +0x78 (gold) - i.e. one chained assignment.
    //
    // Unreached by any official caller in 1.5.8; see the wiring boundary above.
    public static void Open()
    {
        Ensure();
        I.visible = true;
        I.myOffer.Clear();
        I.gold = I.silver = I.copper = "0";
    }

    // 0x06000F5F Hide: null-safe, and visible is the only field it touches.
    public static void Hide()
    {
        if (I == null) return;
        I.visible = false;
    }

    // 0x06000F60 Toggle: Ensure, then close only when already open - upstream UiEsc.CloseTop only
    // ever calls it behind get_Visible (0x06000F61), so the official body has no open-on-closed
    // branch either. After clearing visible it emits the trade cancel while SocialClient.TradeActive
    // is set and NetClient.I passes a Unity null test.
    public static void Toggle()
    {
        Ensure();
        if (!I.visible) return;
        I.visible = false;
        if (SocialClient.TradeActive && NetClient.I != null) NetClient.I.TradeCancelForIndependentRuntime();
    }

    // 0x06000F62 MouseOver: the pointer shape GameHud.MouseOverHud (0x06000417) probes. The
    // official test is not a Rect.Contains - it is a strict half-open box
    // (x <= px < x + 569*s and y <= py < y + 426*s) computed in Rect.contains' own comparison
    // order, which is why NaN in either coordinate answers false instead of throwing.
    // GUI-space, explicit physical-pointer hit testing; independent of Input.mousePosition.
    internal static bool MobileContainsPointer(Vector2 point)
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        float width = NW, height = NH;
        bool centered = float.IsNaN(I.winX) || I._wdGen != WinDrag.Gen;
        float x = centered ? (Screen.width - width * scale) * .5f : I.winX;
        float y = centered ? (Screen.height - height * scale) * .5f : I.winY;
        if (new Rect(x, y, width * scale, height * scale).Contains(point)) return true;
        return false;
    }


    public static bool MouseOver()
        => I != null && I.ContainsPointer(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));

    internal bool ContainsPointer(Vector2 pointer)
    {
        if (!visible) return false;
        float scale = UIScale.Factor;
        float x = float.IsNaN(winX) ? (Screen.width - NW * scale) * .5f : winX;
        float y = float.IsNaN(winX) ? (Screen.height - NH * scale) * .5f : winY;
        return pointer.x >= x && pointer.x < x + NW * scale && pointer.y >= y && pointer.y < y + NH * scale;
    }

    // 0x06000F63 Load: tried is set first and cleared again on the ResLoader.I == null path. The
    // seven ResLoader.LoadGsa calls run in native store order over +0x28..+0x58; the literal
    // indices are 15951 15719 15950 15949 15716 15729 15713 in the arm64 usage table.
    private void Load()
    {
        tried = true;
        ResLoader r = ResLoader.I;
        if (r == null) { tried = false; return; }
        bg = r.LoadGsa("res/uires/tradedlg/tradedlgbk.gsa");
        icoClose = r.LoadGsa("res/uires/common/miniclose.gsa");
        icoTrade = r.LoadGsa("res/uires/tradedlg/tradebtn.gsa");
        icoClear = r.LoadGsa("res/uires/tradedlg/clearbtn.gsa");
        icoGold = r.LoadGsa("res/uires/common/gold.gsa");
        icoSilver = r.LoadGsa("res/uires/common/silver.gsa");
        icoCopper = r.LoadGsa("res/uires/common/copper.gsa");
    }

    // 0x06000F64 L. The x86_64 export is a register-only forwarding stub whose body the decompiler
    // cannot settle; the arm64 body is winX + x * s for the X component and Lbl (0x06000F65)
    // recomputes winY + y * s for the Y component, so the same transform is used here.
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);

    // 0x06000F65 Lbl: draws nothing unless BitmapFont.get_Ready (0x060000b0) is set.
    private void Lbl(float x, float y, string text, Color color)
    { if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, color, s); }

    // 0x06000F66 MyMoneyInput: three independent TryParse calls into local out slots in the order
    // gold (+0x78), silver (+0x80), copper (+0x88), each pushed through Math.Max(0, value) AFTER
    // parsing, then combined as gold * 1000000 + silver * 1000 + copper. The comparison against the
    // caller's current purse happens in OnGUI/SendOffer, not here.
    private long MyMoneyInput()
    {
        long.TryParse(gold, out long g);
        long.TryParse(silver, out long a);
        long.TryParse(copper, out long c);
        return Math.Max(0, g) * 1000000L + Math.Max(0, a) * 1000L + Math.Max(0, c);
    }

    // 0x06000F67 SendOffer: a Unity null test on NetClient.I gates the whole body; the money
    // argument is evaluated by MyMoneyInput (0x06000F66) and the offer list is the LIVE myOffer
    // field (+0x70) - the instance is passed through, not a copy taken at some earlier point.
    private void SendOffer()
    {
        if (NetClient.I == null) return;
        NetClient.I.TradeOfferForIndependentRuntime(MyMoneyInput(), myOffer);
    }

    // 0x06000F6A Edit: one lazily built GUIStyle at +0x90 - GUIStyle.none base, NpcChatter.CnFont()
    // (0x06000911) as the font, MiddleLeft alignment, white normal text colour - then fontSize is
    // recomputed every call as round(12 * s) with the .NET banker's rounding the IL emits.
    private GUIStyle Edit()
    {
        if (_edit == null)
        {
            _edit = new GUIStyle(GUIStyle.none) { font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleLeft };
            _edit.normal.textColor = Color.white;
        }
        _edit.fontSize = Mathf.RoundToInt(12 * s);
        return _edit;
    }

    // 0x06000F6B MoneyField: translucent black backdrop, then GUI.SetNextControlName, then
    // GUI.TextField over the rect inset by 2 on the left and 4 in width with maxLength 8 and the
    // Edit() style. The prior value (null coalesced to "0") seeds the field.
    private string MoneyField(string name, Rect r, string value)
    {
        GUI.color = new Color(0, 0, 0, .3f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.SetNextControlName(name);
        return GUI.TextField(new Rect(r.x + 2, r.y, r.width - 4, r.height), value ?? "0", 8, Edit());
    }

    // 0x06000F6C Coin: optional icon at 11x11 world units, then the value string drawn with the
    // shared BitmapFont colour starting 13 units to the right of the icon.
    private void Coin(float x, float y, Texture2D image, long value)
    {
        if (image != null) GUI.DrawTexture(L(x, y, 11, 11), image);
        Lbl(x + 13, y, value.ToString(), TradeUI.GOLD);
    }

    private static Color LockedColor => new Color(.5f, 1, .5f, 1);

    // 0x06000F69 DrawItems: 5 columns, 40-unit cell pitch, 36-unit icon box. The icon comes from
    // NetClient.LoadItemIcon (0x06000714); the fallback is the first two characters of the item
    // name. The wash tag uses the full 36-unit cell; icons are inset by 4 units. Hovering a pet
    // on either side returns immediately after its tooltip. Ordinary tooltips use the matching
    // received ItemInfo quality/description, then continue to the click-removal check. On the mine side a click removes that row from myOffer and
    // re-sends the offer. The tail then consumes the held inventory item, and only the bound-item
    // branch warns in chat (0x060000e9).
    private void DrawItems(Rect area, List<(int, int, string)> items, bool mine, Vector2 mouse)
    {
        float step = 40 * s, size = 36 * s;
        for (int i = 0; i < items.Count; i++)
        {
            float cx = area.x + (i % 5) * step + 4 * s;
            float cy = area.y + (i / 5) * step + 4 * s;
            float box = size - 8 * s;
            var item = items[i];
            Texture2D icon = NetClient.LoadItemIcon(item.Item1);
            if (icon != null)
            {
                ItemIconUI.Draw(new Rect(cx + 4 * s, cy + 4 * s, box, box), icon, s);
            }
            else if (BitmapFont.Ready)
            {
                string label = item.Item3.Length > 2 ? item.Item3.Substring(0, 2) : item.Item3;
                BitmapFont.DrawOutline(cx + 3 * s, cy + 12 * s, label, TradeUI.GOLD, s);
            }
            if (item.Item2 > 1 && BitmapFont.Ready)
                BitmapFont.DrawOutline(cx + size - 14 * s, cy + size - 13 * s, item.Item2.ToString(), Color.white, s);
            GameHud.DrawWashScrollTag(new Rect(cx, cy, size, size), item.Item1, s);
            if (cx <= mouse.x && mouse.x < cx + size && cy <= mouse.y && mouse.y < cy + size)
            {
                var pets = mine ? SocialClient.MyItemPets : SocialClient.OtherItemPets;
                if (pets.TryGetValue(i, out PetBrief pet))
                {
                    PetInfoTooltip.Show(pet, Event.current.mousePosition, s);
                    return; // Native return skips every later cell and the held-item tail.
                }
                var info = mine ? SocialClient.MyItemInfo : SocialClient.OtherItemInfo;
                string description = i < info.Count ? info[i].Item1 : string.Empty;
                byte quality = i < info.Count ? info[i].Item2 : (byte)0;
                string title = quality != 0
                    ? ((char)('0' + quality)).ToString() + items[i].Item3
                    : items[i].Item3;
                if (!string.IsNullOrEmpty(title)) title = "\u0007" + title;
                UITip.Show(string.IsNullOrEmpty(description) ? title : title + "\n" + description, s);
            }
            if (mine && !SocialClient.MyLocked && cx <= mouse.x && mouse.x < cx + size &&
                cy <= mouse.y && mouse.y < cy + size &&
                Event.current.type == EventType.MouseDown && Event.current.button == 0 && i < myOffer.Count)
            {
                myOffer.RemoveAt(i); SendOffer(); Event.current.Use();
            }
        }
        if (!mine || SocialClient.MyLocked || !InventoryDlg.HasHeld || !area.Contains(mouse) ||
            Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
        int tab = InventoryDlg.HeldTab, slot = InventoryDlg.HeldBagSlot;
        // The confirmation captures this first-match snapshot, including the missing-row default.
        int count = 1;
        foreach (InventoryDlg.Item entry in InventoryDlg.Items[tab])
            if (entry.bagIdx == slot) { count = entry.count; break; }
        if (!InventoryDlg.WarnIfLocked(tab, slot, "交易"))
        {
            bool bound = false;
            foreach (InventoryDlg.Item entry in InventoryDlg.Items[tab])
                if (entry.bagIdx == slot) { bound = entry.bound; break; }
            if (!bound)
            {
                InventoryDlg.Item item = InventoryDlg.ItemAt(tab, slot);
                byte capturedTab = (byte)tab;
                int capturedSlot = slot, capturedCount = count;
                InventoryDlg.ClearHeld(); Event.current.Use();
                FusedGuard.Confirm(item, "放到交易栏", () =>
                {
                    // 0x06000F6F checks the live offer count after duplicate detection.
                    if (!myOffer.Exists(offer => offer.Item1 == capturedTab && offer.Item2 == capturedSlot) && myOffer.Count < 20)
                        myOffer.Add((capturedTab, capturedSlot, capturedCount));
                    SendOffer();
                });
                return;
            }
            ChatUI.Add("系统", "该物品已绑定, 不能交易(可用天工剪解绑)", new Color(1, 180f / 255, 120f / 255, 1));
        }
        InventoryDlg.ClearHeld(); Event.current.Use();
    }

    // 0x06000F68 OnGUI: returns immediately while !visible, lazily Loads, latches s = UIScale.Factor,
    // re-centres through WinDrag.Stale (0x0600107c) when the window is stale, takes the depth and
    // "covered" flag from WinZ.Frame (0x0600107f, base 10) and mirrors the pointer while another
    // window covers this one. Draw order is background, the self label at (40,26) and the partner
    // name at (320,26), the 250x300 item panels at (20,48)/(299,48), the lock captions, the money reads, the
    // three MoneyFields, then - after a change is written back into the fields - SendOffer, the action
    // button (SendOffer + TradeLock, or TradeConfirm, or TradeLock(false)), the clear button
    // (myOffer.Clear + gold=silver=copper="0" + SendOffer), the close control (TradeCancel then
    // visible = false), WinDrag.Handle, and finally the body Use() that swallows the click.
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen)) { winX = (Screen.width - NW * s) * .5f; winY = (Screen.height - NH * s) * .5f; }
        var frame = WinZ.Frame(this, L(0, 0, NW, NH), 10);
        Vector2 saved = Event.current.mousePosition;
        if (frame.Item2) Event.current.mousePosition = new Vector2(-9999, -9999);
        try
        {
            Vector2 mouse = Event.current.mousePosition;
            GUI.depth = frame.Item1;
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else
            {
                GUI.color = new Color(.09f, .08f, .12f, .98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            Lbl(40, 26, CharData.name ?? "我", TradeUI.GOLD);
            Lbl(320, 26, SocialClient.OtherName, new Color(.8f, .9f, 1, 1));
            Rect mineArea = L(20, 48, 250, 300);
            GUI.color = new Color(0, 0, 0, .16f);
            GUI.DrawTexture(mineArea, Texture2D.whiteTexture);
            GUI.color = Color.white;
            Rect otherArea = L(299, 48, 250, 300);
            GUI.color = new Color(0, 0, 0, .16f);
            GUI.DrawTexture(otherArea, Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawItems(mineArea, SocialClient.MyItems, true, mouse);
            DrawItems(otherArea, SocialClient.OtherItems, false, mouse);
            if (SocialClient.MyLocked) Lbl(28, 50, "✔已锁定", LockedColor);
            if (SocialClient.OtherLocked) Lbl(307, 50, "✔已锁定", LockedColor);
            if (InventoryDlg.HasHeld) Lbl(28, 333, "← 点此放入交易", TradeUI.GOLD);
            else Lbl(28, 333, "打开背包(I)拿起物品→点此放入", TradeUI.GRAY);
            long otherMoney = SocialClient.OtherMoney;
            long otherGold = otherMoney / 1000000;
            Coin(341, 358, icoGold, otherGold);
            Coin(377, 358, icoSilver, (int)(otherMoney - otherGold * 1000000) / 1000);
            Coin(413, 358, icoCopper, otherMoney % 1000);
            if (icoGold != null) GUI.DrawTexture(L(341, 383, 11, 11), icoGold);
            string g = MoneyField("t_g", L(354, 383, 34, 12), gold);
            if (icoSilver != null) GUI.DrawTexture(L(390, 383, 11, 11), icoSilver);
            string a = MoneyField("t_s", L(403, 383, 26, 12), silver);
            if (icoCopper != null) GUI.DrawTexture(L(431, 383, 11, 11), icoCopper);
            string c = MoneyField("t_c", L(444, 383, 26, 12), copper);
            if (g != gold || a != silver || c != copper)
            {
                gold = g; silver = a; copper = c;
                if (SocialClient.TradeActive && !SocialClient.MyLocked) SendOffer();
            }
            string action = !SocialClient.MyLocked
                ? "锁定报价"
                : (SocialClient.OtherLocked
                    ? (SocialClient.MyConfirmed ? "等待对方" : "成交")
                    : "等待对方");
            if (TradeUI.Btn(L(296, 400, 90, 22), icoTrade, action, mouse, s))
            {
                // The action retains this receiver even if SendOffer replaces NetClient.I.
                NetClient client = NetClient.I;
                if (client != null)
                {
                    if (!SocialClient.MyLocked) { SendOffer(); client.TradeLockForIndependentRuntime(true); }
                    else if (SocialClient.OtherLocked && !SocialClient.MyConfirmed) client.TradeConfirmForIndependentRuntime();
                    else if (!SocialClient.OtherLocked) client.TradeLockForIndependentRuntime(false);
                }
            }
            if (TradeUI.Btn(L(386, 400, 70, 22), icoClear, "清空", mouse, s))
            {
                myOffer.Clear();
                gold = silver = copper = "0";
                SendOffer();
            }
            Rect closeArea = L(543, 21, 20, 19);
            if (icoClose != null) GUI.DrawTexture(closeArea, icoClose);
            if (TradeUI.Click(closeArea, mouse))
            {
                if (NetClient.I != null) NetClient.I.TradeCancelForIndependentRuntime();
                visible = false;
            }
            WinDrag.Handle(this, L(0, 0, NW, 28), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mouse) && !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally { if (frame.Item2) Event.current.mousePosition = saved; }
    }
}
}
