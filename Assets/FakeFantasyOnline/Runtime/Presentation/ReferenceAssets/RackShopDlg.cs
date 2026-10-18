using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        private static bool holidayActive;

        // Official RackShopDlg..ctor field-initializer order (06000ad4), consolidated from
        // partial files so the initialization sequence is deterministic and reviewable.
        private float winX = float.NaN;
        private int _wdGen = -1;
        private int _lastTab = -1;
        private string czChain = "TRON";
        private string czAmount = "10";
        private string czLocalWarn = "";
        private string czRChain = "";
        private string czAddr = "";
        private string czRWarn = "";
        private float czCopiedAt = -9f;
        private string czQrB64 = "";
        private string czQrBuilt = "";
        private int shopPartTab = -1;
        private string search = "";
        private readonly Dictionary<int, int> cart = new Dictionary<int, int>();
        private readonly Dictionary<int, string> qtyEdit = new Dictionary<int, string>();
        private List<int> cartOrder = new List<int>();
        private List<Good> view = new List<Good>();
        private int viewGroup = -2;
        private int viewCount = -1;
        private string viewSearch = "\0";
        private int viewPart = -2;
        private int viewCatalog = -1;
        private bool fitShow = true;
        private string fitSearch = "";
        private readonly Dictionary<string, int> fitOn = new Dictionary<string, int>();
        private readonly Dictionary<string, int> fitSouls = new Dictionary<string, int>();
        private int fitGender = -1;
        private int fitGlowMode = -1;
        private readonly Dictionary<int, Texture2D> _fitIcon = new Dictionary<int, Texture2D>();
        private int fitPartTab = -1;

        // Official 0x06000a97/0x06000a98/0x06000a9a.
        public static void SetCats(List<(byte, string)> value) => cats = value ?? new List<(byte, string)>();
        public static void SetGoods(List<Good> value)
        {
            goods = value ?? new List<Good>();
            if (I != null)
            {
                // A fresh catalog may follow an icon-map override or resource recovery.
                I.visibleGoodIcons.Clear();
                I.viewCount = -1;
                I.page = 0;
                I.PruneCart();
            }
        }
        public static void SetHoliday(bool value) => holidayActive = value;

        // Official 0x06000aaa.
        public static void Ensure()
        {
            if (I != null) return;
            var go = new GameObject("RackShopDlg");
            I = go.AddComponent<RackShopDlg>();
            DontDestroyOnLoad(go);
        }

        // Official 0x06000aab/0x06000aac.
        public static void Toggle()
        {
            Ensure();
            I.visible = !I.visible;
            if (I.visible)
            {
                I.topTab = 0;
                if (NetClient.I != null) NetClient.I.OpenRackForIndependentRuntime();
            }
        }
        public static bool Visible => I != null && I.visible;

        // Official 0x06000aad. Both centered-coordinate fallbacks test winX.
        public static bool MouseOver()
        {
            if (I == null || !I.visible) return false;
            float scale = UIScale.Factor;
            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
            float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
            return new Rect(x, y, NW * scale, NH * scale).Contains(mouse);
        }

        // Host diagnostics observe the original callback without driving or loading it.
        internal bool ReadyForIndependentRuntime => !visible || tried;

        // Final-session resource disposal only. Unity owns component destruction and its
        // null semantics; a map/session adapter must not republish native startup state.
        public static void ReleaseIndependentRuntime()
        {
            if (I != null)
            {
                RackShopDlg owner = I;
                if (owner.czQrTex != null) Destroy(owner.czQrTex);
                owner.czQrTex = null;
                Destroy(owner.gameObject);
            }
            if (FittingRoomPreview.I != null) FittingRoomPreview.I.ReleaseForIndependentRuntime();
        }

        // Official 0x06000ab5. Every native tab has its original page consumer.
        private void OnGUI()
        {
            if (!visible)
            {
                if (FittingRoomPreview.I != null) FittingRoomPreview.I.Hide();
                return;
            }
            if (!tried) Load();
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen))
            {
                winX = (Screen.width - NW * s) * 0.5f;
                winY = (Screen.height - NH * s) * 0.5f;
            }
            (int depth, bool covered) = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
            Vector2 savedMouse = Event.current.mousePosition;
            Color savedColor = GUI.color;
            int savedDepth = GUI.depth;
            if (covered) Event.current.mousePosition = new Vector2(-9999f, -9999f);
            try
            {
                Vector2 mouse = Event.current.mousePosition;
                GUI.depth = depth;
                if (bg != null) GUI.DrawTexture(L(0f, 0f, NW, NH), bg);
                else
                {
                    GUI.color = new Color(0.09f, 0.08f, 0.12f, 0.98f);
                    GUI.DrawTexture(L(0f, 0f, NW, NH), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                bool hasHolidayGoods = false;
                foreach (Good good in goods)
                    if (good.group == 255 && (good.flags & 1) != 0) { hasHolidayGoods = true; break; }
                bool showHoliday = holidayActive || hasHolidayGoods;
                if (TopTab(34f, "充值", topCz, 1, mouse)) topTab = 1;
                if (TopTab(108f, "充值返利", topCzr, 2, mouse)) topTab = 2;
                if (TopTab(182f, "每日特价", null, 3, mouse)) { topTab = 3; page = 0; }
                float shopX = 256f;
                if (showHoliday)
                {
                    if (TopTab(256f, "限时抢购", null, 4, mouse)) { topTab = 4; page = 0; }
                    shopX = 330f;
                }
                else if (topTab == 4) topTab = 0;
                if (TopTab(shopX, "商城", topShop, 0, mouse)) topTab = 0;
                if (TopTab(shopX + 74f, "试衣间", null, 5, mouse)) topTab = 5;
                if (refundTabOn)
                {
                    if (TopTab(shopX + 148f, "回收", null, 6, mouse))
                    {
                        if (topTab != 6) NetClient.I?.RackRefundListForIndependentRuntime();
                        topTab = 6;
                        refundScroll = 0;
                    }
                }
                else if (topTab == 6) topTab = 0;
                if (topTab != _lastTab && ((topTab == 3 || topTab == 4) != (_lastTab == 3 || _lastTab == 4)))
                {
                    cart.Clear();
                    cartOrder.Clear();
                    qtyEdit.Clear();
                    cartScroll = 0;
                }
                _lastTab = topTab;
                if (topTab != 5 && FittingRoomPreview.I != null) FittingRoomPreview.I.Hide();
                switch (topTab)
                {
                    case 0: DrawShop(mouse); break;
                    case 1: DrawRecharge(mouse); break;
                    case 2: DrawRechargePlaceholder(); break;
                    case 3: DrawFlagGrid(mouse, 2, "每日特价"); break;
                    case 4: DrawFlagGrid(mouse, 1, "限时抢购"); break;
                    case 6: DrawRefund(mouse); break;
                    default: DrawFittingRoom(mouse); break;
                }
                Rect close = L(812f, 21f, 22f, 23f);
                if (icoClose != null) GUI.DrawTexture(close, icoClose);
                if (Click(close, mouse)) visible = false;
                WinDrag.Handle(this, L(0f, 0f, NW, 44f), ref winX, ref winY);
                if (Event.current.type == EventType.MouseDown && L(0f, 0f, NW, NH).Contains(mouse) && !SysMsgBox.Visible)
                    Event.current.Use();
            }
            finally
            {
                GUI.color = savedColor;
                GUI.depth = savedDepth;
                if (covered) Event.current.mousePosition = savedMouse;
                FlushFitDescWant();
            }
        }
    }
}
