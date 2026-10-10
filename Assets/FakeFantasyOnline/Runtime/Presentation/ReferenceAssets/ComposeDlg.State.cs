using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ComposeDlg : MonoBehaviour
{
    internal sealed class Mat
    {
        public int id, need, have;
        public string name;
        public Texture2D icon;
    }
    internal sealed class Recipe
    {
        public int result, cost;
        public string name;
        public bool canMake;
        public List<Mat> mats;
        public Texture2D icon;
    }
    public static ComposeDlg I;
    private bool visible, tried;
    private int modtype, sel = -1;
    private static List<Recipe> recipes = new List<Recipe>();
    private static readonly int[] MatSlotX = { 118, 161, 204, 247, 291 };
    private Texture2D bg, icoClose, icoCancel, icoMake, icoKeep, coinG, coinS, coinC;
    private Vector2 scroll;
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1;
    private const float NW = 450f, NH = 484f;
    public static bool Visible => I != null && I.visible;
    public static void Open(int mt)
    {
        if (I == null)
        {
            var go = new GameObject("ComposeDlg"); I = go.AddComponent<ComposeDlg>(); DontDestroyOnLoad(go);
        }
        I.modtype = mt; I.visible = true; I.sel = -1; recipes = new List<Recipe>();
        if (mt == 1) CraftUpgradeUI.Reset(0);
        else if (NetClient.I != null) NetClient.I.OpenFurnaceForIndependentRuntime((byte)mt);
    }
    public static void Toggle()
    {
        if (I == null)
        {
            var go = new GameObject("ComposeDlg"); I = go.AddComponent<ComposeDlg>(); DontDestroyOnLoad(go);
        }
        if (I.visible) I.visible = false;
        else Open(0);
    }
    public static void SetRecipes(int mt, List<Recipe> list)
    {
        if (I == null) return;
        int previous = I.sel >= 0 && I.sel < recipes.Count ? recipes[I.sel].result : -1;
        I.modtype = mt; recipes = list ?? new List<Recipe>();
        I.sel = previous < 0 ? -1 : recipes.FindIndex(recipe => recipe.result == previous);
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    public static bool MouseOver()
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        Vector2 mp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(mp);
    }
    public static bool TryDropRefine(byte tab, int bagSlot, Vector2 mp)
    {
        if (!Visible || I.modtype != 1 || tab != 2) return false;
        float scale = UIScale.Factor;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        if (!new Rect(x + 103f * scale, y + 78f * scale, 220f * scale, 215f * scale).Contains(mp)) return false;
        foreach (var item in InventoryDlg.Items[2])
            if (item.bagIdx == bagSlot) { CraftUpgradeUI.LoadBagItem(2, bagSlot, item.id, item.name, item.icon); break; }
        return true;
    }
    private void Load()
    {
        tried = true; var loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        bg = loader.LoadGsa("res/uires/composedlg/composedlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoMake = loader.LoadGsa("res/uires/composedlg/composebtn.gsa");
        icoKeep = loader.LoadGsa("res/uires/composedlg/keepcomposebtn.gsa");
        icoCancel = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
        coinG = loader.LoadGsa("res/uires/common/gold.gsa");
        coinS = loader.LoadGsa("res/uires/common/silver.gsa");
        coinC = loader.LoadGsa("res/uires/common/copper.gsa");
    }
    private static string RefineDenyWhy()
    {
        if (CraftUpgradeUI.RefBagSlot < 0) return "请先把要加工的装备从背包拖进炉中";
        if (CraftUpgradeUI.CurLevel == -1) return "正在查询该装备的加工信息, 请稍候";
        if (CraftUpgradeUI.CurLevel == -2) return "该装备已满级或不可加工";
        if (CraftUpgradeUI.CurLevel == -3) return "工匠熟练度不足, 无法加工此装备";
        foreach (var mat in CraftUpgradeUI.Mats)
            if (mat.have < mat.need) return string.Format("加工材料不足：{0} {1}/{2}", mat.name, mat.have, mat.need);
        return InventoryDlg.Money < CraftUpgradeUI.Cost ? "金钱不足, 无法加工" : "当前不能加工";
    }
    private string CombineDenyWhy()
    {
        if (recipes.Count == 0) return "当前没有可以合成的配方(背包里要先有对应材料)";
        if (sel < 0 || sel >= recipes.Count) return "请先在列表里选一个要合成的物品";
        var recipe = recipes[sel];
        if (recipe.mats != null)
            foreach (var mat in recipe.mats)
                if (mat.have < mat.need) return string.Format("合成材料不足：{0} {1}/{2}", mat.name, mat.have, mat.need);
        return InventoryDlg.Money < recipe.cost ? "金钱不足, 无法合成" : "当前不能合成";
    }
}
}
