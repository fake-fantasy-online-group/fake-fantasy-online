using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class UIManager
{
    internal bool MobileWindowOpen => stack.Count>0;
    internal bool MobileWindowContains(Vector2 screenPoint)
    {
        if (cam == null) return false;
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0f));
        foreach (var win in stack)
        {
            if (win.root == null || win.chrome == null || win.chrome.sprite == null) continue;
            Vector3 local = win.chrome.transform.InverseTransformPoint(world);
            Bounds bounds = win.chrome.sprite.bounds;
            if (local.x >= bounds.min.x && local.x <= bounds.max.x &&
                local.y >= bounds.min.y && local.y <= bounds.max.y) return true;
        }
        return false;
    }
}
}
