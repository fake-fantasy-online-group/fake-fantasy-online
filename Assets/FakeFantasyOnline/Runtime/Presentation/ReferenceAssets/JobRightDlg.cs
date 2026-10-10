using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 JobRightDlg (职位权限/指派职位窗口), recovered from the native 12-method surface
/// (tokens 0x0600056d..0x06000578). Evidence:
/// docs/contracts/VERSION_UPGRADE_1_5_8_JOB_RIGHT_DLG_TRANSLATION_ARM64_V1.json and
/// ..._VERIFICATION_X86_V1.json.
/// Logical window 262x336, four official .gsa assets, a 指派:&lt;member&gt; title, a scrollable position
/// list, a permission detail panel and 确定/取消. Opened by FamilyDetailDlg's 授予职位 button.
/// Every button and the close icon go through the official JobUI toolkit, recovered alongside.
/// </summary>
public sealed class JobRightDlg : MonoBehaviour
{
    public static JobRightDlg I;

    /// <summary>Official const NW; the type has no .cctor in 1.5.8.</summary>
    private const float NW = 262f;

    /// <summary>Official const NH.</summary>
    private const float NH = 336f;

    /// <summary>The native position list, shared with JobCustomDlg and the 8041 receipt.</summary>
    public static IReadOnlyList<(int Id, string Name, string Note)> Positions => SocialClient.FamilyPositions;

    private bool visible;
    private bool tried;
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoOk;
    private Texture2D icoCancel;
    private string target = "";
    private int sel = -1;
    private Vector2 listScroll;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    /// <summary>Official Ensure (0x0600056d): lazily builds the singleton GameObject.</summary>
    public static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("JobRightDlg");
        I = go.AddComponent<JobRightDlg>();
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    /// <summary>Official Toggle (0x0600056e): Ensure, flip visible, and run OnOpen only on the
    /// hidden->visible edge.</summary>
    public static void Toggle()
    {
        Ensure();
        bool wasVisible = I.visible;
        I.visible = !wasVisible;
        if (!wasVisible) I.OnOpen();
    }

    /// <summary>Official Open (0x0600056f, string memberName): Ensure, store the member name,
    /// reset the row selection to -1, show the window and run OnOpen.</summary>
    public static void Open(string memberName)
    {
        Ensure();
        I.target = memberName ?? "";
        I.sel = -1;
        I.visible = true;
        I.OnOpen();
    }

    /// <summary>Official OnOpen (0x06000570): ask the server for the position list.</summary>
    private void OnOpen()
    {
        if (NetClient.I != null) NetClient.I.FamilyPositionsForIndependentRuntime();
    }

    /// <summary>Official get_Visible (0x06000571).</summary>
    public static bool get_Visible()
    {
        return I != null && I.visible;
    }

