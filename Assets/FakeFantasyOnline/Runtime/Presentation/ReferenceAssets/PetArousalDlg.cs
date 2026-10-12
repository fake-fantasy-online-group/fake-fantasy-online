using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 PetArousalDlg 0x06000932–0x0600093b.
internal sealed class PetArousalDlg : MonoBehaviour
{
    public static PetArousalDlg I;
    private bool visible, tried;
    private Texture2D bg, okBtn, cancelBtn, closeIc;
    private const float NW = 278f, NH = 259f;
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1;
    // Original static initializer fields 3413A047...5638C and 7479A56F...722D.
    private static readonly int[] Succ = { 0, 100, 100, 95, 90, 80, 70, 60, 50, 40, 30 };
    private static readonly int[] Step = { 0, 5, 10, 10, 10, 15, 15, 15, 20, 20, 40 };

    private static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("PetArousalDlg");
        I = go.AddComponent<PetArousalDlg>();
        DontDestroyOnLoad(go);
    }
    public static bool Visible => I != null && I.visible;
    public static void Toggle() { Ensure(); I.visible = !I.visible; }
    public static void Open() { Ensure(); I.visible = true; }
    internal static void CloseForIndependentRuntime() { if (I != null) I.visible = false; }
    public static bool MouseOver()
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        float mouseX = Input.mousePosition.x;
        float mouseY = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(new Vector2(mouseX, mouseY));
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        bg = loader.LoadUI("ui/petarousaldlg/petarousaldlg.png");
        if (bg == null) bg = loader.LoadGsa("res/uires/petdlg/petarousaldlg.gsa");
        okBtn = loader.LoadGsa("res/uires/common/affirmbtn.gsa");
        cancelBtn = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
        closeIc = loader.LoadGsa("res/uires/common/miniclose.gsa");
    }
    private void Text(float x, float y, string text, Color color)
    {
        BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, color, s);
    }
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * 0.5f;
            winY = (Screen.height - NH * s) * 0.5f;
        }
        var stacking = WinZ.Frame(this, L(0, 0, NW, NH), 10);
        Vector2 originalMouse = Event.current.mousePosition;
        if (stacking.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        Color savedColor = GUI.color;
        int savedDepth = GUI.depth;
        try
        {
            Vector2 mp = Event.current.mousePosition;
            GUI.depth = stacking.Item1;
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else PopUI.Panel(L(0, 0, NW, NH));
            int level = Mathf.Clamp(CharData.petArousal, 0, 10);
            bool missing = CharData.petModel < 1 || string.IsNullOrEmpty(CharData.petName);
            bool full = CharData.petArousal >= 10;
            Color gold = new Color(254f / 255f, 217f / 255f, 0f, 1f);
            if (missing || full)
            {
                if (BitmapFont.Ready) Text(160, 80, missing ? "请先出战宠物" : "已满觉醒 +10",
                    missing ? new Color(1f, 0.6f, 0.5f, 1f) : gold);
            }
            else
            {
                Texture2D book = NetClient.LoadItemIcon(10759 + level);
                if (book != null) GUI.DrawTexture(L(121, 68, 36, 36), book);
                if (BitmapFont.Ready)
                {
                    Text(160, 80, string.Format("+{0}涅磐之书", level + 1), gold);
                    Text(122, 127, Succ[level + 1] + "%", new Color(0.6f, 1f, 0.6f, 1f));
                    Text(234, 127, (100 - Succ[level + 1]) + "%", new Color(1f, 0.6f, 0.6f, 1f));
                }
            }
            Texture2D icon = NetClient.LoadItemIcon(10758);
            if (icon != null) GUI.DrawTexture(L(27, 156, 36, 36), icon);
            // The original artwork says failure destroys/downgrades the pet. Keep it
            // fully obscured beneath the unlabelled skin: patchmiddle2 is translucent.
            Color previous = GUI.color;
            GUI.color = new Color(.055f, .048f, .041f, 1f);
            GUI.DrawTexture(L(86, 142, 172, 76), Texture2D.whiteTexture);
            GUI.color = previous;
            ArtworkSkin.Panel(L(80, 140, 184, 84), s);
            if (BitmapFont.Ready)
            {
                int points = missing || full ? 0 : Step[level + 1];
                Color red = new Color(200f / 255f, 40f / 255f, 30f / 255f, 1f);
                Text(88, 148, "成功：宠物觉醒等级提升1级，", red);
                Text(112, 166, points > 0 ? string.Format("获得{0}点自由属性。", points) : "获得自由属性点。", red);
                Text(88, 184, "失败：只消耗道具，觉醒等级", red);
                Text(112, 202, "不变，不会降级爆掉。", red);
            }
            if (!missing && !full && PopUI.Btn(L(42, 231, 69, 22), okBtn, "觉醒", mp, s))
                if (NetClient.I != null) NetClient.I.PetAwaken();
            if (PopUI.Btn(L(160, 231, 69, 22), cancelBtn, "取消", mp, s)) visible = false;
            Rect close = L(254, 12, 20, 19);
            if (closeIc != null) GUI.DrawTexture(close, closeIc);
            if (PopUI.Click(close, mp)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 36), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mp) && !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally
        {
            GUI.color = savedColor;
            GUI.depth = savedDepth;
            if (stacking.Item2) Event.current.mousePosition = originalMouse;
        }
    }
}
}
