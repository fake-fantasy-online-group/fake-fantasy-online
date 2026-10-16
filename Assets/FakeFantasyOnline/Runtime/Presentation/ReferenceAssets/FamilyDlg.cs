using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 FamilyDlg (家族目录窗口), recovered from the native 15-method surface
/// (tokens 0x06000342..0x06000350). Evidence:
/// docs/contracts/VERSION_UPGRADE_1_5_8_FAMILY_DLG_TRANSLATION_ARM64_V1.json and
/// docs/contracts/VERSION_UPGRADE_1_5_8_FAMILY_DLG_VERIFICATION_X86_V1.json.
/// Window logic size 494x325; rows come from SocialClient.FamilyBrowse
/// (List&lt;ValueTuple&lt;string,int,int,int,string&gt;&gt; = name, level, members, cap, leader).
/// </summary>
public sealed partial class FamilyDlg : MonoBehaviour
{
    public static FamilyDlg I;

    private static readonly Color Gold = new Color(1f, 0.92f, 0.66f, 1f);
    private static readonly Color Gray = new Color(0.66f, 0.64f, 0.55f, 1f);
    private static readonly Color Head = new Color(0.82f, 0.74f, 0.5f, 1f);
    private static readonly (string, float, float)[] Cols =
    {
        ("家族名", 28f, 120f),
        ("等级", 170f, 40f),
        ("人数", 230f, 70f),
        ("族长", 320f, 120f),
    };

    /// <summary>The live native directory root, also mutated by the 8040 receipt.</summary>
    public static IReadOnlyList<(string, int, int, int, string)> Browse => SocialClient.FamilyBrowse;

    private bool visible;
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoJoin;
    private Texture2D icoAlly;
    private Texture2D icoEnemy;
    private bool tried;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;
    private int listScroll;
    private int sel = -1;

