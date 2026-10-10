using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipFuseDlg : MonoBehaviour
{
    public static EquipFuseDlg I;
    private bool visible, tried;
    private float s;
    private float winX = float.NaN, winY;
    private int _wdGen = -1;
    private Texture2D bg, icoClose;
    private const float NW = 420f, NH = 400f, CX0 = 42f, CW = 336f;
    private int mainTab = -1, mainSlot, mainId, mainQ;
    private string mainName = "", mainDesc = "";
    private Texture2D mainIcon;
    private int subTab = -1, subSlot, subId, subQ;
    private string subName = "", subDesc = "";
    private Texture2D subIcon;
    private bool pickSub, ok;
    private string tip = "", confirmText = "";
    private int sig;

    public static bool Visible => I != null && I.visible;
    public static bool Picking => I != null && I.visible;

    public static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("EquipFuseDlg");
        I = go.AddComponent<EquipFuseDlg>();
        DontDestroyOnLoad(go);
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

    public static void CloseNow()
    {
        if (I != null) I.visible = false;
    }
}
}
