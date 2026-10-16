using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Source-owned MailDlg list/read/compose slice using the recovered MItem owner contract.</summary>
public partial class MailDlg : MonoBehaviour
{
    private static readonly int[] SlotX;
    private static readonly int[] SlotY;
    private static readonly Color GOLD;
    private static readonly Color GRAY;
    private static readonly Color WHITE;
    private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    private bool tried;
    private float listScroll;
    private int bodyScroll;
    private long bodyCacheId;
    private float bodyCacheS;
    private string bodyCacheSrc;
    private List<string> bodyLines;
    private GUIStyle _edit;
    private float s;
    private float NW, NH;
    private float Width => Background != null ? GsaDecoder.W(Background) : view == 0 ? 418f : 287f;
    private float Height => Background != null ? GsaDecoder.H(Background) : view == 0 ? 325f : 379f;
    private bool Packed => view == 2 || view == 1 && selected >= 0 && selected < mails.Count &&
        (mails[selected].type == 1 || mails[selected].type == 2 ||
         mails[selected].items.Count > 0 || mails[selected].money > 0 || mails[selected].caiyu > 0);
    private Texture2D Background => Texture(view == 0 ? "maillistdlg" : Packed ? "bigpackdlg" : "maildlg");
    private readonly List<WorldInventoryItemState> attachmentItems = new List<WorldInventoryItemState>();
    public static MailDlg I;
    internal static bool IndependentGuiOwner;
    private IMailCommands commands;
    private Action<string, Color> notify;
    private CancellationToken lifetime;
    private Func<WorldInventoryItemState> heldItem;
    private Action clearHeld;
    private Func<WorldInventoryState> inventory;
    private byte composeType;
    public sealed class MItem
    {
        public long id;
        public string from, title, body;
        public long money, caiyu;
        public byte type;
        public bool read, canClaim, canReturn;
        public long time;
        public List<(int, int, string, string)> items;

        // .ctor（060005ab）：官方 arm64 0x4762f4（140 字节）/ x86_64 0x48d880（128 字节）双架构一致，
        // 只分配 items 列表（实例偏移 +0x50，写屏障紧跟其后）再调 System.Object..ctor；
        // 三个 string 字段在官方 .ctor 里没有写入，因此不保留 string.Empty 字段初始值。
        public MItem()
        {
            items = new List<(int, int, string, string)>();
        }
    }
    private List<MItem> mails = new List<MItem>();
    private int total, cap, newn;
    private float winX = float.NaN, winY;
    private int _wdGen = -1, selected = -1, view;
    private bool visible;
    private string toName = string.Empty, title = string.Empty, body = string.Empty;
    private string gold = "0", silver = "0", copper = "0", caiyuStr = "0";
    private readonly List<(byte, int, int)> attachments = new List<(byte, int, int)>();
    private Task<Result> pending;

    // .cctor（060005aa）：官方 arm64 0x4761e0（276 字节）/ x86_64 0x48d770（272 字节）双架构一致。
    // 两块 int[10] 走 RuntimeHelpers.InitializeArray，数据 blob 位于 <PrivateImplementationDetails>：
    // SlotX = 08173034997E3D9A4211F675C56B6EAC2433CA752712B405A5161DFA060A13F0
    // SlotY = 9A6D31B440E04B6AAFC769025D291CDD8279A2771DC455BC29AD0CE8ABC307A3
    // （字段名即数据 SHA256，已按小端 10×int 逐字节复核命中）。三个颜色常量取自
    // GameAssembly-arm64.dylib 0x1127390 / 0x11273d0 / 0x1127790 的 16 字节 Color。
    static MailDlg()
    {
        SlotX = new int[] { 21, 60, 100, 139, 178, 217, 21, 60, 100, 139 };
        SlotY = new int[] { 255, 255, 255, 255, 255, 255, 294, 294, 294, 294 };
        GOLD = new Color(1f, .92f, .66f, 1f);
        GRAY = new Color(.66f, .64f, .55f, 1f);
        WHITE = new Color(.95f, .95f, .92f, 1f);
    }

    // .ctor（060005a9）：官方 arm64 0x475ff4（492 字节）/ x86_64 0x48d580（496 字节）双架构一致。
    // 依次分配 mails / bodyLines / attach 三个列表，bodyCacheId = -1，四个收件文本字段置空串
    // （usage 10335），四个金额文本字段置 "0"（usage 11011），winX = NaN（0x7fc00000），_wdGen = -1。
    // 官方 attach 的元组元数是 6（byte,int,int,int,string,Texture2D），此处沿用产品侧 3 元
    // 组以匹配已恢复的 TryDropAttach / SendAsync 契约，差别记录在台账 note 中。
    public MailDlg()
    {
        bodyCacheId = -1;
        bodyLines = new List<string>();
        attachments = new List<(byte, int, int)>();
    }

