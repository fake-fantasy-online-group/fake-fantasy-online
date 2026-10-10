using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>Recovered official 1.5.8 transport window owner.</summary>
public class TransportUI : MonoBehaviour
{
    public static TransportUI I;

    private Texture2D winBg;
    private Texture2D itemBg;
    private Texture2D itemSelCover;
    private Texture2D closeTex;
    private Texture2D transBtn;
    private Texture2D cancelBtn;
    private Texture2D goldIc;
    private Texture2D silverIc;
    private Texture2D copperIc;
    private Font font;
    private GUIStyle capSt;
    private GUIStyle nameSt;
    private GUIStyle priceSt;
    private bool show;
    private int transList;
    private List<TeleportData.Dest> dests = new List<TeleportData.Dest>();
    private int sel;
    private Vector2 scroll;
    private float lastClickT = -1f;
    private int lastClickIdx = -1;
    private bool thumbDrag;

    // Independent-runtime connection: the original BitmapFont enters through the existing source
    // font. The official command this window emits is NetClient.Teleport (0x060006e9) on the
    // static NetClient.I instance (0x06000f96 reads TypeInfo 02073 +0xb8 after its cctor gate),
    // so the window calls that member directly and no second writerhop is introduced here.
    private ReferenceWorldBitmapFont bitmapFont;

    internal void BindForIndependentRuntime(ReferenceWorldBitmapFont valueFont)
    {
        bitmapFont = valueFont;
    }

    internal void InitializeForIndependentRuntime() => Awake();
    internal bool VisibleForIndependentRuntime => show;
    internal int TransportListForIndependentRuntime => transList;

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        ResLoader res = ResLoader.I;
        if (res != null)
        {
            winBg = res.LoadGsa("res/uires/transportdlg/transportdlg.gsa");
            itemBg = res.LoadGsa("res/uires/transportdlg/transitem.gsa");
            itemSelCover = res.LoadGsa("res/uires/transportdlg/checkcover.gsa");
            closeTex = res.LoadUI("ui/equipdlg05/miniclose_f0.png");
            transBtn = res.LoadGsaFrame("res/uires/transportdlg/transportbtn.gsa", 0);
            cancelBtn = res.LoadGsaFrame("res/uires/common/cancelbtn.gsa", 0);
            goldIc = res.LoadGsaFrame("res/uires/common/gold.gsa", 0);
            silverIc = res.LoadGsaFrame("res/uires/common/silver.gsa", 0);
            copperIc = res.LoadGsaFrame("res/uires/common/copper.gsa", 0);
        }

