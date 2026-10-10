using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class NewbieTipBox
{
    private Texture2D pUL, pUR, pDL, pDR, pUM, pDM, pLM, pRM, dark;
    private GUIStyle txt;
    private bool loaded;

    private static void Ensure()
    {
        if (I != null) return;
        var go = new GameObject("NewbieTipBox");
        I = go.AddComponent<NewbieTipBox>();
        DontDestroyOnLoad(go);
    }

    public static void PushText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Ensure();
        I.active.Add(new Tip { text = Strip(text), start = Time.time });
        while (I.active.Count > MAXN) I.active.RemoveAt(0);
    }

    private void Load()
    {
        loaded = true;
        ResLoader loader = ResLoader.I;
        if (loader != null)
        {
            pUL = loader.LoadGsa("res/uires/patch/patchupleft.gsa");
            pUR = loader.LoadGsa("res/uires/patch/patchupright.gsa");
            pDL = loader.LoadGsa("res/uires/patch/patchdownleft.gsa");
            pDR = loader.LoadGsa("res/uires/patch/patchdownright.gsa");
            pUM = loader.LoadGsa("res/uires/patch/patchupmiddle.gsa");
            pDM = loader.LoadGsa("res/uires/patch/patchdownmiddle.gsa");
            pLM = loader.LoadGsa("res/uires/patch/patchleftmiddle.gsa");
            pRM = loader.LoadGsa("res/uires/patch/patchrightmiddle.gsa");
        }
        dark = new Texture2D(1, 1);
        dark.SetPixel(0, 0, new Color(26f / 255f, 27f / 255f, 22f / 255f, 196f / 255f));
        dark.Apply();
        txt = new GUIStyle { font = NpcChatter.CnFont(), wordWrap = true, richText = false,
            alignment = TextAnchor.MiddleCenter };
    }

    private void OnGUI()
    {
        if (active.Count == 0) return;
        if (!loaded) Load();
        float s = UIScale.Factor;
        txt.fontSize = Mathf.RoundToInt(12f * s);
        float width = BOX_W * s, padding = 6f * s;
        float x = Screen.width - 382f * s;
        float y = Screen.height - 62f * s - 28f * s;
        Event current = Event.current;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Tip tip = active[i];
            float elapsed = Time.time - tip.start;
            if (elapsed > SHOW_SEC)
            {
                active.RemoveAt(i);
                continue;
            }
            float alpha = Mathf.Min(1f, elapsed / FADE);
            if (SHOW_SEC - elapsed < FADE) alpha = Mathf.Min(alpha, (SHOW_SEC - elapsed) / FADE);
            var content = new GUIContent(tip.text);
            float height = Mathf.Max(48f * s, txt.CalcHeight(content, width - padding * 2f) + padding * 2f);
            y -= 4f * s + height;
            var rect = new Rect(x, y, width, height);
            Color previous = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            DrawFrame(rect, s);
            txt.normal.textColor = new Color(1, 1, 1, alpha);
            GUI.Label(new Rect(x + padding, y + padding, width - padding * 2f, height - padding * 2f), content, txt);
            GUI.color = previous;
            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
            {
                active.RemoveAt(i);
                current.Use();
            }
        }
    }

    private void DrawFrame(Rect r, float s)
    {
        GUI.DrawTexture(new Rect(r.x + 2f * s, r.y + 2f * s, r.width - 4f * s, r.height - 4f * s), dark);
        float corner = 11f * s;
        if (pUM != null) GUI.DrawTexture(new Rect(r.x + corner, r.y, r.width - corner * 2f, GsaDecoder.H(pUM) * s), pUM);
        if (pDM != null) GUI.DrawTexture(new Rect(r.x + corner, r.yMax - GsaDecoder.H(pDM) * s, r.width - corner * 2f, GsaDecoder.H(pDM) * s), pDM);
        if (pLM != null) GUI.DrawTexture(new Rect(r.x, r.y + corner, GsaDecoder.W(pLM) * s, r.height - corner * 2f), pLM);
        if (pRM != null) GUI.DrawTexture(new Rect(r.xMax - GsaDecoder.W(pRM) * s, r.y + corner, GsaDecoder.W(pRM) * s, r.height - corner * 2f), pRM);
        if (pUL != null) GUI.DrawTexture(new Rect(r.x, r.y, corner, corner), pUL);
        if (pUR != null) GUI.DrawTexture(new Rect(r.xMax - corner, r.y, corner, corner), pUR);
        if (pDL != null) GUI.DrawTexture(new Rect(r.x, r.yMax - corner, corner, corner), pDL);
        if (pDR != null) GUI.DrawTexture(new Rect(r.xMax - corner, r.yMax - corner, corner, corner), pDR);
    }
}

}
