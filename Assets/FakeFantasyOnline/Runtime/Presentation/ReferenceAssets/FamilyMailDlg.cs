using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 FamilyMailDlg (家族邮件窗口), recovered from the native 10-method surface
/// (tokens 0x06000579..0x06000582). Evidence:
/// docs/contracts/VERSION_UPGRADE_1_5_8_FAMILY_MAIL_DLG_TRANSLATION_ARM64_V1.json and
/// ..._VERIFICATION_X86_V1.json.
/// Logical window 280x240: a title field, a body field, 发送/取消 and a close icon. Opened by
/// FamilyDetailDlg's 家族邮件 button; unlike every other recovered family window it declares no
/// Toggle and no get_Visible, so Open is the only official way in and the three close controls the
/// only official way out. Buttons and hit tests go through the official JobUI toolkit.
/// The official type therefore has no escape-chain entry; the product adds Escape closing as the
/// approved deviation FAMILYMAIL-ESC-001 (docs/project/APPROVED_DEVIATIONS.yaml), exposed through
/// the two product-only members get_Visible() and Hide().
/// </summary>
public sealed class FamilyMailDlg : MonoBehaviour
{
    public static FamilyMailDlg I;

    /// <summary>Official const NW; the type has no .cctor in 1.5.8.</summary>
    private const float NW = 280f;

    /// <summary>Official const NH.</summary>
    private const float NH = 240f;

