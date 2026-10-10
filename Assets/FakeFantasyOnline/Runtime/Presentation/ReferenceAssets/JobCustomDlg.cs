using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 JobCustomDlg (自定职位窗口), recovered from the native 7-method surface
/// (tokens 0x0600055e..0x06000564). Evidence:
/// docs/contracts/VERSION_UPGRADE_1_5_8_JOB_CUSTOM_DLG_TRANSLATION_ARM64_V1.json and
/// ..._VERIFICATION_X86_V1.json.
/// Logical window 304x236, five official .gsa assets, a scrollable position list, an 8-character
/// name field and 设定/恢复/权限. Opened by FamilyDetailDlg's 自定职位 button. Buttons and the
/// close icon go through the JobUI toolkit recovered with JobRightDlg.
/// Unlike the other family windows the official type has no Ensure: the singleton is created
/// inside <see cref="Toggle"/>, and the product keeps that shape.
/// </summary>
public sealed class JobCustomDlg : MonoBehaviour
{
    public static JobCustomDlg I;

    /// <summary>Official const NW; the type has no .cctor in 1.5.8.</summary>
    private const float NW = 304f;

    /// <summary>Official const NH.</summary>
    private const float NH = 236f;

    private bool visible;
    private bool tried;
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoNew;
    private Texture2D icoDel;
    private Texture2D icoRight;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;
    private int sel = -1;
    private string nameEdit = "";
    private Vector2 scroll;

