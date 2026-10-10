using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class StallDetailDlg : MonoBehaviour
{
    private static StallDetailDlg I;
    private bool visible, tried;
    private byte bagTab;
    private int bagSlot, itemId, maxCount, quality;
    private string itemName = "", itemDesc = "";
    private Texture2D icon;
    private long refPrice;
    private string gold = "0", silver = "0", copper = "0", count = "1";
    private Texture2D bg, okBtn, cancelBtn, closeIc, goldIc, silverIc, copperIc;
    private GUIStyle edit;
    private float winX = float.NaN;
    private float winY, s;
    private int _wdGen = -1;
    private const float NW = 255f, NH = 212f;
    private const int PetSoulItemId = 0x0d39;
    private static readonly Color GOLD = new Color(1f, 0.92f, 0.66f, 1f);
    private static readonly Color GRAY = new Color(0.62f, 0.6f, 0.52f, 1f);

    private static void Ensure()
    {
        if (I != null) return;
        var go = new GameObject("StallDetailDlg");
        I = go.AddComponent<StallDetailDlg>();
        DontDestroyOnLoad(go);
    }
    public static bool Active => I != null && I.visible;
    public static void CloseNow() { if (I != null) I.visible = false; }
    public static void Open(byte bagTab, int bagSlot, int itemId, int maxCount, string name,
        Texture2D icon, long refP, int quality = 0, string desc = "")
    {
        Ensure();
        I.bagTab = bagTab;
        I.bagSlot = bagSlot;
        I.itemId = itemId;
        I.maxCount = Mathf.Max(1, maxCount);
        I.itemName = name;
        I.icon = icon;
        I.refPrice = refP;
        I.quality = quality;
        I.itemDesc = desc;
        I.gold = ((refP / 1000000) % 1000000).ToString();
        I.silver = ((refP / 1000) % 1000).ToString();
        I.copper = (refP % 1000).ToString();
        I.count = "1";
        I.visible = true;
    }
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(new Vector2(mx, my));
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private static int PI(string v) => int.TryParse(v, out int value) ? Mathf.Max(0, value) : 0;
}
}
