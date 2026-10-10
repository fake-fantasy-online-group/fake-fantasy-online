using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 FamilyRelationDlg (家族关系窗口), recovered from the native 9-method surface
/// (tokens 0x0600037f..0x06000387). Evidence:
/// docs/contracts/VERSION_UPGRADE_1_5_8_FAMILY_RELATION_DLG_TRANSLATION_ARM64_V1.json and
/// docs/contracts/VERSION_UPGRADE_1_5_8_FAMILY_RELATION_DLG_VERIFICATION_X86_V1.json.
/// Logical window 304x431, two official .gsa assets, two empty panels carrying the official
/// 暂无盟友 / 暂无敌对 labels and a close icon. The official window has no data source at all --
/// the type declares no relation collection and its OnGUI reads no client static -- so the empty
/// state is the whole content and is reproduced as-is. The window is opened by FamilyDetailDlg's
/// 关系 button and closed by UiEsc.CloseTop (the product owner's escape chain).
/// </summary>
public sealed class FamilyRelationDlg : MonoBehaviour
{
    public static FamilyRelationDlg I;

    /// <summary>Official const NW; the type has no .cctor in 1.5.8.</summary>
    private const float NW = 304f;

    /// <summary>Official const NH.</summary>
    private const float NH = 431f;

    private bool visible;
    private Texture2D bg;
    private Texture2D icoClose;
    private bool tried;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    /// <summary>Official Ensure (0x0600037f): lazily builds the singleton GameObject.</summary>
    public static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("FamilyRelationDlg");
        I = go.AddComponent<FamilyRelationDlg>();
        Object.DontDestroyOnLoad(go);
    }

    /// <summary>Official Toggle (0x06000380): Ensure plus a visible flip and nothing else. Unlike
    /// FamilyDetailDlg this window never refreshes on the hidden->visible edge, because it has no
    /// server-backed content.</summary>
    public static void Toggle()
    {
        Ensure();
        I.visible = !I.visible;
    }

    /// <summary>Official get_Visible (0x06000381).</summary>
    public static bool get_Visible()
    {
        return I != null && I.visible;
    }

    /// <summary>Window-level hit test used by adjacent dialogs (official MouseOver, 0x06000382):
    /// true when the window exists, is visible and the real mouse is inside the 304x431 scaled
    /// rect. A NaN winX means the window has not been placed yet, so the centred rect is used for
    /// both axes.</summary>
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float factor = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = I.winX;
        if (float.IsNaN(x))
        {
            x = (Screen.width - factor * NW) * 0.5f;
        }
        float y = float.IsNaN(I.winX)
            ? (Screen.height - factor * NH) * 0.5f
            : I.winY;
        return mx >= x && mx < x + factor * NW && my >= y && my < y + factor * NH;
    }

    /// <summary>Official Load (0x06000383): sets tried first and clears it back when the loader is
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
        bg = loader.LoadGsa("res/uires/family/familyrelationdlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
    }

    /// <summary>Rect in scaled window space: (winX + x*s, winY + y*s, w*s, h*s). Official
    /// L 0x06000384; 1.5.8 inlines it at every use site, so the native method has no callers.</summary>
    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    /// <summary>Official Lbl (0x06000385): DrawOutline at (winX+x*s, winY+y*s) behind
    /// BitmapFont.Ready. 1.5.8 never calls this member (its two labels are drawn inline in OnGUI);
    /// it is restored on the official surface and intentionally left without a caller.</summary>
    private void Lbl(float x, float y, string text, Color fill)
    {
        if (!BitmapFont.Ready) return;
        BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, fill, s);
    }

    /// <summary>Official OnGUI (0x06000386), invoked by Unity on the lazy persistent owner.</summary>
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

        Rect alliesPanel = L(25f, 74f, 242f, 142f);
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(alliesPanel, Texture2D.whiteTexture);
        GUI.color = Color.white;
        Color alliesColor = FamUI.Gray;
        if (BitmapFont.Ready)
        {
            BitmapFont.DrawOutline(winX + 120f * s, winY + 138f * s, "暂无盟友", alliesColor, s);
        }

        Rect enemiesPanel = L(25f, 272f, 242f, 142f);
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(enemiesPanel, Texture2D.whiteTexture);
        GUI.color = Color.white;
        Color enemiesColor = FamUI.Gray;
        if (BitmapFont.Ready)
        {
            BitmapFont.DrawOutline(winX + 120f * s, winY + 336f * s, "暂无敌对", enemiesColor, s);
        }

        Rect closeRect = L(279f, 9f, 20f, 19f);
        if (icoClose != null) GUI.DrawTexture(closeRect, icoClose);
        if (FamUI.Click(closeRect, mouse)) visible = false;

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