    public static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("FamilyDlg");
        I = go.AddComponent<FamilyDlg>();
        DontDestroyOnLoad(go);
    }

    public static void Toggle()
    {
        Ensure();
        I.visible = !I.visible;
        if (I.visible) I.Refresh();
    }

    public static void OpenSelf()
    {
        Ensure();
        I.visible = true;
        I.Refresh();
    }

    public static void Hide()
    {
        if (I != null) I.visible = false;
    }

    public static bool get_Visible()
    {
        return I != null && I.visible;
    }

    /// <summary>Window-level hit test used by adjacent dialogs (official MouseOver): true when the
    /// window exists, is visible and the real mouse is inside the 494x325 scaled rect.</summary>
    // GUI-space, explicit physical-pointer hit testing; independent of Input.mousePosition.
    internal static bool MobileContainsPointer(Vector2 point)
    {
        if (!get_Visible()) return false;
        float scale = UIScale.Factor;
        float width = 494f, height = 325f;
        bool centered = float.IsNaN(I.winX) || I._wdGen != WinDrag.Gen;
        float x = centered ? (Screen.width - width * scale) * .5f : I.winX;
        float y = centered ? (Screen.height - height * scale) * .5f : I.winY;
        if (new Rect(x, y, width * scale, height * scale).Contains(point)) return true;
        return false;
    }


    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float factor = UIScale.Factor;
        float x = float.IsNaN(I.winX) ? (Screen.width - factor * 494f) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - factor * 325f) * 0.5f : I.winY;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y; // top-left origin
        return mx >= x && mx < x + factor * 494f && my >= y && my < y + factor * 325f;
    }

    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null)
        {
            tried = false;
            return;
        }
        bg = loader.LoadGsa("res/uires/family/familylistdlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoJoin = loader.LoadGsa("res/uires/family/joinbtn.gsa");
        icoAlly = loader.LoadGsa("res/uires/family/allybtn.gsa");
        icoEnemy = loader.LoadGsa("res/uires/family/enemybtn.gsa");
    }

    public void Refresh()
    {
        sel = -1;
        if (NetClient.I != null) NetClient.I.FamilyListForIndependentRuntime();
    }

    /// <summary>Rect in scaled window space: (winX + x*s, winY + y*s, w*s, h*s).</summary>
    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    private void Lbl(float x, float y, string text, Color fill)
    {
        if (BitmapFont.Ready)
            BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, fill, s);
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - s * 494f) * 0.5f;
            winY = (Screen.height - s * 325f) * 0.5f;
        }
        // Official order: the pointer position is read before WinZ.Frame and read again after it,
        // the second read becoming the value the frame restores when it blanks the pointer.
        Vector2 mouse = Event.current.mousePosition;
        (int depth, bool covered) = WinZ.Frame(this, new Rect(winX, winY, s * 494f, s * 325f), 10);
        Vector2 saved = Event.current.mousePosition;
        if (covered)
            Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            GUI.depth = depth;

            if (bg == null)
            {
                GUI.color = new Color(0.09f, 0.08f, 0.12f, 0.98f);
                GUI.DrawTexture(L(0f, 0f, 494f, 325f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            else
            {
                GUI.DrawTexture(L(0f, 0f, 494f, 325f), bg);
            }

            foreach ((string name, float cx, float cw) in Cols)
            {
                Rect r = L(cx, 40f, cw, 12f);
                if (r.Contains(mouse))
                {
                    ArtworkSkin.Row(r, s, true);
                }
                if (BitmapFont.Ready) BitmapFont.DrawOutline(r.x, r.y, name, Head, s);
                Click(r, mouse);
            }

            Rect panel = L(23f, 56f, 440f, 230f);
            ArtworkSkin.Input(panel, s);

            IReadOnlyList<(string, int, int, int, string)> rows = Browse;
            int count = rows.Count;

            if (Event.current.type == EventType.ScrollWheel && panel.Contains(mouse))
            {
                listScroll += Event.current.delta.y >= 0f ? 1 : -1;
                listScroll = Mathf.Clamp(listScroll, 0, Mathf.Max(0, count - 12));
                Event.current.Use();
            }
            listScroll = Mathf.Clamp(listScroll, 0, Mathf.Max(0, count - 12));

            if (rows.Count < 1)
            {
                Lbl(180f, 160f, "暂无家族, 点【创建】建立一个", Gray);
            }
            else
            {
                for (int i = 0; i < 12; i++)
                {
                    int rowIndex = i + listScroll;
                    if (rowIndex >= count) break;
                    Rect rowRect = new Rect(
                        winX + 24f * s, winY + (57f + 18f * i) * s, 422f * s, 17f * s);
                    if (sel == rowIndex) ArtworkSkin.Input(rowRect, s, true);
                    else ArtworkSkin.Row(rowRect, s, rowRect.Contains(mouse));
                    (string name, int level, int members, int cap, string leader) = rows[rowIndex];
                    float rowTextY = 58f + 18f * i;
                    Lbl(28f, rowTextY, BitmapFont.Elide(name, 138f * s, s),
                        new Color(0.95f, 0.95f, 0.92f, 1f));
                    Lbl(170f, rowTextY, "Lv" + level.ToString(), Gray);
                    Lbl(230f, rowTextY, members.ToString() + "/" + cap.ToString(), Gray);
                    Lbl(320f, rowTextY, BitmapFont.Elide(leader, 124f * s, s),
                        new Color(0.85f, 0.78f, 0.55f, 1f));
                    if (Click(rowRect, mouse)) sel = rowIndex;
                }
            }

            NativeScroll.BarRows(this,
                new Rect(winX + 463.5f * s, winY + 57f * s, 15f * s, 228f * s),
                12, count, ref listScroll);

            if (Btn(new Rect(winX + 22f * s, winY + 296f * s, 90f * s, 22f * s),
                    icoJoin, "加入", mouse) &&
                sel >= 0 && sel < rows.Count && NetClient.I != null)
            {
                NetClient.I.FamilyApplyForIndependentRuntime(rows[sel].Item1);
            }
            if (Btn(new Rect(winX + 200f * s, winY + 296f * s, 90f * s, 22f * s),
                    null, "创建", mouse))
            {
                FamilyCreateDlg.Toggle();
            }
            if (RefreshBtn(new Rect(winX + 378f * s, winY + 296f * s, 90f * s, 22f * s), mouse))
            {
                Refresh();
            }

            Rect closeRect = L(469f, 10f, 20f, 19f);
            if (icoClose != null)
            {
                GUI.DrawTexture(closeRect, icoClose);
            }
            if (Click(closeRect, mouse)) visible = false;

            WinDrag.Handle(this, L(0f, 0f, 494f, 28f), ref winX, ref winY);

            if (Event.current.type == EventType.MouseDown &&
                L(0f, 0f, 494f, 325f).Contains(mouse) &&
                !SysMsgBox.Visible && !FamilyCreateDlg.MouseOver())
            {
                Event.current.Use();
            }
            return;
        }
        finally
        {
            if (covered) Event.current.mousePosition = saved;
        }
    }

    private bool Click(Rect r, Vector2 mp)
    {
        if (!r.Contains(mp) || SysMsgBox.Visible) return false;
        if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return false;
        Event.current.Use();
        return true;
    }

    private bool RefreshBtn(Rect r, Vector2 mouse)
    {
        bool enabled = GUI.enabled && !SysMsgBox.Visible;
        bool hover = enabled && r.Contains(mouse);
        int frame = !enabled ? 3 : hover && Input.GetMouseButton(0) ? 2 : hover ? 1 : 0;
        Texture2D[] refreshFrames = ArtworkSkin.Frames("res/uires/family/refreshbtn.gsa");
        Texture2D texture = refreshFrames != null ? refreshFrames[frame] ?? refreshFrames[0] : null;
        if (texture != null)
        {
            Color oldColor = GUI.color;
            try
            {
                GUI.color = enabled ? oldColor : oldColor * new Color(.65f, .65f, .65f, 1f);
                GUI.DrawTexture(r, texture);
            }
            finally { GUI.color = oldColor; }
        }
        else
            ArtworkSkin.ButtonFace(r, "刷新", s, enabled, hover,
                pressed: hover && Input.GetMouseButton(0));
        return Click(r, mouse);
    }

    private bool Btn(Rect r, Texture2D tex, string fallback, Vector2 mp)
    {
        bool hover = r.Contains(mp);
        if (tex != null)
        {
            Color oldColor = GUI.color;
            try
            {
                GUI.color = hover && !SysMsgBox.Visible ? oldColor * new Color(1.25f, 1.25f, 1.25f, 1f) : oldColor;
                GUI.DrawTexture(r, tex);
            }
            finally { GUI.color = oldColor; }
        }
        else
        {
            ArtworkSkin.ButtonFace(r, fallback, s, !SysMsgBox.Visible, hover,
                pressed: hover && Input.GetMouseButton(0));
        }
        if (!hover || SysMsgBox.Visible) return false;
        if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return false;
        Event.current.Use();
        return true;
    }
}
}
