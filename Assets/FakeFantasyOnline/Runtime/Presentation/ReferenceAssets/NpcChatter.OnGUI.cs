using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class NpcChatter
{
    // Official 1.5.8 token 0600091e.
    private void OnGUI()
    {
        GUI.depth = 100;
        if (cam == null || font == null) return;

        Vector3 screen = cam.WorldToScreenPoint(headAnchor);
        if (screen.z <= 0f) return;
        if (screen.x < 0f) return;
        if (screen.x > Screen.width) return;
        if (screen.y < 0f) return;
        if (screen.y > Screen.height) return;

        float screenX = screen.x;
        float anchorY = Screen.height - screen.y;
        if (!string.IsNullOrEmpty(npcName) && BitmapFont.Ready)
        {
            int scale = Mathf.Max(1, Mathf.RoundToInt(UIScale.Factor));
            if (_bakedName == null || _bakedNameFor != npcName || _bakedNameScale != scale)
            {
                if (_bakedName != null) Destroy(_bakedName);
                _bakedName = BitmapFont.BakeNameTag(
                    npcName, string.Empty, Color.green, Color.green,
                    scale, null, null, null, null, null);
                _bakedNameFor = npcName;
                _bakedNameScale = scale;
            }
            if (_bakedName != null)
            {
                anchorY = Mathf.Round(anchorY - _bakedName.height);
                GUI.DrawTexture(
                    new Rect(Mathf.Round(screenX - _bakedName.width * 0.5f), anchorY,
                        _bakedName.width, _bakedName.height),
                    _bakedName);
            }
        }

        if (!string.IsNullOrEmpty(npcName))
        {
            var symbol = TaskClient.HeadSymbol(npcName);
            if (symbol.Item1 >= 0)
            {
                Texture2D texture = MagicSymTex(SymMagicId(symbol.Item1, symbol.Item2));
                if (texture != null)
                {
                    float factor = UIScale.Factor;
                    float height = 34f * factor;
                    float width = height * texture.width / Mathf.Max(1, texture.height);
                    Rect rect = new Rect(
                        screenX - width * 0.5f,
                        anchorY - height - 4f * factor,
                        width, height);
                    float angle = Mathf.Sin(Time.time * 3f) * 14f;
                    Matrix4x4 previous = GUI.matrix;
                    GUIUtility.RotateAroundPivot(
                        angle, new Vector2(rect.x + width * 0.5f, rect.y + height));
                    GUI.DrawTexture(rect, texture);
                    GUI.matrix = previous;
                }
            }
        }

        if (bubble == null) return;
        if (Time.time > bubbleUntil) return;
        DrawBubble(screenX, anchorY, bubble, bubbleTheme);
    }
}

}