    public static void Ensure()
    {
        if (I != null) return;
        var gameObject = new GameObject("MailDlg");
        I = gameObject.AddComponent<MailDlg>();
        DontDestroyOnLoad(gameObject);
    }

    internal static MailDlg BindIndependentRuntime(IMailCommands commands,
        Action<string, Color> notify, CancellationToken lifetime,
        Func<WorldInventoryItemState> heldItem, Action clearHeld, Func<WorldInventoryState> inventory)
    {
        IndependentGuiOwner = true;
        Ensure();
        I.commands = commands;
        I.notify = notify;
        I.lifetime = lifetime;
        I.heldItem = heldItem;
        I.clearHeld = clearHeld;
        I.inventory = inventory;
        return I;
    }

    internal void BindWindowForIndependentRuntime(Func<WorldInventoryItemState> heldItem,
        Action clearHeld, Func<WorldInventoryState> inventory)
    {
        this.heldItem = heldItem;
        this.clearHeld = clearHeld;
        this.inventory = inventory;
    }
    internal static void SetIndependentGuiOwner(bool manual)
    {
        IndependentGuiOwner = manual;
    }
    internal void ReleaseBindingsForIndependentRuntime()
    {
        BindWindowForIndependentRuntime(null, null, null);
        commands = null;
        notify = null;
        lifetime = default;
    }

