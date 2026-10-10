using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class AttrWashDlg : MonoBehaviour
{
    public static AttrWashDlg I;
    private bool visible, tried;
    private const float NW = 343f, NH = 426f;
    private float s;
    private float winX = float.NaN, winY;
    private int _wdGen = -1;
    private Texture2D bg, icoClose;
    private int tKind = -1, tTab, tSlot, tItemId, quality;
    private string tName = "";
    private Texture2D tIcon;
    private List<string> affixes = new List<string>();
    private List<bool> atMax = new List<bool>();

    public static bool Visible => I != null && I.visible;
    public static bool Picking => I != null && I.visible;

    public static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("AttrWashDlg");
        I = go.AddComponent<AttrWashDlg>();
        DontDestroyOnLoad(go);
    }

    public static void Open()
    {
        Ensure();
        I.visible = true;
        I.tKind = -1;
        I.tIcon = null;
        I.tName = "";
        I.affixes = new List<string>();
        I.atMax = new List<bool>();
    }

    public static void CloseNow()
    {
        if (I != null) I.visible = false;
    }

    public static void OnInfo(int kind, int tab, int slot, int itemId, int q,
        List<string> affs, List<bool> maxFlags = null)
    {
        if (I == null) return;
        I.tKind = kind;
        I.tTab = tab;
        I.tSlot = slot;
        I.tItemId = itemId;
        I.quality = q;
        I.affixes = affs ?? new List<string>();
        I.atMax = maxFlags ?? new List<bool>();
    }

    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(mouse);
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
        bg = loader.LoadGsa2021First("res/uires/transportdlg/transportdlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
    }
}
}
