using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    private GUIStyle nameSt;
    private GUIStyle lvSt;

    // Official 1.5.8 0600042f. The first style alone is the initialization guard;
    // neither a scale change nor a partially initialized level style resets it.
    private void EnsureStyles()
    {
        if (nameSt != null) return;

        nameSt = new GUIStyle
        {
            font = font,
            fontSize = Mathf.RoundToInt(12f * s),
            alignment = TextAnchor.MiddleCenter
        };
        nameSt.normal.textColor = new Color(249f / 255f, 203f / 255f, 43f / 255f, 1f);

        lvSt = new GUIStyle
        {
            font = font,
            fontSize = Mathf.RoundToInt(12f * s),
            alignment = TextAnchor.MiddleCenter
        };
        lvSt.normal.textColor = new Color(249f / 255f, 203f / 255f, 43f / 255f, 1f);
    }

    // Official 1.5.8 06000423. Preserve the draw order and ordinary-path color
    // restoration; the native method does not introduce a finally policy.
    private void OutLabel(Rect r, string txt, GUIStyle st, float ow = 1f)
    {
        if (string.IsNullOrEmpty(txt)) return;

        Color main = st.normal.textColor;
        float o = Mathf.Max(1f, ow * s);
        st.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x - o, r.y, r.width, r.height), txt, st);
        GUI.Label(new Rect(r.x + o, r.y, r.width, r.height), txt, st);
        GUI.Label(new Rect(r.x, r.y - o, r.width, r.height), txt, st);
        GUI.Label(new Rect(r.x, r.y + o, r.width, r.height), txt, st);
        GUI.Label(new Rect(r.x - o, r.y - o, r.width, r.height), txt, st);
        GUI.Label(new Rect(r.x + o, r.y - o, r.width, r.height), txt, st);
        GUI.Label(new Rect(r.x - o, r.y + o, r.width, r.height), txt, st);
        GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), txt, st);
        st.normal.textColor = main;
        GUI.Label(r, txt, st);
    }

    // The status carrier supplies only the existing native owner, frame scale,
    // rectangles and snapshot text. The native owner retains both GUIStyles.
    internal void EnsureStylesForIndependentRuntime() => EnsureStyles();

    internal void DrawNameFallbackForIndependentRuntime(Rect bounds, string text) =>
        OutLabel(bounds, text, nameSt);

    internal void DrawLevelFallbackForIndependentRuntime(Rect bounds, string text) =>
        OutLabel(bounds, text, lvSt);
}

}
