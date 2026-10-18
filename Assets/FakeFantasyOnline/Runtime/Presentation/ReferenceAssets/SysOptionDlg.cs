using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysOptionDlg : MonoBehaviour
{
    public static SysOptionDlg I;
    private bool visible;
    private bool tried;
    private Texture2D bg;
    private Texture2D bReturn;
    private Texture2D bRelive;
    private Texture2D bSelPlayer;
    private Texture2D bSelServer;
    private Texture2D bSysConfig;
    private Texture2D bQuit;
    private const float NW = 192f;
    private const float NH = 207f;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    public static void Ensure()
    {
        if (I == null)
        {
            GameObject owner = new GameObject("SysOptionDlg");
            I = owner.AddComponent<SysOptionDlg>();
            DontDestroyOnLoad(owner);
        }
    }

    public static void Toggle()
    {
        Ensure();
        I.visible = !I.visible;
        if (I.visible) SysConfigDlg.Hide();
    }

    public static void Open()
    {
        Ensure();
        I.visible = true;
        SysConfigDlg.Hide();
    }

    public static void Hide()
    {
        if (I != null) I.visible = false;
    }

    // Official 0x06000e2f: the frozen body is a single ChatUI.Add with the channel literal
    // "系统", the caller's text, the amber colour (1, 220/255, 130/255, 1), an empty who and
    // a null share list. The colour is read as arm64 immediates 0x3f800000 / 0x3f5cdcdd /
    // 0x3f028283 / 0x3f800000 and cross-read at x86_64 0x1118af0 (1.0, 0.8627451) and
    // 0x111a110 (0.509804, 1.0).
    public static void Notice(string t)
    {
        ChatUI.Add("系统", t, new Color(1f, 220f / 255f, 130f / 255f, 1f), "", null);
    }

    public static bool Visible => I != null && I.visible;

    // Reuse the detailed settings safe-area fit without changing the user's UI scale.
    private Rect MobileLayout(out float scale)
    {
        Rect safe = MobileClientPresentation.SafeAreaGui;
        scale = Mathf.Min(UIScale.Factor, Mathf.Min(safe.width / NW, safe.height / NH));
        float width = NW * scale, height = NH * scale;
        bool centered = float.IsNaN(winX) || _wdGen != WinDrag.Gen;
        float x = centered ? safe.x + (safe.width - width) * .5f : winX;
        float y = centered ? safe.y + (safe.height - height) * .5f : winY;
        return new Rect(Mathf.Clamp(x, safe.x, safe.xMax - width),
            Mathf.Clamp(y, safe.y, safe.yMax - height), width, height);
    }

    // GUI-space, explicit physical-pointer hit testing; independent of Input.mousePosition.
    internal static bool MobileContainsPointer(Vector2 point)
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        float width = NW, height = NH;
        bool centered = float.IsNaN(I.winX) || I._wdGen != WinDrag.Gen;
        float x = centered ? (Screen.width - width * scale) * .5f : I.winX;
        float y = centered ? (Screen.height - height * scale) * .5f : I.winY;
        if (MobileClientPresentation.Enabled) return I.MobileLayout(out scale).Contains(point);
        if (new Rect(x, y, width * scale, height * scale).Contains(point)) return true;
        return false;
    }


    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        if (MobileClientPresentation.Enabled)
            return MobileContainsPointer(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        float scale = UIScale.Factor;
        float px = Input.mousePosition.x;
        float py = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        // Original code uses winX's NaN state for both coordinate fallbacks.
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(new Vector2(px, py));
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
        bg = loader.LoadGsa("res/uires/sysoption/sysoptiondlg.gsa");
        bReturn = loader.LoadGsaFrame("res/uires/sysoption/returngame.gsa", 0);
        bRelive = loader.LoadGsaFrame("res/uires/sysoption/reliveplayer.gsa", 0);
        bSelPlayer = loader.LoadGsaFrame("res/uires/sysoption/selplayer.gsa", 0);
        bSelServer = loader.LoadGsaFrame("res/uires/sysoption/selserver.gsa", 0);
        bSysConfig = loader.LoadGsaFrame("res/uires/sysoption/sysconfig.gsa", 0);
        bQuit = loader.LoadGsaFrame("res/uires/sysoption/quitgame.gsa", 0);
    }

    private Rect L(float x, float y, float width, float height) =>
        new Rect(winX + x * s, winY + y * s, width * s, height * s);
}

}
