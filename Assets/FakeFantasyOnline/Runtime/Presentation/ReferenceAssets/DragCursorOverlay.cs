using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 tokens 0x06000270..0x06000272. This component owns the held cursor
// independently of inventory visibility; it consumes the shared native held state.
public sealed class DragCursorOverlay : MonoBehaviour
{
    public static DragCursorOverlay I;

    public static void Ensure()
    {
        if (I != null) return;
        var go = new GameObject("DragCursorOverlay");
        I = go.AddComponent<DragCursorOverlay>();
        DontDestroyOnLoad(go);
    }

    private void OnGUI()
    {
        GUI.depth = -1000;
        if (!InventoryDlg.HasHeldAny) return;
        float s = UIScale.Factor;
        Vector2 mp = Event.current.mousePosition;
        Texture2D icon = InventoryDlg.HeldIcon;
        if (icon != null)
        {
            ItemIconUI.Draw(new Rect(mp.x - s * 18f, mp.y - s * 18f, s * 36f, s * 36f), icon, s);
            return;
        }
        if (!BitmapFont.Ready || string.IsNullOrEmpty(InventoryDlg.HeldName)) return;
        BitmapFont.DrawOutline(mp.x + s * -10f, mp.y + s * -6f, InventoryDlg.HeldName, Color.white, s);
    }
}
}
