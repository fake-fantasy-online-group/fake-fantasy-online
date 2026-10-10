using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 JobOfferDlg (授予成员确认窗), recovered from the native 8-method surface
/// (tokens 0x06000565..0x0600056c). Evidence:
/// docs/contracts/VERSION_UPGRADE_1_5_8_JOB_OFFER_DLG_TRANSLATION_ARM64_V1.json and
/// ..._VERIFICATION_X86_V1.json.
/// Logical window 304x236 (the same box as JobCustomDlg), four official .gsa assets - the background
/// falls back to jobcustomdlg.gsa when jobofferdlg.gsa is missing - an empty 240x140 panel with the
/// static 选择成员 label and 授予/取消 plus the close icon. The official type references neither
/// NetClient nor SocialClient and has no selection state, so both buttons only hide the window: it
/// is an unfinished confirm shell in the official client and is reproduced as-is.
/// Like JobCustomDlg the official type has no Ensure: the singleton is created inside
/// <see cref="Toggle"/>. No recovered call site opens this window (only the escape chain reaches
/// Toggle), so the product wires no button to it either.
/// </summary>
public sealed class JobOfferDlg : MonoBehaviour
{
    public static JobOfferDlg I;

    /// <summary>Official const NW; the type has no .cctor in 1.5.8.</summary>
    private const float NW = 304f;

    /// <summary>Official const NH.</summary>
    private const float NH = 236f;

    private bool visible;
    private bool tried;
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoOk;
    private Texture2D icoCancel;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    /// <summary>Official Toggle (0x06000565): create the singleton when it is missing, then flip
    /// visible - nothing else (this window has no editor state to reset and no list to load).</summary>
    public static void Toggle()
    {
        if (I == null)
        {
            GameObject go = new GameObject("JobOfferDlg");
            I = go.AddComponent<JobOfferDlg>();
            Object.DontDestroyOnLoad(go);
        }
        I.visible = !I.visible;
    }

    /// <summary>Official get_Visible (0x06000566).</summary>
    public static bool get_Visible()
    {
        return I != null && I.visible;
    }

    /// <summary>Window-level hit test used by adjacent dialogs (official MouseOver, 0x06000567):
    /// true when the window exists, is visible and the real mouse is inside the 304x236 scaled
    /// rect. A NaN winX means the window has not been placed yet, so the centred rect is used for
    /// both axes.</summary>
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float factor = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y; // top-left origin
        float x = float.IsNaN(I.winX) ? (Screen.width - factor * NW) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - factor * NH) * 0.5f : I.winY;
        return mx >= x && mx < x + factor * NW && my >= y && my < y + factor * NH;
    }

    /// <summary>Official Load (0x06000568): sets tried first and clears it back when the loader is
    /// missing so a later frame retries. The background is the only resource with a fallback chain -
    /// jobofferdlg.gsa first, jobcustomdlg.gsa when that comes back null.</summary>
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null)
        {
            tried = false;
            return;
        }
        Texture2D loaded = loader.LoadGsa("res/uires/family/jobofferdlg.gsa");
        if (object.ReferenceEquals(loaded, null))
        {
            loaded = loader.LoadGsa("res/uires/family/jobcustomdlg.gsa");
        }
        bg = loaded;
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoOk = loader.LoadGsa("res/uires/common/affirmbtn.gsa");
        icoCancel = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
    }

    /// <summary>Rect in scaled window space: (winX + x*s, winY + y*s, w*s, h*s). Official L
    /// 0x06000569; 1.5.8 inlines it at every use site, so the native method has no callers.</summary>
    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    /// <summary>Official Lbl (0x0600056a): DrawOutline at (winX+x*s, winY+y*s) behind
    /// BitmapFont.Ready. 1.5.8 never calls this member; it is restored on the official surface and
    /// intentionally left without a caller.</summary>
    private void Lbl(float x, float y, string text, Color fill)
    {
        if (!BitmapFont.Ready) return;
        BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, fill, s);
    }

    /// <summary>Official automatic Unity callback (0x0600056b). This window retains its
    /// original visible guard and literal GUI.depth 2; it does not use WinZ.</summary>
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

        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(L(27f, 56f, 240f, 140f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (BitmapFont.Ready)
        {
            BitmapFont.DrawOutline(winX + 115f * s, winY + 120f * s, "选择成员", JobUI.GRAY, s);
        }

        // Both buttons only hide the window: the official type references neither NetClient nor
        // SocialClient and has no selection state, so there is nothing to send.
        if (JobUI.Btn(L(55f, 210f, 70f, 22f), icoOk, "授予", mouse, s))
        {
            visible = false;
        }

        if (JobUI.Btn(L(185f, 210f, 70f, 22f), icoCancel, "取消", mouse, s))
        {
            visible = false;
        }

        Rect closeRect = L(279f, 8f, 20f, 19f);
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
