using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysConfigDlg : MonoBehaviour
{
    public static SysConfigDlg I;
    private bool visible;
    private bool tried;
    private int pk;
    private int pickQ;
    private bool pickOpen;
    private bool[] opt = new bool[7];
    private bool[] sndOn = { true, true, true };
    private float[] sndVal = { 60f, 50f, 80f };
    private bool loaded;
    private int dragSlider = -1;
    private bool resOpen;
    private int curRes = -1;
    private bool uiBlocked;
    private bool smartCast;
    private bool hpBar;
    private Texture2D drop;
    private Texture2D dropHi;
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoApply;
    private Texture2D icoCancel;
    private Texture2D radioOff;
    private Texture2D radioOn;
    private Texture2D chkOff;
    private Texture2D chkOn;
    private Texture2D knob;
    private const float NW = 267f;
    private const float BG_W = 267f;
    private const float BG_H = 445f;
    private const float EXT_H = 26f;
    private const float RESY = 362f;
    private const float PICKY = 411f;
    private const float SMARTY = 380f;
    private const float HPBARY = 396f;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;
    private static readonly Vector2[] PK =
    {
        new Vector2(38f, 75f), new Vector2(137f, 75f), new Vector2(38f, 95f),
        new Vector2(137f, 95f), new Vector2(38f, 115f)
    };
    private static readonly float[] OPTY = { 143f, 162f, 181f, 200f, 219f, 237f, 257f };
    private static readonly float[] SNDY = { 305f, 325f, 345f };

    public static void Ensure()
    {
        if (I == null)
        {
            GameObject owner = new GameObject("SysConfigDlg");
            I = owner.AddComponent<SysConfigDlg>();
            DontDestroyOnLoad(owner);
        }
    }

    public static void Toggle()
    {
        Ensure();
        I.visible = !I.visible;
        if (I.visible) SysOptionDlg.Hide();
    }

    public static void Hide()
    {
        if (I != null) I.visible = false;
    }

    public static bool Visible => I != null && I.visible;

    private Rect L(float x, float y, float width, float height) =>
        new Rect(winX + x * s, winY + y * s, width * s, height * s);

    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null)
        {
            tried = false;
            return;
        }
        bg = loader.LoadGsa("res/uires/sysoption/sysconfigdlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoApply = loader.LoadGsa("res/uires/common/applybtn.gsa");
        icoCancel = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
        radioOff = loader.LoadGsaFrame("res/uires/common/radiobtn.gsa", 0);
        radioOn = loader.LoadGsaFrame("res/uires/common/radiobtn.gsa", 1);
        chkOff = loader.LoadGsaFrame("res/uires/common/checkbox.gsa", 0);
        chkOn = loader.LoadGsaFrame("res/uires/common/checkbox.gsa", 1);
        knob = loader.LoadGsa("res/uires/common/slidebtn.gsa");
        drop = loader.LoadGsaFrame("res/uires/newcommon/downbtn.gsa", 2) ??
            loader.LoadGsaFrame("res/uires/newcommon/downbtn.gsa", 0);
        dropHi = loader.LoadGsaFrame("res/uires/newcommon/downbtn.gsa", 1) ?? drop;
    }
}

}