    public static bool Visible => I != null && I.visible;
    internal static bool ComposeVisible => Visible && I.view == 2;
    internal static int CurrentView => I != null ? I.view : 0;
    public static void Toggle()
    {
        Ensure();
        I.visible = !I.visible;
        if (I.visible) { I.view = 0; I.Refresh(); }
        else Typing = false;
    }
    public static void Hide()
    {
        if (I == null) return;
        I.visible = false;
        Typing = false;
    }
    private void Refresh()
    {
        HasNew = false;
        if (NetClient.I != null) _ = commands.OpenMailAsync(CancellationToken.None);
    }
    public static void SetHasNew(bool hasNew)
    {
        Ensure();
        HasNew = hasNew;
        if (!hasNew) return;
        if (!Visible)
            ChatUI.Add("系统", "你收到一封新邮件(按 M 或点邮件按钮查看)",
                new Color(1f, 230f / 255f, 130f / 255f, 1f));
        if (Visible) I.Refresh();
    }
    public static void SetMailList(int total, int cap, int newn, List<MItem> list)
    {
        Ensure();
        bool hadSelection = I.selected >= 0 && I.selected < I.mails.Count;
        long selectedId = hadSelection ? I.mails[I.selected].id : 0;
        I.total = total;
        I.cap = cap;
        I.newn = newn;
        I.mails = list;
        if (!hadSelection) return;
        I.selected = I.mails.FindIndex(mail => mail.id == selectedId);
        if (I.selected < 0 && I.view == 1) I.view = 0;
    }
    // GUI-space, explicit physical-pointer hit testing; independent of Input.mousePosition.
    internal static bool MobileContainsPointer(Vector2 point)
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        float width = I.Width, height = I.Height;
        bool centered = float.IsNaN(I.winX) || I._wdGen != WinDrag.Gen;
        float x = centered ? (Screen.width - width * scale) * .5f : I.winX;
        float y = centered ? (Screen.height - height * scale) * .5f : I.winY;
        if (new Rect(x, y, width * scale, height * scale).Contains(point)) return true;
        return false;
    }


    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        float width = I.view == 0 ? 418f : 287f;
        float height = I.view == 0 ? 325f : 379f;
        float left = float.IsNaN(I.winX) ? (Screen.width - width * scale) * .5f : I.winX;
        float top = float.IsNaN(I.winX) ? (Screen.height - height * scale) * .5f : I.winY;
        Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return new Rect(left, top, width * scale, height * scale).Contains(pointer);
    }
    internal static bool ContainsPointer(Vector2 pointer, float scale)
    {
        if (!Visible) return false;
        scale = UIScale.Factor;
        float width = I.view == 0 ? 418f : 287f;
        float height = I.view == 0 ? 325f : 379f;
        float left = float.IsNaN(I.winX) ? (Screen.width - width * scale) * .5f : I.winX;
        float top = float.IsNaN(I.winX) ? (Screen.height - height * scale) * .5f : I.winY;
        return new Rect(left, top, width * scale, height * scale).Contains(pointer);
    }
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        Draw(UIScale.Factor);
    }
    private bool Draw(float scale)
    {
        if (!visible || !(scale > 0f) || float.IsNaN(scale)) return true;
        s = scale;
        bool packed = Packed;
        Texture2D background = Texture(view == 0 ? "maillistdlg" : packed ? "bigpackdlg" : "maildlg");
        NW = background != null ? GsaDecoder.W(background) : view == 0 ? 418f : 287f;
        NH = background != null ? GsaDecoder.H(background) : view == 0 ? 325f : 379f;
        if (WinDrag.Stale(winX, ref _wdGen)) { winX = (Screen.width - NW * s) * .5f; winY = (Screen.height - NH * s) * .5f; }
        var z = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
        Vector2 saved = Event.current.mousePosition;
        if (z.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            Vector2 pointer = Event.current.mousePosition;
            GUI.depth = z.Item1;
            MailDlg.Typing = false;
            if (background != null) GUI.DrawTexture(L(0, 0, NW, NH), background);
            else
            {
                GUI.color = new Color(.09f, .08f, .12f, .98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (view == 0) DrawList(pointer); else if (view == 1) DrawRead(pointer, packed); else DrawCompose(pointer);
            if (view != 0)
            {
                Rect close = L(262, 6, 20, 19);
                if (Texture("close") != null) GUI.DrawTexture(close, Texture("close"));
                if (Click(close, pointer)) ReturnToList();
            }
            WinDrag.Handle(this, L(0, 0, NW, 28), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(pointer) && !SysMsgBox.Visible) Event.current.Use();
            return true;
        }
        finally { if (z.Item2) Event.current.mousePosition = saved; }
    }
    private void DrawList(Vector2 pointer)
    {
        int mailCount = mails.Count;
        Rect viewport = L(15, 62, 370, 198);
        float rowHeight = 21f * s;
        float totalHeight = mailCount * rowHeight;
        if (viewport.Contains(pointer) && Event.current.type == EventType.ScrollWheel)
        {
            listScroll = Mathf.Clamp(listScroll + Event.current.delta.y * 18f, 0, Mathf.Max(0, totalHeight - viewport.height));
            Event.current.Use();
        }
        listScroll = Mathf.Clamp(listScroll, 0, Mathf.Max(0, totalHeight - viewport.height));
        GUI.BeginGroup(viewport);
        // 官方 MailDlg.DrawList 0600059a（arm64 0x47104c / x86_64 0x4882d0；冻结导出
        // Build/ReferenceReverseGhidra-1.5.8/queries/{arm64,x86_64}/…/packet-MailDlg/0600059a.c）
        // 在 BeginGroup 之后用 local = pointer - group.position 作为行坐标。pointer 是 Draw 调用时
        // 按值传入的坐标副本，不会因 BeginGroup 改变；行矩形在组内坐标系，故换算后再比较。
        Vector2 localPointer = pointer - viewport.position;
        try
        {
            for (int i = 0; i < mails.Count; i++)
            {
                MItem mail = mails[i];
                float rowY = i * rowHeight - listScroll;
                if (rowY + rowHeight < 0 || rowY > viewport.height) continue;
                Rect row = new Rect(0, rowY, viewport.width, rowHeight);
                if (row.Contains(localPointer))
                {
                    GUI.color = new Color(.55f, .42f, .16f, .45f);
                    GUI.DrawTexture(row, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                Color color = mail.read ? GRAY : GOLD;
                string prefix = mail.type == 1 ? "[小包]" : mail.type == 2 ? "[大包]" : mail.type == 3 ? "[系统]" : string.Empty;
                if (BitmapFont.Ready)
                {
                    float textY = rowY + 5f * s;
                    if (!mail.read) BitmapFont.DrawOutline(3f * s, textY, "●", new Color(1f, .85f, .3f), s);
                    BitmapFont.DrawOutline(15f * s, textY, Trunc(mail.from, 5), color, s);
                    BitmapFont.DrawOutline(97f * s, textY, prefix + Trunc(mail.title, 6), color, s);
                    BitmapFont.DrawOutline(215f * s, textY, TimeStr(mail.time), color, s);
                    if (mail.items.Count > 0) BitmapFont.DrawOutline(310f * s, textY, mail.items.Count.ToString(), color, s);
                    if (mail.money >= 1000000) BitmapFont.DrawOutline(341f * s, textY, (mail.money / 1000000).ToString(), color, s);
                    if (mail.caiyu > 0) BitmapFont.DrawOutline(375f * s, textY, mail.caiyu.ToString(), new Color(.7f, .9f, 1f), s);
                }
                // Consume the selection event only after its immediate mail action returns.
                if (row.Contains(localPointer) && !SysMsgBox.Visible &&
                    Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    SelectMail(i);
                    Event.current.Use();
                }
            }
        }
        finally { GUI.EndGroup(); }
        Lbl(103, 266, total.ToString(), WHITE);
        Lbl(228, 266, newn.ToString(), newn > 0 ? new Color(1f, .75f, .3f) : WHITE);
        Rect capacity = L(12, 288, 220f * (cap > 0 ? Mathf.Clamp01((float)total / cap) : 0), 8);
        GUI.color = new Color(.45f, .75f, .35f, .9f);
        GUI.DrawTexture(capacity, Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (Btn(L(9, 297, 70, 22), Texture("delbtn"), "删除", pointer) && selected >= 0 && selected < mails.Count && NetClient.I != null)
            _ = commands.ActionAsync(1, mails[selected].id, CancellationToken.None);
        if (Btn(L(79, 297, 90, 22), Texture("delallbtn"), "全部删除", pointer) && NetClient.I != null)
            _ = commands.ActionAsync(3, 0, CancellationToken.None);
        // Writing and returning mail have no matching baked-word artwork. Keep their current
        // labels on the shared blank skin instead of borrowing the unrelated send/team buttons.
        if (Btn(L(175, 297, 70, 22), null, "写  信", pointer)) StartCompose(0, string.Empty, string.Empty);
        if (Btn(L(342, 297, 70, 22), Texture("cancelbtn"), "关闭", pointer)) { visible = false; Typing = false; }
        if (Btn(L(233, 266, 90, 22), Texture("mailresistbtn"), "拒收列表", pointer)) MailResistDlg.Toggle();
        if (Btn(L(323, 266, 90, 22), Texture("configbtn"), "邮箱设置", pointer)) MailConfigDlg.Toggle();
        Rect close = L(392, 18, 20, 19);
        if (Texture("close") != null) GUI.DrawTexture(close, Texture("close"));
        if (Click(close, pointer)) { visible = false; Typing = false; }
    }

    private void SelectMail(int index)
    {
        selected = index;
        view = 1;
        bodyScroll = 0;
        MItem mail = mails[index];
        if (!mail.read && NetClient.I != null) _ = commands.ActionAsync(4, mail.id, CancellationToken.None);
    }

    private void ReturnToList() { selected = -1; view = 0; }

    private void StartCompose(byte type, string recipient, string subject)
    {
        view = 2;
        composeType = type;
        toName = recipient;
        title = subject;
        body = string.Empty;
        gold = silver = copper = caiyuStr = "0";
        attachments.Clear();
        attachmentItems.Clear();
    }

    private void DrawRead(Vector2 pointer, bool pack)
    {
        if (selected < 0 || selected >= mails.Count) { view = 0; return; }
        MItem mail = mails[selected];
        if (Texture("sender") != null) GUI.DrawTexture(L(18, 55, 39, 15), Texture("sender"));
        Lbl(66, 35, Trunc(mail.title, 16), WHITE);
        Lbl(66, 57, Trunc(mail.from, 12), WHITE);
        DrawBody(mail, pointer);
        if (pack)
            for (int i = 0; i < mail.items.Count && i < SlotX.Length; i++)
            {
                var item = mail.items[i];
                Rect slot = SlotAbsForDraw(i);
                DrawAttachment(slot, NetClient.LoadItemIcon(item.Item1), item.Item3, item.Item2, s);
                // 官方 0600059d 的 UITip.Show 无 HasHeld 门（冻结导出 475 行），紧跟图标/数量。
                if (Hit(slot)) UITip.Show(string.IsNullOrEmpty(item.Item4) ? item.Item3 : item.Item3 + "\n" + item.Item4, s);
            }
        if (pack)
        {
            Lbl(38, 336, (mail.money / 1000000).ToString(), GOLD);
            Lbl(93, 336, ((mail.money % 1000000) / 1000).ToString(), GOLD);
            Lbl(134, 336, (mail.money % 1000).ToString(), GOLD);
            if (mail.caiyu > 0) Lbl(170, 336, "彩玉" + mail.caiyu.ToString(), new Color(.7f, .9f, 1f));
        }
        float buttonX = 8;
        if (mail.canClaim)
        {
            if (Btn(L(buttonX, 354, 74, 22), Texture("ReciveAllMailBtn"), "收取", pointer) && NetClient.I != null)
                _ = commands.ActionAsync(0, mail.id, CancellationToken.None);
            buttonX += 78;
        }
        // The native action suffix reads cur again after MailAction can return synchronously.
        mail = selected >= 0 && selected < mails.Count ? mails[selected] : null;
        if (mail.canReturn)
        {
            if (Btn(L(buttonX, 354, 54, 22), null, "退回", pointer))
            {
                if (NetClient.I != null) _ = commands.ActionAsync(2, mail.id, CancellationToken.None);
                ReturnToList();
                return;
            }
            buttonX += 58;
        }
        if (mail.type != 3)
        {
            if (Btn(L(buttonX, 354, 64, 22), Texture("replybtn"), "回复", pointer))
                StartCompose(0, mail.from, "回复:" + mail.title);
            buttonX += 68;
        }
        if (Btn(L(buttonX, 354, 64, 22), Texture("delbtn"), "删除", pointer))
        {
            if (NetClient.I != null) _ = commands.ActionAsync(1, mail.id, CancellationToken.None);
            ReturnToList();
        }
    }

    private void DrawCompose(Vector2 pointer)
    {
        title = Field("m_ti", L(63, 34, 204, 12), title, 30);
        toName = Field("m_to", L(62, 56, 122, 12), toName, 16);
        body = FieldArea("m_bd", L(19, 80, 237, 167), body, 250);
        WorldInventoryItemState held = heldItem?.Invoke();
        for (int i = 0; i < SlotX.Length; i++)
        {
            Rect slot = SlotAbsForDraw(i);
            if (held != null)
            {
                GUI.color = new Color(.6f, .5f, .2f, .5f);
                GUI.DrawTexture(new Rect(slot.x, slot.y, slot.width, 2 * s), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (i < attachmentItems.Count)
            {
                WorldInventoryItemState item = attachmentItems[i];
                string name = item.Pet == null ? item.DisplayName :
                    (string.IsNullOrEmpty(item.Pet.Nickname) ? item.Pet.Species : item.Pet.Nickname) + "(魂之精)";
                DrawAttachment(slot, NetClient.LoadItemIcon(item.IconLookupId), name, attachments[i].Item3, s);
                // 官方 060005a1（arm64 0x473d30）在附件图标、数量之后对同一格子调用
                // GameHud.DrawWashScrollTag(06000431)，Int32 实参是附件元组第四字段 itemId
                // （元组布局 tab+0 / slot+4 / count+8 / itemId+0xc / name+0x10 / icon+0x18）；
                // 与读信视图不同，写信视图有这一跳而 DrawRead（0600059d）没有。
                GameHud.DrawWashScrollTag(slot, item.ItemId, s);
                // 官方提示门：只在 !InventoryDlg.HasHeld 时弹（HasHeld 循环外读一次，
                // (uVar2 & 1) == 0 才进 Hit + UITip.Show），且在 DrawWashScrollTag 之后。
                if (held == null && Hit(slot)) UITip.Show(name + " (点击移除)", s);
            }
            if (Click(slot, pointer))
            {
                if (held != null)
                {
                    TryDropAttach(held.Tab, held.BagIndex, Event.current.mousePosition);
                    clearHeld?.Invoke();
                }
                else if (i < attachmentItems.Count)
                { attachments.RemoveAt(i); attachmentItems.RemoveAt(i); }
            }
        }
        // 官方 .cctor（060005aa，arm64 0x4761e0）静态槽：+0x20 GOLD、+0x30 GRAY、+0x40 WHITE。
        // 写信视图（060005a1）这一行标签读 +0x20（GOLD），列表与读信的标签读 +0x40（WHITE）。
        Lbl(19, 240, held != null ? "← 点格子放入附件" : "附件 " + attachments.Count + "/10", GOLD);
        gold = Field("m_g", L(38, 336, 35, 12), gold, 7);
        silver = Field("m_s", L(93, 336, 24, 12), silver, 3);
        copper = Field("m_c", L(134, 336, 24, 12), copper, 3);
        // 官方 060005a1：「彩玉」的 DrawOutline 颜色实参是 0x3f333333/0x3f666666/1/1，即 (0.7, 0.9, 1, 1)，不是 WHITE。
        Lbl(168, 338, "彩玉", new Color(.7f, .9f, 1f));
        caiyuStr = Field("m_cy", L(198, 336, 50, 12), caiyuStr, 9);
        if (Btn(L(215, 354, 70, 22), Texture("cancelbtn"), "取消", pointer)) { view = 0; MailDlg.Typing = false; }
        // 官方 060005a1：发送按钮的兜底文案是 17330「寄出」，贴图仍是实例字段 +0xf0（sendbtn.gsa）。
        if (Btn(L(145, 354, 70, 22), Texture("send"), "寄出", pointer)) DoSend();
    }

    // DoSend（060005a2）：双架构证据 Build/ReferenceReverseGhidra-1.5.8/queries/WBD-118/
    // {arm64,x86_64}-5a2/060005a2.c。最前是原生门：读 NetClient.I（usage 02073 +0xb8）判空即 return
    // （不关写信窗），commands 判空只是 adapter 自身的空引用保护、不是官方门；之后才是
    // IsNullOrWhiteSpace(toName) 提示。money = 金*1e6+银*1e3+铜（各过 Math.Max(0,v)），type 默认 2、
    // 仅 (caiyu<1 && money<1 && attach.Count<1) 时归 0；发送前新建 List 逐项 Add（复制，不按引用交出
    // 窗口列表），再对 toName/title/body 逐个 Trim（判空失败走不返回 helper，不是空串兜底），
    // 然后 SendMail → view(+0x24)=0 → MailDlg.Typing(静态 +0x8)=false；发送后不清理附件与文本字段。
    private void DoSend()
    {
        if (NetClient.I == null) return;
        if (commands == null) return;
        if (string.IsNullOrWhiteSpace(toName))
        {
            notify?.Invoke("请填写收件人名称!", new Color(1f, 150f / 255f, 150f / 255f, 1f));
            return;
        }
        long money = MoneyValue(gold, silver, copper), caiyuValue = NonNegative(caiyuStr);
        byte type = money > 0 || caiyuValue > 0 || attachments.Count > 0 ? (byte)2 : (byte)0;
        var attach = new List<(byte, int, int)>();
        foreach ((byte, int, int) item in attachments) attach.Add(item);
        pending = commands.SendAsync(toName.Trim(), title.Trim(), body.Trim(), money, caiyuValue, type,
            attach, CancellationToken.None);
        view = 0;
        Typing = false;
    }
    private static long NonNegative(string value) => long.TryParse((value ?? string.Empty).Trim(), out long parsed) && parsed > 0 ? parsed : 0L;
    private static long MoneyValue(string g, string s, string c) => unchecked(NonNegative(g) * 1000000L + NonNegative(s) * 1000L + NonNegative(c));

    // CoinStr（060005a8，static）：官方 arm64 0x475edc（280 字节）/ x86_64 0x48d470（272 字节）
    // 双架构均为 string.Format(usage 16707, v/1000000, (v%1000000)/1000, v%1000) 且三个
    // long 实参先装箱。usage 16707 的字面量为 "{0}金{1}银{2}铜"。
    private static string CoinStr(long v) =>
        string.Format("{0}金{1}银{2}铜", v / 1000000, (v % 1000000) / 1000, v % 1000);

    public static bool TryDropAttach(byte tab, int slot, Vector2 pointer)
    {
        if (!ComposeVisible) return false;
        WorldInventoryItemState item = null;
        WorldInventoryState snapshot = I.inventory?.Invoke();
        if (snapshot != null)
            foreach (WorldInventoryItemState row in snapshot.Items)
                if (row.Tab == tab && row.BagIndex == slot) { item = row; break; }
        return I.TryDropResolvedAttachment(item, pointer);
    }

    private bool TryDropResolvedAttachment(WorldInventoryItemState item, Vector2 pointer)
    {
        if (!ComposeVisible) return false;
        bool overSlot = false;
        for (int i = 0; i < SlotX.Length; i++)
            if (SlotAbsForDraw(i).Contains(pointer)) { overSlot = true; break; }
        if (!overSlot) return false;
        if (item == null) return true;
        if (item.Locked)
        {
            notify?.Invoke("该物品已加锁, 无法作为附件邮寄。Shift+右键 可解锁", new Color(1f, 180f / 255f, 120f / 255f));
            return true;
        }
        if (attachments.Count >= 10)
        {
            notify?.Invoke("附件最多10件", new Color(1f, 150f / 255f, 150f / 255f));
            return true;
        }
        for (int i = 0; i < attachments.Count; i++)
            if (attachments[i].Item1 == item.Tab && attachments[i].Item2 == item.BagIndex) return true;
        attachments.Add((item.Tab, item.BagIndex, item.Pet == null ? item.Count : 1));
        attachmentItems.Add(item);
        return true;
    }

    public static Rect SlotAbs(int index)
    {
        if (I != null) return I.SlotAbsForDraw(index);
        float scale = UIScale.Factor;
        return new Rect((Screen.width - 287f * scale) * .5f + SlotX[index] * scale,
            (Screen.height - 379f * scale) * .5f + SlotY[index] * scale, 36f * scale, 36f * scale);
    }
    private Rect SlotAbsForDraw(int index)
    {
        float left = float.IsNaN(winX) ? (Screen.width - 287f * s) * .5f : winX;
        float top = float.IsNaN(winX) ? (Screen.height - 379f * s) * .5f : winY;
        return new Rect(left + SlotX[index] * s, top + SlotY[index] * s, 36f * s, 36f * s);
    }

    private string Field(string name, Rect rect, string value, int max)
    {
        GUI.color = new Color(0, 0, 0, .3f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.SetNextControlName(name);
        GUIStyle style = EditStyle();
        string next = UiInput.Field(new Rect(rect.x + 3, rect.y, rect.width - 6, rect.height),
            "mail_f_" + rect.x + "_" + rect.y, value ?? string.Empty, max, style, s);
        if (GUI.GetNameOfFocusedControl() == name) MailDlg.Typing = true;
        return next;
    }

    private string FieldArea(string name, Rect rect, string value, int max)
    {
        GUI.color = new Color(0, 0, 0, .3f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.SetNextControlName(name);
        GUIStyle style = EditStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.wordWrap = true;
        string next = UiInput.Area(new Rect(rect.x + 3, rect.y + 2, rect.width - 6, rect.height - 4),
            "mail_a_" + rect.x + "_" + rect.y, value ?? string.Empty, max, style, s);
        if (GUI.GetNameOfFocusedControl() == name) MailDlg.Typing = true;
        return next;
    }

    private GUIStyle EditStyle()
    {
        if (_edit == null)
        {
            _edit = new GUIStyle(GUIStyle.none) { font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleLeft };
            _edit.normal.textColor = Color.white;
        }
        _edit.fontSize = Mathf.RoundToInt(s * 12);
        return _edit;
    }

    private void DrawBody(MItem mail, Vector2 pointer)
    {
        Rect area = L(19, 80, 237, 167);
        GUI.color = new Color(0, 0, 0, .22f);
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (!BitmapFont.Ready) return;
        List<string> lines = BodyLines(mail.id, mail.body, 213f, s);
        int maximum = Mathf.Max(0, lines.Count - 10);
        if (Event.current.type == EventType.ScrollWheel && area.Contains(pointer))
        {
            bodyScroll = Mathf.Clamp(bodyScroll + (Event.current.delta.y < 0 ? -1 : 1), 0, maximum);
            Event.current.Use();
        }
        bodyScroll = Mathf.Clamp(bodyScroll, 0, maximum);
        for (int i = 0; i < 10 && i + bodyScroll < lines.Count; i++)
            BitmapFont.DrawOutline(winX + 23f * s, winY + (84f + i * 15f) * s,
                lines[i + bodyScroll], WHITE, s);
        NativeScroll.BarRows(this, L(264.5f, 82, 15, 163), 10, lines.Count, ref bodyScroll);
    }

    // BodyLines（0600059e）：官方 arm64 0x474ce8（340 字节）/ x86_64 0x48c290（320 字节）双架构一致。
    // 先按 bodyCacheId == id 做短路，再以 Mathf.Approximately(bodyCacheS, sc) 与
    // string 相等（bodyCacheSrc == t）复核缓存；未命中才写入三个缓存字段并调 WrapLines(t, wPx, sc)。
    private List<string> BodyLines(long id, string t, float wPx, float sc)
    {
        if (bodyCacheId == id && Mathf.Approximately(bodyCacheS, sc) && bodyCacheSrc == t)
            return bodyLines;
        bodyCacheId = id;
        bodyCacheS = sc;
        bodyCacheSrc = t;
        bodyLines = WrapLines(t, wPx, sc);
        return bodyLines;
    }

    private static List<string> WrapLines(string text, float width, float scale)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) return lines;
        var line = new StringBuilder();
        foreach (char character in text)
        {
            if (character == '\n') { lines.Add(line.ToString()); line.Clear(); continue; }
            if (line.Length > 0 && BitmapFont.Measure(line.ToString() + character, scale) > width * scale)
            { lines.Add(line.ToString()); line.Clear(); }
            line.Append(character);
        }
        if (line.Length > 0) lines.Add(line.ToString());
        return lines;
    }

    // 官方 060005a1 逐格顺序：图标 → 数量 → DrawWashScrollTag → (!HasHeld 时) Hit + UITip.Show。
    // 本 helper 只画图标与数量，提示由调用点按上述顺序紧跟（读信无 WashScrollTag 一跳）。
    private static void DrawAttachment(Rect slot, Texture2D icon, string name, int count, float scale)
    {
        if (icon != null)
            ItemIconUI.Draw(new Rect(slot.x + 4 * scale, slot.y + 4 * scale, slot.width - 8 * scale, slot.height - 8 * scale), icon, scale);
        else if (BitmapFont.Ready)
            BitmapFont.DrawOutline(slot.x + 4 * scale, slot.y + 12 * scale, Trunc(name, 2), WHITE, scale);
        if (count > 1 && BitmapFont.Ready)
            BitmapFont.DrawOutline(slot.xMax - 14 * scale, slot.yMax - 13 * scale, count.ToString(), WHITE, scale);
    }

    private static string Trunc(string text, int max)
    {
        text = text ?? string.Empty;
        return text.Length > max ? text.Substring(0, max) + "…" : text;
    }
    private static string TimeStr(long time) => time > 0 ? DateTimeOffset.FromUnixTimeSeconds(time).ToLocalTime().ToString("MM-dd HH:mm") : string.Empty;
    private Texture2D Texture(string name) => textures.TryGetValue(name, out Texture2D texture) ? texture : null;
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        foreach (string name in new[] { "maillistdlg", "maildlg", "bigpackdlg", "sender", "close", "delbtn", "delallbtn", "send", "cancelbtn", "replybtn", "sendsmall", "sendbig", "mailresistbtn", "configbtn", "ReciveAllMailBtn" })
        {
            string path = name == "close" ? "res/uires/common/miniclose.gsa" :
                name == "send" ? "res/uires/common/sendbtn.gsa" : "res/uires/maildlg/" + name + ".gsa";
            textures[name] = loader.LoadGsa(path);
        }
    }

    // L（06000597）：官方 arm64 0x470608（40 字节）/ x86_64 0x487920（48 字节）双架构一致，
    // 只做 winX + x * s（实例字段 s @0x134、winX @0x138、winY @0x13c），无 scale 形参。
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);

    // Lbl（06000598）：官方 arm64 0x470630（168 字节）/ x86_64 0x487950（128 字节）双架构一致，
    // 仅在 BitmapFont.Ready 时把 (x, y) 换算成窗口绝对坐标后描边输出。
    private void Lbl(float x, float y, string t, Color c)
    {
        if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, t, c, s);
    }

    private Rect Panel(float scale) => new Rect(winX, winY, Width * scale, Height * scale);
    private static bool Hit(Rect r) => Event.current != null && r.Contains(Event.current.mousePosition);

    // Click（060005a7）：官方 arm64 0x4742fc（524 字节）/ x86_64 0x48b8c0（512 字节）双架构一致，
    // r.Contains(mp) → !SysMsgBox.Visible → Event.current.type == MouseDown → button == 0 → Use()。
    private bool Click(Rect r, Vector2 mp)
    {
        if (!r.Contains(mp)) return false;
        if (SysMsgBox.Visible) return false;
        if (Event.current.type != EventType.MouseDown) return false;
        if (Event.current.button != 0) return false;
        Event.current.Use();
        return true;
    }

    // Retain the original MouseDown/Use contract; this helper changes only the button face.
    private bool Btn(Rect r, Texture2D tex, string fallback, Vector2 mp)
    {
        bool hover = r.Contains(mp);
        bool enabled = GUI.enabled && !SysMsgBox.Visible;
        if (tex != null)
        {
            Color oldColor = GUI.color;
            try
            {
                GUI.color = hover && enabled ? oldColor * new Color(1.25f, 1.25f, 1.25f, 1f) : oldColor;
                GUI.DrawTexture(r, tex);
            }
            finally { GUI.color = oldColor; }
        }
        else
        {
            ArtworkSkin.ButtonFace(r, fallback, s, enabled, hover,
                pressed: hover && enabled && Input.GetMouseButton(0));
        }
        if (!hover || SysMsgBox.Visible) return false;
        if (Event.current.type != EventType.MouseDown) return false;
        if (Event.current.button != 0) return false;
        Event.current.Use();
        return true;
    }

}
}
