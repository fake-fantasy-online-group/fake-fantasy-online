using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 FamilyCreateDlg (创建家族窗口), recovered from the native 9-method surface
/// (tokens 0x06000363..0x0600036b). Evidence:
/// docs/contracts/VERSION_UPGRADE_1_5_8_FAMILY_CREATE_DLG_TRANSLATION_ARM64_V1.json and
/// docs/contracts/VERSION_UPGRADE_1_5_8_FAMILY_CREATE_DLG_VERIFICATION_X86_V1.json.
/// Logical window 262x390, five official .gsa assets, one 16-character family-name field, one
/// 200-character declaration area, a 选择 button, 确定/取消 and a close icon. The window is opened
/// by FamilyDlg's 创建 button and closes after NetClient.FamilyCreate(name, declaration).
/// </summary>
public sealed class FamilyCreateDlg : MonoBehaviour
{
    public static FamilyCreateDlg I;

    private const float Width = 262f;
    private const float Height = 390f;

    private bool visible;
    private string nameField = "";
    private string declaration = "";
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoList;
    private Texture2D icoAffirm;
    private Texture2D icoCancel;
    private bool tried;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;
    private GUIStyle _edit;

    public static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("FamilyCreateDlg");
        I = go.AddComponent<FamilyCreateDlg>();
        Object.DontDestroyOnLoad(go);
    }

    public static void Toggle()
    {
        Ensure();
        I.visible = !I.visible;
    }

    public static bool get_Visible()
    {
        return I != null && I.visible;
    }

    /// <summary>Window-level hit test used by adjacent dialogs (official MouseOver): true when the
    /// window exists, is visible and the real mouse is inside the 262x390 scaled rect. A NaN winX
    /// means the window has not been placed yet, so the centred rect is used for both axes.</summary>
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float factor = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = I.winX;
        if (float.IsNaN(x))
        {
            x = (Screen.width - factor * Width) * 0.5f;
        }
        float y = float.IsNaN(I.winX)
            ? (Screen.height - factor * Height) * 0.5f
            : I.winY;
        return mx >= x && mx < x + factor * Width && my >= y && my < y + factor * Height;
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
        bg = loader.LoadGsa("res/uires/family/familycreatedlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoList = loader.LoadGsa("res/uires/family/listbtn.gsa");
        icoAffirm = loader.LoadGsa("res/uires/common/affirmbtn.gsa");
        icoCancel = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
    }

    /// <summary>Rect in scaled window space: (winX + x*s, winY + y*s, w*s, h*s).</summary>
    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    /// <summary>Official Edit(): lazily built shared edit style (CnFont, MiddleLeft, white text)
    /// whose font size tracks the UI scale at 12 px per factor step.</summary>
    private GUIStyle Edit()
    {
        if (_edit == null)
        {
            _edit = new GUIStyle(GUIStyle.none)
            {
                font = NpcChatter.CnFont(),
                alignment = TextAnchor.MiddleLeft
            };
            _edit.normal.textColor = Color.white;
        }
        _edit.fontSize = (int)System.Math.Round((double)(s * 12f));
        return _edit;
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();

        (int depth, bool covered) = WinZ.Frame(this, new Rect(winX, winY, s * Width, s * Height), 10);
        Vector2 saved = Event.current.mousePosition;
        if (covered) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen))
            {
                winX = (Screen.width - s * Width) * 0.5f;
                winY = (Screen.height - s * Height) * 0.5f;
            }
            Vector2 mouse = Event.current.mousePosition;
            GUI.depth = depth;

            if (bg == null)
            {
                GUI.color = new Color(0.1f, 0.09f, 0.13f, 0.98f);
                GUI.DrawTexture(L(0f, 0f, Width, Height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            else
            {
                GUI.DrawTexture(L(0f, 0f, Width, Height), bg);
            }

            GUI.SetNextControlName("fc_name");
            Rect nameBg = L(29f, 60f, 214f, 12f);
            GUI.color = new Color(0f, 0f, 0f, 0.3f);
            GUI.DrawTexture(nameBg, Texture2D.whiteTexture);
            GUI.color = Color.white;
            nameField = GUI.TextField(
                new Rect(nameBg.x + 3f, nameBg.y, nameBg.width - 6f, nameBg.height),
                nameField ?? "", 16, Edit());

            GUI.color = new Color(0f, 0f, 0f, 0.25f);
            GUI.DrawTexture(L(28f, 103f, 36f, 36f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            FamUI.Btn(L(70f, 120f, 44f, 22f), icoList, "选择", mouse, s);

            Rect declBg = L(27f, 167f, 200f, 180f);
            GUI.color = new Color(0f, 0f, 0f, 0.3f);
            GUI.DrawTexture(declBg, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle area = new GUIStyle(Edit());
            area.alignment = TextAnchor.UpperLeft;
            area.wordWrap = true;
            declaration = GUI.TextArea(
                new Rect(declBg.x + 3f, declBg.y + 2f, declBg.width - 6f, declBg.height - 4f),
                declaration ?? "", 200, area);

            if (FamUI.Btn(L(38f, 363f, 70f, 22f), icoAffirm, "确定", mouse, s))
            {
                if (NetClient.I != null)
                {
                    if ((nameField ?? "").Trim().Length > 0)
                    {
                        NetClient.I.FamilyCreateForIndependentRuntime(nameField.Trim(), declaration ?? "");
                        visible = false;
                    }
                }
            }
            if (FamUI.Btn(L(160f, 363f, 70f, 22f), icoCancel, "取消", mouse, s))
            {
                visible = false;
            }

            Rect closeRect = L(237f, 9f, 20f, 19f);
            if (icoClose != null) GUI.DrawTexture(closeRect, icoClose);
            if (FamUI.Click(closeRect, mouse)) visible = false;

            WinDrag.Handle(this, L(0f, 0f, Width, 28f), ref winX, ref winY);

            if (Event.current.type == EventType.MouseDown &&
                L(0f, 0f, Width, Height).Contains(mouse) && !SysMsgBox.Visible)
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
}
}
