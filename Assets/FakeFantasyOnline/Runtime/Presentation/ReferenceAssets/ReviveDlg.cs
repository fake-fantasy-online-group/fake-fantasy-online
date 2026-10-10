using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class ReviveDlg : MonoBehaviour
{
    public static ReviveDlg I;
    private bool visible;
    private bool tried;
    private byte flags;
    private Texture2D bg;
    private Texture2D bRevive;
    private Texture2D bItem;
    private Texture2D bSkill;
    private Texture2D bItemDis;
    private Texture2D bSkillDis;
    private const float NW = 192f;
    private const float NH = 169f;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    public static void Ensure()
    {
        if (I == null)
        {
            GameObject owner = new GameObject("ReviveDlg");
            I = owner.AddComponent<ReviveDlg>();
            DontDestroyOnLoad(owner);
        }
    }

    public static void Open(byte reviveFlags)
    {
        Ensure();
        I.flags = reviveFlags;
        I.visible = true;
    }

    public static void Hide()
    {
        if (I != null)
            I.visible = false;
    }

    public static bool Visible => I != null && I.visible;

    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        float px = Input.mousePosition.x;
        float py = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * .5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * .5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(new Vector2(px, py));
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
        bg = loader.LoadGsa("res/uires/revivedlg/revivedlgbk.gsa");
        bRevive = loader.LoadGsaFrameA("res/uires/revivedlg/revivebtn.gsa", 0);
        bItem = loader.LoadGsaFrameA("res/uires/revivedlg/itemrevivebtn.gsa", 0);
        bSkill = loader.LoadGsaFrameA("res/uires/revivedlg/skillrevivebtn.gsa", 0);
        bItemDis = loader.LoadGsaFrameA("res/uires/revivedlg/itemrevivebtn.gsa", 3);
        bSkillDis = loader.LoadGsaFrameA("res/uires/revivedlg/skillrevivebtn.gsa", 3);
    }

    private Rect L(float x, float y, float w, float h) =>
        new Rect(winX + x * s, winY + y * s, w * s, h * s);

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
        Vector2 mouse = Event.current.mousePosition;
        GUI.depth = 1;
        if (bg != null)
            GUI.DrawTexture(L(0f, 0f, NW, NH), bg);
        else
        {
            GUI.color = new Color(.1f, .09f, .13f, .98f);
            GUI.DrawTexture(L(0f, 0f, NW, NH), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        if (BtnImg(L(19f, 35f, 152f, 33f), bRevive, "重生角色", mouse, true))
        {
            if (NetClient.I != null) NetClient.I.ReviveForIndependentRuntime(0);
            visible = false;
        }
        bool item = (flags & 1) != 0;
        if (BtnImg(L(19f, 70f, 152f, 33f), item ? bItem : (bItemDis ?? bItem),
                "使用替身娃娃", mouse, item) && item)
        {
            if (NetClient.I != null) NetClient.I.ReviveForIndependentRuntime(1);
            visible = false;
        }
        bool skill = (flags & 2) != 0;
        if (BtnImg(L(19f, 105f, 152f, 33f), skill ? bSkill : (bSkillDis ?? bSkill),
                "使用回魂娃娃", mouse, skill) && skill)
        {
            if (NetClient.I != null) NetClient.I.ReviveForIndependentRuntime(2);
            visible = false;
        }
        WinDrag.Handle(this, L(0f, 0f, NW, 28f), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown && L(0f, 0f, NW, NH).Contains(mouse) &&
            !SysMsgBox.Visible)
            Event.current.Use();
    }

    private bool BtnImg(Rect rect, Texture2D texture, string label, Vector2 mouse, bool enabled)
    {
        bool hover = enabled && rect.Contains(mouse);
        if (texture != null)
        {
            float brightness = enabled ? (hover ? 1.3f : 1f) : .55f;
            GUI.color = new Color(brightness, brightness, brightness, enabled ? 1f : .85f);
            GUI.DrawTexture(rect, texture);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = !enabled ? new Color(.22f, .2f, .14f, .7f)
                : hover ? new Color(.55f, .42f, .18f, .95f) : new Color(.3f, .26f, .16f, .9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready)
                BitmapFont.CenterOutline(rect, label,
                    new Color(.6f, enabled ? .8f : .6f, enabled ? 1f : .6f, 1f), s);
        }
        if (hover && !SysMsgBox.Visible && Event.current.type == EventType.MouseDown &&
            Event.current.button == 0)
        {
            Event.current.Use();
            return true;
        }
        return false;
    }
}
}