    private bool visible;
    private bool tried;
    private Texture2D icoClose;
    private Texture2D icoOk;
    private Texture2D icoCancel;
    private GUIStyle _t;
    private GUIStyle _b;
    private string title = "";
    private string body = "";
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    /// <summary>Official Ensure (0x06000579): lazily builds the singleton GameObject.</summary>
    public static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("FamilyMailDlg");
        I = go.AddComponent<FamilyMailDlg>();
        Object.DontDestroyOnLoad(go);
    }

    /// <summary>Official Open (0x0600057a): Ensure and show. The official body performs no reset and
    /// issues no request, so whatever the previous session left in the two fields stays there.</summary>
    public static void Open()
    {
        Ensure();
        I.visible = true;
    }

    /// <summary>PRODUCT-ONLY, not an official member. Official 1.5.8 declares no get_Visible for
    /// this type (which is why UiEsc.CloseTop cannot close it), so the product adds this accessor
    /// solely to serve the approved Escape-close deviation recorded as FAMILYMAIL-ESC-001 in
    /// docs/project/APPROVED_DEVIATIONS.yaml. Shape mirrors JobRightDlg.get_Visible.</summary>
    public static bool get_Visible()
    {
        return I != null && I.visible;
    }

    /// <summary>PRODUCT-ONLY, not an official member. Hides the window without touching the title
    /// or body fields - the same effect 取消 and the close icon have. Added for the approved
    /// Escape-close deviation FAMILYMAIL-ESC-001.</summary>
    public static void Hide()
    {
        if (I == null) return;
        I.visible = false;
    }

    /// <summary>Window-level hit test used by adjacent dialogs (official MouseOver, 0x0600057b):
    /// true when the window exists, is visible and the real mouse is inside the 280x240 scaled
    /// rect. A NaN winX means the window has not been placed yet, so the centred rect is used for
    /// both axes.</summary>
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float factor = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = I.winX;
        if (float.IsNaN(x)) x = (Screen.width - factor * NW) * 0.5f;
        float y = float.IsNaN(I.winX) ? (Screen.height - factor * NH) * 0.5f : I.winY;
        return mx >= x && mx < x + factor * NW && my >= y && my < y + factor * NH;
    }

    /// <summary>Official Load (0x0600057c): sets tried first and clears it back when the loader is
    /// missing so a later frame retries. The window has no .gsa of its own in 1.5.8; the official
    /// background is the JobCustomDlg one.</summary>
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null)
        {
            tried = false;
            return;
        }
        // The role editor background has baked role headings, so mail uses a blank panel.
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoOk = loader.LoadGsa("res/uires/common/sendbtn.gsa");
        icoCancel = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
    }

    /// <summary>Rect in scaled window space: (winX + x*s, winY + y*s, w*s, h*s). Official L
    /// 0x0600057d; 1.5.8 inlines it at every use site, so the native method has no callers.</summary>
    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    /// <summary>Official Lbl (0x0600057e): DrawOutline at (winX+x*s, winY+y*s) behind
    /// BitmapFont.Ready. 1.5.8 never calls this member; it is restored on the official surface and
    /// intentionally left without a caller.</summary>
    private void Lbl(float x, float y, string text, Color fill)
    {
        if (!BitmapFont.Ready) return;
        BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, fill, s);
    }

    /// <summary>Official TS (0x0600057f): the lazily built single-line title style (CnFont,
    /// MiddleLeft, white) whose font size tracks the UI scale at 12 px per factor step.</summary>
    private GUIStyle TS()
    {
        if (_t == null)
        {
            _t = new GUIStyle(GUIStyle.none)
            {
                font = NpcChatter.CnFont(),
                alignment = TextAnchor.MiddleLeft
            };
            _t.normal.textColor = Color.white;
        }
        _t.fontSize = Mathf.RoundToInt(s * 12f);
        return _t;
    }

    /// <summary>Official BS (0x06000580): the lazily built body style - same as TS but UpperLeft
    /// and word-wrapped, because the mail body is a multi-line TextArea.</summary>
    private GUIStyle BS()
    {
        if (_b == null)
        {
            _b = new GUIStyle(GUIStyle.none)
            {
                font = NpcChatter.CnFont(),
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            _b.normal.textColor = Color.white;
        }
        _b.fontSize = Mathf.RoundToInt(s * 12f);
        return _b;
    }

    /// <summary>Official OnGUI body, called automatically by Unity. This window does not use
    /// WinZ: the official sets the literal GUI.depth 1.</summary>
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

        ArtworkSkin.Panel(L(0f, 0f, NW, NH), s);

        Color labelColor = JobUI.GOLD;
        if (BitmapFont.Ready)
        {
            BitmapFont.DrawOutline(winX + 20f * s, winY + 34f * s, "家族邮件(发送给全体成员)",
                labelColor, s);
        }
        labelColor = JobUI.GRAY;
        if (BitmapFont.Ready)
        {
            BitmapFont.DrawOutline(winX + 20f * s, winY + 58f * s, "标题", labelColor, s);
        }

        Rect titleRect = L(56f, 52f, 200f, 24f);
        ArtworkSkin.Input(titleRect, s, GUI.GetNameOfFocusedControl() == "family_mail_title");
        GUI.SetNextControlName("family_mail_title");
        // Match the artwork border at every UI scale while keeping the same text limit.
        title = GUI.TextField(
            new Rect(titleRect.x + 4f * s, titleRect.y + 4f * s, titleRect.width - 8f * s, titleRect.height - 8f * s),
            title ?? "", 30, TS());

        labelColor = JobUI.GRAY;
        if (BitmapFont.Ready)
        {
            BitmapFont.DrawOutline(winX + 20f * s, winY + 80f * s, "正文", labelColor, s);
        }

        Rect bodyRect = L(20f, 96f, 236f, 96f);
        ArtworkSkin.Input(bodyRect, s, GUI.GetNameOfFocusedControl() == "family_mail_body");
        GUI.SetNextControlName("family_mail_body");
        body = GUI.TextArea(
            new Rect(bodyRect.x + 4f * s, bodyRect.y + 4f * s, bodyRect.width - 8f * s, bodyRect.height - 8f * s),
            body ?? "", 300, BS());

        if (JobUI.Btn(L(50f, 206f, 70f, 22f), icoOk, "发送", mouse, s))
        {
            // The two emptiness tests are nested in the official body, so an empty title with a
            // non-empty body still sends.
            if (string.IsNullOrEmpty((title ?? "").Trim()) &&
                string.IsNullOrEmpty((body ?? "").Trim()))
            {
                ChatUI.Add("系统", "邮件内容不能为空", new Color(1f, 0.8627f, 0.5882f, 1f), "");
            }
            else
            {
                if (NetClient.I != null) NetClient.I.FamilyMailForIndependentRuntime(title ?? "", body ?? "");
                title = "";
                body = "";
                visible = false;
            }
        }

        if (JobUI.Btn(L(160f, 206f, 70f, 22f), icoCancel, "取消", mouse, s))
        {
            visible = false;
        }

        Rect closeRect = L(255f, 8f, 20f, 19f);
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
