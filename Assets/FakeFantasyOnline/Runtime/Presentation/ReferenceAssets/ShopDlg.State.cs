using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    public struct SItem
    {
        public int id, price, count;
        public byte blocked, quality;
        public string name, desc;
        public Texture2D icon;
        public PetBrief? pet;
    }

    private struct Cart
    {
        public int id, bagIdx, qty, stallIdx;
        public long unit;
        public byte tab, quality;
        public string name, desc;
        public Texture2D icon;
    }

    public static ShopDlg I;
    private static readonly List<SItem> buyItems = new List<SItem>();
    private readonly List<Cart> cart = new List<Cart>();
    private bool visible, sellMode;
    private int shopId;
    private bool stallMode;
    private int stallOwnerId;
    private byte stallType;
    private string stallName = "";
    private readonly Dictionary<int, int> stallIdxByItem = new Dictionary<int, int>();
    private readonly Dictionary<int, (int, long)> stallWant = new Dictionary<int, (int, long)>();
    private Texture2D buyLabelIc;
    private Vector2 scrollL, scrollR;
    private bool picking;
    private int pickId, pickBagIdx, pickMax, pickQty, pickQuality;
    private int pickStallIdx = -1;
    private byte pickTab;
    private string pickName, pickDesc;
    private Texture2D pickIcon;
    private long pickUnit;
    private string pickText = "";
    private const int PickMaxCap = 999;
    private GUIStyle _pickNumSt;
    private string tipText;
    private PetBrief? tipPet;
    private Texture2D bg, goldIc, silverIc, copperIc, closeIc, itemBg, itemBgR;
    private Texture2D repairAll, repairSpec, repairOne, tradeBtn, clearBtn, giveupBtn;
    private Texture2D numBg, inputBtn, minBtn, maxBtn, spinL, spinR, slide;
    private bool pickDrag, triedAssets;
    private const float NW = 569f, NH = 426f;
    private float s;
    private float winX = float.NaN, winY;
    private int _wdGen = -1;

    public static bool Visible => I != null && I.visible;
    public static bool Picking => I != null && I.picking;

    public static void Ensure()
    {
        if (I != null) return;
        var go = new GameObject("ShopDlg");
        I = go.AddComponent<ShopDlg>();
        DontDestroyOnLoad(go);
    }

    public static void OpenSellOnly()
    {
        Ensure();
        I.stallMode = false;
        I.shopId = 0;
        I.sellMode = true;
        I.visible = true;
        I.cart.Clear();
        I.picking = false;
        I.scrollR = Vector2.zero;
        I.scrollL = Vector2.zero;
        buyItems.Clear();
    }

    public static void SetItems(int shop, List<SItem> items)
    {
        Ensure();
        buyItems.Clear();
        buyItems.AddRange(items);
        I.shopId = shop;
    }

    public static void Hide()
    {
        if (I != null) I.visible = false;
    }
}
}