    /// <summary>Official 1.6.0 Toggle (0x06000568): create the singleton when it is missing, flip
    /// visible, and on the hidden->visible edge reset the editor and ask for the position
    /// list.</summary>
    public static void Toggle()
    {
        if (I == null)
        {
            GameObject go = new GameObject("JobCustomDlg");
            I = go.AddComponent<JobCustomDlg>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
        bool wasVisible = I.visible;
        I.visible = !wasVisible;
        if (!wasVisible)
        {
            I.nameEdit = "";
            I.sel = -1;
            // The original checks the managed reference, not Unity object liveness.
            NetClient.I?.FamilyPositionsForIndependentRuntime();
        }
    }

    /// <summary>Official get_Visible (0x0600055f).</summary>
    public static bool get_Visible()
    {
        return I != null && I.visible;
    }

    /// <summary>Window-level hit test used by adjacent dialogs (official 1.6.0 MouseOver, 0x0600056a):
    /// true when the window exists, is visible and the real mouse is inside the 304x236 scaled
    /// rect. A NaN winX means the window has not been placed yet, so the centred rect is used for
    /// both axes.</summary>
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float factor = UIScale.Factor;
        // Preserve the original separate input reads before the window-position reads.
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX)
            ? (Screen.width - factor * NW) * 0.5f
            : I.winX;
        float y = float.IsNaN(I.winX)
            ? (Screen.height - factor * NH) * 0.5f
            : I.winY;
        return mx >= x && mx < x + factor * NW && my >= y && my < y + factor * NH;
    }

    /// <summary>Official Load (0x06000561): sets tried first and clears it back when the loader is
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
        bg = loader.LoadGsa("res/uires/family/jobcustomdlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoNew = loader.LoadGsa("res/uires/family/jobnewbtn.gsa");
        icoDel = loader.LoadGsa("res/uires/family/jobdelbtn.gsa");
        icoRight = loader.LoadGsa("res/uires/family/jobrightbtn.gsa");
    }

    /// <summary>Rect in scaled window space: (winX + x*s, winY + y*s, w*s, h*s). Official L
    /// 0x06000562; 1.5.8 inlines it at every use site, so the native method has no callers.</summary>
    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    /// <summary>Official automatic Unity callback. This window does not use WinZ:
    /// the original body sets the literal GUI.depth 2. The 1.6.0 input/action boundary
    /// (0x0600056d) is recorded in VERSION_UPGRADE_1_6_0_JOB_CUSTOM_CALLBACK_BOUNDARY_V1.md.</summary>
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

        Event e = Event.current;
        Vector2 mouse = e.mousePosition;
        GUI.depth = 2;

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

        Rect listRect = L(27f, 56f, 240f, 126f);
        ArtworkSkin.Input(listRect, s);

        IReadOnlyList<(int Id, string Name, string Note)> rows = JobRightDlg.Positions;
        if (rows.Count == 0)
        {
            if (BitmapFont.Ready)
            {
                // Official 1.6.0 literal (metadata index 520) is "(载入职位…)" with ASCII parentheses,
                // not the full-width pair the earlier contract wrote down.
                BitmapFont.CenterOutline(L(27f, 120f, 240f, 12f), "(载入职位…)", JobUI.GRAY, s);
            }
        }
        else
        {
            Rect listView = L(30f, 59f, 234f, 120f);
            float contentHeight = rows.Count * 16f * s;
            scroll.y = Mathf.Clamp(scroll.y, 0f, Mathf.Max(0f, contentHeight - listView.height));
            scroll.x = 0f;
            scroll = GUI.BeginScrollView(listView, scroll,
                new Rect(0f, 0f, 216f * s, contentHeight), GUIStyle.none, GUIStyle.none);
            for (int i = 0; i < rows.Count; i++)
            {
                float rowY = i * 16f * s;
                float rowH = 16f * s - 1f;
                Rect rowRect = new Rect(0f, rowY, 216f * s, rowH);

                if (i == sel) ArtworkSkin.Input(rowRect, s, true);
                else ArtworkSkin.Row(rowRect, s, rowRect.Contains(Event.current.mousePosition));

                if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
                {
                    sel = i;
                    nameEdit = rows[i].Name;
                }

                if (BitmapFont.Ready)
                {
                    // The original prefix test reads Item3; Item2 is copied and displayed.
                    bool custom = rows[i].Note.StartsWith("[本族自定义]");
                    Color textColor = custom
                        ? new Color(0.588f, 0.9216f, 1f, 1f)
                        : new Color(0.745f, 0.8235f, 0.843f, 1f);
                    string label = String.Format("{0}. {1}", rows[i].Id, rows[i].Name);
                    BitmapFont.DrawOutline(4f * s, rowY + 2f * s,
                        BitmapFont.Elide(label, (custom ? 162f : 208f) * s, s), textColor, s);
                    if (custom)
                    {
                        BitmapFont.DrawOutline(174f * s, rowY + 2f * s, "自定义",
                            new Color(1f, 0.8627f, 0.5882f, 1f), s);
                    }
                }
            }
            GUI.EndScrollView();
            NativeScroll.Bar(this, listView, contentHeight, ref scroll);
        }

        ArtworkSkin.Input(L(27f, 186f, 240f, 22f), s,
            GUI.GetNameOfFocusedControl() == "family_job_name");
        GUI.SetNextControlName("family_job_name");
        nameEdit = GUI.TextField(L(31f, 189f, 232f, 16f),
            nameEdit ?? "", 8, JobUI.Edit(s));
        if (BitmapFont.Ready && sel < 0)
        {
            BitmapFont.DrawOutline(winX + 33f * s, winY + 190f * s, "先在上面选一个职位",
                JobUI.GRAY, s);
        }

        if (JobUI.Btn(L(18f, 210f, 90f, 22f), icoNew, "设定", mouse, s) &&
            sel >= 0 && sel < rows.Count)
        {
            NetClient client = NetClient.I;
            if (client != null)
                client.FamilyPosNameForIndependentRuntime(rows[sel].Id, (nameEdit ?? "").Trim());
        }

        if (JobUI.Btn(L(112f, 210f, 90f, 22f), icoDel, "恢复", mouse, s) &&
            sel >= 0 && sel < rows.Count)
        {
            NetClient client = NetClient.I;
            if (client != null)
                client.FamilyPosNameForIndependentRuntime(rows[sel].Id, "");
            nameEdit = "";
        }

        if (JobUI.Btn(L(204f, 210f, 90f, 22f), icoRight, "权限", mouse, s))
        {
            JobRightDlg.Ensure();
            JobRightDlg.Toggle();
        }

        Rect closeRect = L(279f, 8f, 20f, 19f);
        if (icoClose != null) GUI.DrawTexture(closeRect, icoClose);
        if (JobUI.Click(closeRect, mouse)) visible = false;

        WinDrag.Handle(this, L(0f, 0f, NW, 28f), ref winX, ref winY);

        if (e != null && e.type == EventType.MouseDown &&
            L(0f, 0f, NW, NH).Contains(mouse) && !SysMsgBox.Visible &&
            !JobRightDlg.MouseOver())
        {
            e.Use();
        }
        return;
    }
}
}
