using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed class HairSalonDlg : MonoBehaviour
{
    private static HairSalonDlg I;
    private bool visible, tried;
    private int mode, tentSel, scrollOff;
    private PlayerAvatar doll;
    private int dollHair = -1, dollHead = -1;
    private Texture2D bkTex, closeIc, okBtn, cancelBtn, goldIc, silverIc, copperIc;
    private Texture2D[] thumb = new Texture2D[9];
    private int thumbKey = -1;
    private float winX = float.NaN, winY, s;
    private int _wdGen = -1;
    private const float NW = 345f, NH = 476f, ROW_H = 40f;
    private const int VIS_ROWS = 4, StyleMoney = 30000;
    private static readonly Rect R_PREVIEW = new Rect(127, 111, 88, 107);
    private static readonly Rect R_LIST = new Rect(60, 272, 202, 160);
    private static readonly Rect R_SCROLL = new Rect(274, 286, 15, 128);
    private static readonly Rect R_OK = new Rect(67, 441, 90, 22);
    private static readonly Rect R_CANCEL = new Rect(205, 441, 70, 22);
    private static readonly Rect R_CLOSE = new Rect(276, 34, 20, 19);
    private static readonly Rect R_CAPTION = new Rect(95, 38, 150, 12);
    private static readonly Color GOLD = new Color(1f, .92f, .66f, 1f);
    private static readonly int[] DyeItem = { 3502, 3513, 3501, 3506, 3503, 3504, 3505, 3514 };
    private static readonly int[] DyeMoney = { 40000, 20000, 50000, 70000, 50000, 70000, 40000, 20000 };

    private static void Ensure()
    {
        if (I != null) return;
        var go = new GameObject("HairSalonDlg");
        I = go.AddComponent<HairSalonDlg>();
        DontDestroyOnLoad(go);
    }
    public static bool Active => I != null && I.visible;
    public static void CloseNow() { if (I != null) I.visible = false; }
    public static void Open(int mode)
    {
        Ensure();
        I.mode = mode;
        I.tentSel = Mathf.Clamp(mode == 0 ? CharData.hair : CharData.head, 1, 8);
        I.scrollOff = 0;
        I.thumbKey = -1;
        I.visible = true;
    }
    private void Load()
    {
        tried = true;
        var loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        bkTex = loader.LoadUI("ui/figuredlg/figuredlg.png");
        if (bkTex == null) bkTex = loader.LoadGsa("res/uires/figuredlg/figuredlg.gsa");
        okBtn = loader.LoadGsa("res/uires/figuredlg/figurebtn.gsa");
        cancelBtn = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
        closeIc = loader.LoadGsa("res/uires/common/miniclose.gsa");
        goldIc = loader.LoadGsa("res/uires/common/gold.gsa");
        silverIc = loader.LoadGsa("res/uires/common/silver.gsa");
        copperIc = loader.LoadGsa("res/uires/common/copper.gsa");
    }
    private static string HeadPath(int style, int color)
    {
        if (CharData.gender == 1)
        {
            if (style == 7) style = 8;
            else if (style == 8) style = 7;
        }
        return string.Format("res/head/DThed{0:D2}{1}_{2}.gsa", style, color, CharData.gender == 1 ? "01" : "51");
    }
    private void EnsureThumbs()
    {
        int other = Mathf.Clamp(mode == 0 ? CharData.head : CharData.hair, 1, 8);
        int key = mode * 100 + other;
        if (thumbKey == key) return;
        thumbKey = key;
        var loader = ResLoader.I;
        if (loader == null) { thumbKey = -1; return; }
        for (int i = 1; i <= 8; i++)
            thumb[i] = loader.LoadGsa(mode == 0 ? HeadPath(other, i) : HeadPath(i, other));
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private Rect L(Rect r) => L(r.x, r.y, r.width, r.height);
    private int PreviewHead => mode == 1 ? tentSel : Mathf.Clamp(CharData.head, 1, 8);
    private int PreviewHair => mode == 0 ? tentSel : Mathf.Clamp(CharData.hair, 1, 8);
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * .5f;
            winY = (Screen.height - NH * s) * .5f;
        }
        var z = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
        Vector2 originalPointer = Event.current.mousePosition;
        if (z.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            Vector2 mp = Event.current.mousePosition;
            GUI.depth = z.Item1;
            EnsureThumbs();
            if (bkTex != null) GUI.DrawTexture(L(0, 0, NW, NH), bkTex);
            else
            {
                GUI.color = new Color(.09f, .08f, .07f, .98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (BitmapFont.Ready) BitmapFont.CenterOutline(L(R_CAPTION), mode == 0 ? "改变发色" : "改变发型", GOLD, s);
            if (Btn(L(R_CLOSE), closeIc, mp)) visible = false;
            else
            {
                if (dollHair != PreviewHair || dollHead != PreviewHead || (doll != null && !doll.ok)) RebuildPreview();
                DrawPreview();
                DrawList(mp);
                if (Btn(L(R_OK), okBtn, mp)) Confirm();
                else if (Btn(L(R_CANCEL), cancelBtn, mp)) visible = false;
            }
        }
        finally
        {
            if (z.Item2) Event.current.mousePosition = originalPointer;
        }
    }
    private void DrawList(Vector2 mp)
    {
        Rect list = L(R_LIST);
        if (list.Contains(mp) && Event.current.type == EventType.ScrollWheel)
        {
            scrollOff = Mathf.Clamp(scrollOff + (Event.current.delta.y < 0f ? -1 : 1), 0, 4);
            Event.current.Use();
        }
        GUI.BeginGroup(list);
        mp -= list.position;
        for (int row = 0; row < VIS_ROWS; row++)
        {
            int index = row + scrollOff;
            if (index >= 8) break;
            int id = index + 1;
            float y = row * ROW_H * s;
            Rect rr = new Rect(0, y, R_LIST.width * s, 38f * s);
            bool hover = rr.Contains(mp);
            if (id == tentSel || hover)
            {
                GUI.color = id == tentSel ? new Color(.55f, .45f, .3f, .7f) : new Color(.3f, .27f, .22f, .55f);
                GUI.DrawTexture(rr, Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            Texture2D head = thumb[id];
            if (head != null)
            {
                float headHeight = 34f * s;
                float headWidth = headHeight * head.width / Mathf.Max(1, head.height);
                GUI.DrawTexture(new Rect(4f * s, y + 3f * s, headWidth, headHeight), head, ScaleMode.ScaleToFit);
            }
            if (mode == 0)
            {
                Texture2D item = NetClient.LoadItemIcon(DyeItem[index]);
                float itemY = y + 8f * s;
                if (item != null) GUI.DrawTexture(new Rect(42f * s, itemY, 22f * s, 22f * s), item, ScaleMode.ScaleToFit);
                if (BitmapFont.Ready) BitmapFont.DrawOutline(64f * s, y + 15f * s, "×1", Color.white, s);
            }
            DrawCoins(R_LIST.width * s - 104f * s, y + 14f * s, mode == 0 ? DyeMoney[index] : StyleMoney);
            if (hover && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                tentSel = id;
                Event.current.Use();
            }
        }
        GUI.EndGroup();
        NativeScroll.BarRows(this, L(R_SCROLL), VIS_ROWS, 8, ref scrollOff);
    }
    private void DrawCoins(float x, float y, int copper)
    {
        x = DrawCoin(x, y, goldIc, copper / 1000000);
        x = DrawCoin(x, y, silverIc, copper / 1000 % 1000);
        DrawCoin(x, y, copperIc, copper % 1000);
    }
    private float DrawCoin(float x, float y, Texture2D ic, long n)
    {
        if (ic != null) { GUI.DrawTexture(new Rect(x, y, 11f * s, 11f * s), ic); x += 12f * s; }
        if (BitmapFont.Ready) BitmapFont.DrawOutline(x, y + s, n.ToString(), Color.white, s);
        return x + (n < 10 ? 10f : n < 100 ? 16f : 22f) * s + 4f * s;
    }
    private void Confirm()
    {
        if (NetClient.I != null) NetClient.I.ChangeHairForIndependentRuntime((byte)mode, (byte)tentSel);
        visible = false;
    }
    private void RebuildPreview()
    {
        PlayerAvatar old = doll;
        doll = PlayerAvatar.Build(CharData.gender, PreviewHead, PreviewHair, new PlayerAvatar.Appear(), 100f);
        if (old != null && old != doll) old.Dispose();
        dollHead = PreviewHead;
        dollHair = PreviewHair;
    }
    private void DrawPreview()
    {
        PlayerAvatar preview = doll;
        if (preview == null || !preview.ok || preview.Still(4) == null) return;
        Sprite sprite = preview.Still(4);
        Rect source = sprite.textureRect;
        Rect box = L(R_PREVIEW);
        float factor = Mathf.Min(1.6f, Mathf.Min((box.width - 6f * s) / (source.width * s), (box.height - 6f * s) / (source.height * s)));
        float width = source.width * s * factor, height = source.height * s * factor;
        GUI.DrawTextureWithTexCoords(
            new Rect(box.center.x - width * .5f, box.yMax - 4f * s - height, width, height),
            sprite.texture,
            new Rect(source.x / sprite.texture.width, source.y / sprite.texture.height,
                source.width / sprite.texture.width, source.height / sprite.texture.height));
    }
    private bool Btn(Rect r, Texture2D t, Vector2 mp)
    {
        bool hover = r.Contains(mp);
        if (t != null)
        {
            GUI.color = hover ? new Color(1.25f, 1.25f, 1.25f, 1f) : Color.white;
            GUI.DrawTexture(r, t);
            GUI.color = Color.white;
        }
        if (!hover || Event.current.type != EventType.MouseDown || Event.current.button != 0) return false;
        Event.current.Use();
        return true;
    }
}
}
