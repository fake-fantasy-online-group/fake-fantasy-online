using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class StallConfigDlg
{
    private static void Ensure()
    {
        if (I != null) return;
        var go = new GameObject("StallConfigDlg");
        I = go.AddComponent<StallConfigDlg>();
        DontDestroyOnLoad(go);
    }

    public static void Open(byte type)
    {
        Ensure();
        I.shoptype = type;
        I.shopName = "";
        I.staged.Clear();
        I.RestoreTemplate(type);
        I.visible = true;
    }

    public static bool MouseOver()
    {
        if (!Active) return false;
        float scale = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(new Vector2(mx, my));
    }

    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        bg = loader.LoadGsa("res/uires/grocerydlg/groceryconfig.gsa");
        beginBtn = loader.LoadGsa("res/uires/grocerydlg/beginshop.gsa");
        cancelBtn = loader.LoadGsa("res/uires/common/giveupbtn.gsa");
        closeIc = loader.LoadGsa("res/uires/common/miniclose.gsa");
        helpIc = loader.LoadGsa("res/uires/gameui/helpicon.gsa");
        rowBk = loader.LoadGsa("res/uires/tradedlg/tradeitemr.gsa");
        slideBtn = loader.LoadGsa("res/uires/common/slidebtn.gsa");
    }

    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);

    private bool Btn(Rect r, Texture2D texture, Vector2 mp)
    {
        bool hover = r.Contains(mp);
        if (texture != null)
        {
            GUI.color = hover ? new Color(1.25f, 1.25f, 1.25f, 1f) : Color.white;
            GUI.DrawTexture(r, texture);
            GUI.color = Color.white;
        }
        if (hover && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        { Event.current.Use(); return true; }
        return false;
    }
}
}
