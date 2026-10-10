using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SelectPopupDlg : MonoBehaviour
{
    public static SelectPopupDlg I;

    private bool visible;

    private const float NW = 106f;
    private const float NH = 122f;

    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    public static bool Visible => I != null && I.visible;

    public static void Toggle()
    {
        if (I == null)
        {
            GameObject gameObject = new GameObject("SelectPopupDlg");
            I = gameObject.AddComponent<SelectPopupDlg>();
            Object.DontDestroyOnLoad(gameObject);
        }
        I.visible = !I.visible;
    }

    public static bool MouseOver()
    {
        if (I == null || !I.visible)
        {
            return false;
        }
        float s = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - s * NW) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - s * NH) * 0.5f : I.winY;
        return mx >= x && mx < x + s * NW && my >= y && my < y + s * NH;
    }

    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - s * NW) * 0.5f;
            winY = (Screen.height - s * NH) * 0.5f;
        }
        Vector2 mousePos = Event.current.mousePosition;
        GUI.depth = 2;
        CraftUI.Panel(L(0f, 0f, NW, NH));
        Rect inset = L(6f, 6f, 94f, 110f);
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(inset, Texture2D.whiteTexture);
        GUI.color = Color.white;
        WinDrag.Handle(this, L(0f, 0f, NW, 20f), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown
            && L(0f, 0f, NW, NH).Contains(mousePos)
            && !SysMsgBox.Visible)
        {
            Event.current.Use();
        }
    }
}

}
