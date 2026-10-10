using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg : MonoBehaviour
{
    public static void CloseStallIf(int ownerId)
    {
        if (I != null && I.visible && I.stallMode && I.stallOwnerId == ownerId)
            I.visible = false;
    }
}
}