    /// <summary>Window-level hit test used by adjacent dialogs (official MouseOver, 0x06000572):
    /// true when the window exists, is visible and the real mouse is inside the 262x336 scaled
    /// rect. A NaN winX means the window has not been placed yet, so the centred rect is used for
    /// both axes.</summary>
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float factor = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - factor * NW) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - factor * NH) * 0.5f : I.winY;
        return mx >= x && mx < x + factor * NW && my >= y && my < y + factor * NH;
    }

    /// <summary>Official Load (0x06000573): sets tried first and clears it back when the loader is
    /// missing so a later frame retries.</summary>
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null)
        {
            tried = false;
            return;
        }
        bg = loader.LoadGsa("res/uires/family/jobrightdlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoOk = loader.LoadGsa("res/uires/common/affirmbtn.gsa");
        icoCancel = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
    }

    /// <summary>Rect in scaled window space: (winX + x*s, winY + y*s, w*s, h*s). Official L
    /// 0x06000574; 1.5.8 inlines it at every use site, so the native method has no callers.</summary>
    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    /// <summary>Official Lbl (0x06000575): DrawOutline at (winX+x*s, winY+y*s) behind
    /// BitmapFont.Ready. 1.5.8 never calls this member; it is restored on the official surface and
    /// intentionally left without a caller.</summary>
    private void Lbl(float x, float y, string text, Color fill)
    {
        if (!BitmapFont.Ready) return;
        BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, fill, s);
    }

    /// <summary>Official WrapLocal (0x06000576): word-wrap draw for the permission text. It starts
    /// at (x*s, y*s) without adding winX/winY because the official call site wraps it in
    /// GUI.BeginClip, which supplies the window origin. Each character advances by its measured
    /// width; a break moves to the next line (y += s*14) when the next character would pass
    /// (x + wPx)*s, and '\n' breaks unconditionally.</summary>
    private void WrapLocal(float x, float y, float wPx, string text, Color c)
    {
        if (text.Length == 0) return;
        float startX = x * s;
        float right = (x + wPx) * s;
        float lineY = y * s;
        float curX = startX;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == '\n')
            {
                lineY += s * 14f;
                curX = startX;
                continue;
            }
            float width = BitmapFont.Measure(ch.ToString(), s);
            if (right < curX + width)
            {
                lineY += s * 14f;
                curX = startX;
            }
            BitmapFont.DrawOutline(curX, lineY, ch.ToString(), c, s);
            curX += width;
        }
    }

    /// <summary>Official automatic Unity callback. This window does not use WinZ:
    /// the original body sets the literal GUI.depth 1.</summary>
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();

        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - s * NW) * 0.5f;
            winY = (Screen.height - s * NH) * 0.5f;
        }

        Vector2 mouse = Event.current.mousePosition;
        GUI.depth = 1;

        if (bg == null)
        {
            GUI.color = new Color(0.1f, 0.09f, 0.13f, 0.98f);
            GUI.DrawTexture(L(0f, 0f, NW, NH), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        else
        {
            GUI.DrawTexture(L(0f, 0f, NW, NH), bg);
        }

        Rect titleRect = L(29f, 60f, 214f, 12f);
        GUI.color = new Color(0f, 0f, 0f, 0.3f);
        GUI.DrawTexture(titleRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        // The title and its color are evaluated before the font-ready gate.
        string title = string.IsNullOrEmpty(target) ? "（未选成员）" : "指派: " + target;
        Color titleColor = JobUI.GOLD;
        if (BitmapFont.Ready)
        {
            BitmapFont.DrawOutline(winX + 32f * s, winY + 60f * s, title, titleColor, s);
        }

        Rect listRect = L(29f, 99f, 194f, 100f);
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(listRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        IReadOnlyList<(int Id, string Name, string Note)> rows = Positions;
        if (rows.Count == 0)
        {
            if (BitmapFont.Ready)
            {
                // Official literal (metadata usage 10854) is "(载入职位…)" with ASCII parentheses,
                // not the full-width pair the earlier contract wrote down.
                BitmapFont.CenterOutline(L(29f, 144f, 194f, 12f), "(载入职位…)", JobUI.GRAY, s);
            }
        }
        else
        {
            float contentHeight = rows.Count * 16f * s;
            float contentWidth = listRect.width - 16f * s;
            listScroll = GUI.BeginScrollView(listRect, listScroll,
                new Rect(0f, 0f, contentWidth, contentHeight));
            for (int i = 0; i < rows.Count; i++)
            {
                // The official reads Event.mousePosition inside the scroll view, so the hover test
                // is in scroll-view space; re-read it here for the same reason.
                float rowY = i * 16f * s;
                float rowH = 16f * s;
                Vector2 rowMouse = Event.current.mousePosition;
                bool hover = rowMouse.x >= 0f && rowMouse.x < contentWidth &&
                    rowMouse.y >= rowY && rowMouse.y < rowY + rowH;
                bool selected = i == sel;

                if (selected)
                {
                    GUI.color = new Color(0.45f, 0.38f, 0.16f, 0.65f);
                    GUI.DrawTexture(new Rect(0f, rowY, contentWidth, rowH), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                else if (hover)
                {
                    GUI.color = new Color(0.3f, 0.3f, 0.7f, 0.2f);
                    GUI.DrawTexture(new Rect(0f, rowY, contentWidth, rowH), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                if (BitmapFont.Ready)
                {
                    BitmapFont.DrawOutline(6f * s, rowY + 2f * s, rows[i].Name,
                        i == sel ? JobUI.GOLD : FamUI.Light, s);
                }

                if (hover && Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0)
                {
                    sel = i;
                    Event.current.Use();
                }
            }
            GUI.EndScrollView();
        }

        Rect detailRect = L(26f, 225f, 218f, 74f);
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(detailRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (sel >= 0 && sel < rows.Count && BitmapFont.Ready)
        {
            string note = rows[sel].Note;
            if (string.IsNullOrEmpty(note)) note = "（基础职位, 无附加权限）";
            GUI.BeginClip(detailRect);
            WrapLocal(4f, 3f, 210f, note, FamUI.Light);
            GUI.EndClip();
        }

        if (JobUI.Btn(L(38f, 310f, 70f, 22f), icoOk, "确定", mouse, s))
        {
            if (string.IsNullOrEmpty(target))
            {
                ChatUI.Add("系统", "请先选中一名成员", new Color(1f, 220f / 255f, 150f / 255f, 1f), "");
            }
            else if (sel < 0 || sel >= rows.Count)
            {
                ChatUI.Add("系统", "请选择要指派的职位", new Color(1f, 220f / 255f, 150f / 255f, 1f), "");
            }
            else
            {
                if (NetClient.I != null)
                {
                    NetClient.I.FamilyManageForIndependentRuntime(1, target, rows[sel].Id, "");
                }
                visible = false;
            }
        }

        if (JobUI.Btn(L(165f, 310f, 70f, 22f), icoCancel, "取消", mouse, s))
        {
            visible = false;
        }

        Rect closeRect = L(237f, 9f, 20f, 19f);
        if (icoClose != null) GUI.DrawTexture(closeRect, icoClose);
        if (JobUI.Click(closeRect, mouse)) visible = false;

        WinDrag.Handle(this, L(0f, 0f, NW, 28f), ref winX, ref winY);

        if (Event.current.type == EventType.MouseDown &&
            L(0f, 0f, NW, NH).Contains(mouse) && !SysMsgBox.Visible)
        {
            Event.current.Use();
        }
        return;
    }
}
}
