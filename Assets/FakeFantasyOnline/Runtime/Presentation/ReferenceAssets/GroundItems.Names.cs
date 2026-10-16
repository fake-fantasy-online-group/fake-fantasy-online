using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class GroundItems
{
    internal static Color LabelColor(int quality, int uniqueId, bool protectedItem) =>
        protectedItem ? Color.red : (Color)UITip.QualityColor(uniqueId > 0 ? 5 : quality);

    private void OnGUI()
    {
        if (!BitmapFont.Ready || items.Count == 0) return;
        GUI.depth = 100;
        Camera camera = Camera.main;
        if (camera == null) return;
        float scale = UIScale.Factor;
        foreach (Item item in items.Values)
        {
            if (item.go == null) continue;
            bool protectedItem = !item.owned && !(Time.time >= item.lockEnd);
            float top = item.sr != null ? item.sr.bounds.max.y : item.go.transform.position.y + 0.3f;
            Vector3 screen = camera.WorldToScreenPoint(new Vector3(item.go.transform.position.x, top, 0f));
            if (screen.z < 0f) continue;
            float labelY = Screen.height - screen.y - 13f * scale;
            Color color = LabelColor(item.quality, item.uniqueId, protectedItem);
            BitmapFont.CenterOutline(new Rect(screen.x - 60f * scale,
                labelY, 120f * scale, 12f * scale), item.name, color, scale);
        }
    }
}
}
