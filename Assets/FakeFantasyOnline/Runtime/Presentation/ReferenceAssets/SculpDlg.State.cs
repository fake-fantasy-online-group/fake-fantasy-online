using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class SculpDlg : MonoBehaviour
{
    public static SculpDlg I;
    private bool visible, tried;
    private string title = "镶嵌炉", actLabel = "镶嵌";
    private Texture2D bg, icoClose, icoMake, icoKeep, icoCancel, icoHole, coinG, coinS, coinC;
    private const float NW = 474f, NH = 521f;
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1;
    private static readonly float[] HoleX = { 125f, 169f, 209f, 255f, 299f };
    public static bool Visible => I != null && I.visible;
    public static void Open(string furnaceTitle = "打孔炉", string action = "打孔")
    {
        if (I == null)
        {
            var go = new GameObject("SculpDlg"); I = go.AddComponent<SculpDlg>(); DontDestroyOnLoad(go);
        }
        I.title = furnaceTitle; I.actLabel = action; I.visible = true; SocketUI.Reset();
    }
    public static void Toggle()
    {
        if (I == null)
        {
            var go = new GameObject("SculpDlg"); I = go.AddComponent<SculpDlg>(); DontDestroyOnLoad(go);
        }
        I.visible = !I.visible;
    }
    public static bool MouseOver()
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        Vector2 mp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(mp);
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private Rect HoleAbs(int i) => L(HoleX[i], 251, 40, 40);
    private void Load()
    {
        tried = true; var loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        bg = loader.LoadGsa("res/uires/composedlg/sculpdlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoMake = loader.LoadGsa("res/uires/composedlg/sculpbtn.gsa");
        icoKeep = loader.LoadGsa("res/uires/composedlg/keepcomposebtn.gsa");
        icoCancel = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
        icoHole = loader.LoadGsa("res/uires/composedlg/sculphole.gsa");
        coinG = loader.LoadGsa("res/uires/common/gold.gsa");
        coinS = loader.LoadGsa("res/uires/common/silver.gsa");
        coinC = loader.LoadGsa("res/uires/common/copper.gsa");
    }
    public static bool TryDropSculp(byte tab, int bagSlot)
    {
        if (tab != 2 || bagSlot < 0 || NetClient.I == null) return false;
        foreach (var item in InventoryDlg.Items[2])
            if (item.bagIdx == bagSlot && item.id > 0) { SocketUI.SelectBag(2, bagSlot, item.id, item.name, item.icon); return true; }
        return false;
    }
    public static bool TryDropRune(byte tab, int bagSlot, int holeIdx)
    {
        if (tab >= InventoryDlg.Items.Length || bagSlot < 0 || NetClient.I == null || !SocketUI.CanInlayHole(holeIdx)) return false;
        foreach (var item in InventoryDlg.Items[tab])
            if (item.bagIdx == bagSlot && item.id > 0) { SocketUI.Inlay(holeIdx, tab, bagSlot); return true; }
        return false;
    }
    private static string DrillDenyWhy()
    {
        if (!SocketUI.HasTarget || SocketUI.CurLevel < 0) return "请先把要打孔的装备拖进装备槽";
        if (SocketUI.Reason.Length > 0) return SocketUI.Reason;
        if (SocketUI.MaxHoles > 0 && SocketUI.HoleCount >= SocketUI.MaxHoles)
            return string.Format("已打满 {0} 孔", SocketUI.MaxHoles);
        foreach (var mat in SocketUI.DrillMats)
            if (mat.have < mat.need) return string.Format("打孔材料不足：{0} {1}/{2}", mat.name, mat.have, mat.need);
        return "当前不能打孔(材料/金钱/工匠技能其一不满足)";
    }
}
}