        font = ReferenceWorldChatBubbleRenderer.CnFont();
    }

    public void Open(List<TeleportData.Dest> d, int tl = 0)
    {
        transList = tl;
        dests = d ?? new List<TeleportData.Dest>();
        sel = dests.Count > 0 ? 0 : -1;
        scroll = Vector2.zero;
        show = true;
    }

    public void Close() => show = false;
    public static bool VisibleS => I != null && I.show;
    public static void CloseS()
    {
        if (I != null)
        {
            I.Close();
        }
    }

    public bool MouseOver() => show;

    private static string MoneyText(int money)
    {
        if (money <= 0)
        {
            return "免费";
        }

        StringBuilder text = new StringBuilder();
        if (money / 1000000 > 0)
        {
            text.Append(money / 1000000).Append("金");
        }
        if (money % 1000000 / 1000 > 0)
        {
            text.Append(money % 1000000 / 1000).Append("银");
        }
        if (money % 1000 > 0)
        {
            text.Append(money % 1000).Append("铜");
        }
        return text.ToString();
    }

    private void EnsureStyles()
    {
        if (capSt != null)
        {
            return;
        }

        capSt = new GUIStyle { alignment = TextAnchor.MiddleCenter, font = font };
        capSt.normal.textColor = new Color(1f, 0.92f, 0.6f, 1f);
        capSt.fontSize = Mathf.RoundToInt(UIScale.Factor * 13f);
        nameSt = new GUIStyle { alignment = TextAnchor.MiddleLeft, font = font };
        nameSt.normal.textColor = Color.white;
        nameSt.fontSize = Mathf.RoundToInt(UIScale.Factor * 13f);
        priceSt = new GUIStyle { alignment = TextAnchor.MiddleLeft, font = font };
        priceSt.normal.textColor = new Color(1f, 0.85f, 0.35f, 1f);
        priceSt.fontSize = Mathf.RoundToInt(UIScale.Factor * 12f);
    }

    private void OnGUI()
    {
        if (!show)
        {
            return;
        }

        EnsureStyles();
        float s = UIScale.Factor;
        float width = s * 343f;
        float height = s * 426f;
        float left = (Screen.width - width) * 0.5f;
        float top = (Screen.height - height) * 0.5f;
        Rect R(float x, float y, float w, float h) =>
            new Rect(s * x + left, s * y + top, s * w, s * h);

        if (winBg != null)
        {
            GUI.DrawTexture(R(0f, 0f, 343f, 426f), winBg);
        }
        else
        {
            GUI.color = new Color(0.1f, 0.07f, 0.05f, 0.95f);
            GUI.DrawTexture(R(0f, 0f, 343f, 426f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        bitmapFont?.Measure(".", s);
        if (bitmapFont != null && bitmapFont.Ready)
        {
            bitmapFont.CenterOutline(R(70f, 27f, 180f, 14f), "传送",
                new Color(1f, 0.92f, 0.6f, 1f), s);
        }
        else
        {
            GUI.Label(R(70f, 27f, 180f, 14f), "传送", capSt);
        }

        if (Btn(R(294f, 23f, 20f, 19f), closeTex))
        {
            show = false;
            return;
        }

        Rect view = R(34f, 54f, 257f, 340f);
        float rowHeight = s * 67f;
        float contentHeight = rowHeight * dests.Count;
        float maxScroll = Mathf.Max(0f, contentHeight - view.height);
        if (Event.current.type == EventType.ScrollWheel && view.Contains(Event.current.mousePosition))
        {
            scroll.y = Mathf.Clamp(scroll.y + s * (Event.current.delta.y * 18f), 0f, maxScroll);
            Event.current.Use();
        }

        scroll = GUI.BeginScrollView(view, scroll,
            new Rect(0f, 0f, s * 257f, contentHeight), GUIStyle.none, GUIStyle.none);
        for (int i = 0; i < dests.Count; i++)
        {
            float y = rowHeight * i;
            Rect rowRect = new Rect(0f, y, s * 257f, rowHeight);
            if (itemBg != null)
            {
                GUI.DrawTexture(new Rect(s * 68f, s * 5f + y, s * 188f, s * 59f), itemBg);
            }

            DrawWrapName(s * 4f, s * 5f + y, s * 55f, s * 59f, dests[i].name, s);
            if (bitmapFont != null && bitmapFont.Ready)
            {
                bitmapFont.Draw(s * 72f, s + s + s * 12f + y, dests[i].name, Color.white, s);
            }
            else
            {
                GUI.Label(new Rect(s * 72f, s * 12f + y, s * 120f, s * 12f),
                    dests[i].name, nameSt);
            }
            DrawMoney(s * 72f, s * 32f + y, dests[i].price, s);

            if (i == sel)
            {
                if (itemSelCover != null)
                {
                    GUI.DrawTexture(rowRect, itemSelCover);
                }
                else
                {
                    GUI.color = new Color(0.3f, 0.9f, 1f, 0.18f);
                    GUI.DrawTexture(rowRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }

            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                bool doubleClick = i == lastClickIdx && Time.unscaledTime - lastClickT < 0.35f;
                sel = i;
                lastClickIdx = i;
                lastClickT = Time.unscaledTime;
                Event.current.Use();
                if (doubleClick)
                {
                    GUI.EndScrollView();
                    DoTeleport();
                    return;
                }
            }
        }
        GUI.EndScrollView();
        NativeScroll.BarAt(this, R(297f, 72f, 15f, 300f), view.height, contentHeight, ref scroll);

        if (Btn(R(30f, 399f, 90f, 22f), transBtn, "传送"))
        {
            DoTeleport();
            return;
        }
        if (Btn(R(226f, 399f, 70f, 22f), cancelBtn, "取消"))
        {
            show = false;
        }
    }

    private void DrawWrapName(float x, float y, float w, float h, string name, float s)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        int count = Mathf.Max(1, Mathf.FloorToInt(w / (s * 12f)));
        List<string> lines = new List<string>();
        for (int offset = 0; offset < name.Length; offset += count)
        {
            lines.Add(name.Substring(offset, Mathf.Min(count, name.Length - offset)));
        }
        float lineHeight = s * 14f;
        y += (h - lineHeight * lines.Count) * 0.5f;
        foreach (string line in lines)
        {
            if (bitmapFont != null && bitmapFont.Ready)
            {
                bitmapFont.Draw(x, y, line, Color.white, s);
            }
            else
            {
                GUI.Label(new Rect(x, y, w, lineHeight), line, nameSt);
            }
            y += lineHeight;
        }
    }

    private void DrawMoney(float x, float y, int money, float s)
    {
        x = Coin(x, y, goldIc, money / 1000000, s);
        x = Coin(x, y, silverIc, money % 1000000 / 1000, s);
        Coin(x, y, copperIc, money % 1000, s);
    }

    private float Coin(float x, float y, Texture2D ic, int n, float s)
    {
        float size = s * 11f;
        if (ic != null)
        {
            GUI.DrawTexture(new Rect(x, y + s, size, size), ic);
        }
        string text = n.ToString();
        x = s + s + x + size;
        if (bitmapFont != null && bitmapFont.Ready)
        {
            bitmapFont.Draw(x, y, text, new Color(1f, 0.9f, 0.5f, 1f), s);
        }
        else
        {
            GUI.Label(new Rect(x, y, s * 18f, s * 12f), text, priceSt);
        }
        return x + s * (text.Length * 7 + 5);
    }

    // Official 1.5.8 TransportUI.DoTeleport (0x06000f96, arm64 0x3a5cfc / 184 bytes, x86_64
    // 0x3b8cf0 / 176 bytes; both agree line for line):
    //
    //   if (sel >= 0) {                                        // -1 < *(int*)(this + 0x98)
    //     if (dests == null) <raise>;                          // non-returning helper: an IL2CPP
    //                                                         // NullReferenceException, never a
    //                                                         // skipped send
    //     if (sel < dests.Count) {                             // List.Count at dests + 0x18
    //       show = false;                                      // this + 0x88, written BEFORE the
    //                                                         // NetClient.I read
    //       NetClient cctor gate (+0xe4) -> I (+0xb8);
    //       if (I != 0) NetClient.Teleport(I, transList, sel);
    //     }                                                      // bare reference test: neither
    //   }                                                        // body calls a null-check helper
    //
    // So the window always hides when a row is selected, connection or not, and no second effect
    // follows the call: no local map change, no money check, no unavailability check and no
    // "success" of any kind. Which destination the index names and whether it can be travelled to
    // stay server-authored - the row price above is display only.
    //
    // The singleton gate is a compare against zero of the raw static slot on both architectures
    // even though NetClient derives from UnityEngine.MonoBehaviour in official 1.5.8 as well, so
    // the write below uses ReferenceEquals and stays a bare reference test rather than becoming a
    // Unity Object null test, whose behaviour differs for a destroyed instance.
    private void DoTeleport()
    {
        if (sel < 0 || sel >= dests.Count)
        {
            return;
        }

        show = false;
        if (!ReferenceEquals(NetClient.I, null))
        {
            NetClient.I.TeleportForIndependentRuntime(transList, sel);
        }
    }

    private bool Btn(Rect r, Texture2D tex, string fallback = null)
    {
        if (tex != null)
        {
            GUI.DrawTexture(r, tex);
        }
        else if (fallback != null)
        {
            GUI.color = new Color(0.3f, 0.2f, 0.12f, 0.9f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(r, fallback, capSt);
        }
        return Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition) &&
            UseEvent();
    }

    private bool UseEvent()
    {
        Event.current.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }
}

}
