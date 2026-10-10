using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    private static Texture2D _focusBk;
    public static readonly Color ItemFocusBkColor = new Color(0.92f, 0.12f, 0.10f, 0.55f);
    public static Texture2D FocusBk
    {
        get
        {
            if (_focusBk == null && ResLoader.I != null)
                _focusBk = ResLoader.I.LoadGsa("res/effect/focusbk0.gsa");
            return _focusBk;
        }
    }
}
}
