using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class DuelDlg
{
    private Texture2D bg, close0, close1, rowSel, rowLine;
    // 三个按钮帧数组由 DuelDlg..ctor（06000287）分配。
    private readonly Texture2D[] redBtn, okBtn, noBtn;
    private Texture2D pUL, pUR, pDL, pDR, pUM, pDM, pLM, pRM, pMid;
    private bool triedAssets;
    private GUIStyle editSt;

    private void EnsureAssets()
    {
        if (triedAssets) return;
        triedAssets = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { triedAssets = false; return; }
        bg = loader.LoadGsa("res/uires/qqradio/qqlivedlg.gsa");
        close0 = loader.LoadGsaFrame("res/uires/common/miniclose.gsa", 0);
        close1 = loader.LoadGsaFrame("res/uires/common/miniclose.gsa", 1);
        rowSel = loader.LoadGsa("res/uires/arenadlg/detailitemsel.gsa");
        rowLine = loader.LoadGsa("res/uires/arenadlg/itembk.gsa");
        for (int i = 0; i < 4; i++)
        {
            redBtn[i] = loader.LoadGsaFrameA("res/uires/votedlg/buttonbk.gsa", i);
            okBtn[i] = loader.LoadGsaFrameA("res/uires/common/affirmbtn.gsa", i);
            noBtn[i] = loader.LoadGsaFrameA("res/uires/common/cancelbtn.gsa", i);
        }
        pUL = loader.LoadGsa("res/uires/patch/patchupleft2.gsa");
        pUR = loader.LoadGsa("res/uires/patch/patchupright2.gsa");
        pDL = loader.LoadGsa("res/uires/patch/patchdownleft2.gsa");
        pDR = loader.LoadGsa("res/uires/patch/patchdownright2.gsa");
        pUM = loader.LoadGsa("res/uires/patch/patchupmiddle2.gsa");
        pDM = loader.LoadGsa("res/uires/patch/patchdownmiddle2.gsa");
        pLM = loader.LoadGsa("res/uires/patch/patchleftmiddle2.gsa");
        pRM = loader.LoadGsa("res/uires/patch/patchrightmiddle2.gsa");
        pMid = loader.LoadGsa("res/uires/patch/patchmiddle2.gsa");
    }

    private void EnsureEditStyle(float scale)
    {
        if (editSt == null)
        {
            editSt = new GUIStyle(GUIStyle.none)
            {
                font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleLeft
            };
            editSt.normal.textColor = new Color(1f, .95f, .8f, 1f);
            editSt.focused.textColor = Color.white;
            editSt.padding = new RectOffset(4, 4, 0, 0);
        }
        editSt.fontSize = Mathf.RoundToInt(12f * scale);
    }

    private static Color C(byte r, byte g, byte b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
    private static void TX(float px, float py, string text, Color color, float font)
        => BitmapFont.DrawOutline(Mathf.Round(px), Mathf.Round(py), text, color, font);

    private void Box(Rect b, float scale, bool lit = false, float fillA = .55f)
    {
        // 官方成员是 DuelDlg.<Box>g__D|53_0（06000289）：Box 内的静态本地函数，
        // 贴图为空时以 (0.72, 0.61, 0.38, 0.8) 铺白底再恢复白色。
        static void D(Texture2D t, Rect rr)
        {
            if (t != null) GUI.DrawTexture(rr, t);
            else
            {
                GUI.color = new Color(.72f, .61f, .38f, .8f);
                GUI.DrawTexture(rr, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }
        float edge = 7f * scale;
        GUI.color = lit ? new Color(.36f, .27f, .12f, .95f) : new Color(0, 0, 0, fillA);
        GUI.DrawTexture(new Rect(b.x + edge, b.y + edge, b.width - 2 * edge, b.height - 2 * edge), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (pMid != null) GUI.color = lit ? new Color(1.3f, 1.1f, .7f, 1f) : Color.white;
        D(pUM, new Rect(b.x + edge, b.y, b.width - 2 * edge, edge));
        D(pDM, new Rect(b.x + edge, b.yMax - edge, b.width - 2 * edge, edge));
        D(pLM, new Rect(b.x, b.y + edge, edge, b.height - 2 * edge));
        D(pRM, new Rect(b.xMax - edge, b.y + edge, edge, b.height - 2 * edge));
        D(pUL, new Rect(b.x, b.y, edge, edge));
        D(pUR, new Rect(b.xMax - edge, b.y, edge, edge));
        D(pDL, new Rect(b.x, b.yMax - edge, edge, edge));
        D(pDR, new Rect(b.xMax - edge, b.yMax - edge, edge, edge));
        GUI.color = Color.white;
    }

    private bool SkinBtn(Rect rect, Texture2D[] frames, string text, bool enabled, float font, bool lit = false)
    {
        bool hover = enabled && rect.Contains(Event.current.mousePosition);
        int index = !enabled ? 3 : hover && Input.GetMouseButton(0) ? 2 : hover || lit ? 1 : 0;
        Texture2D texture = frames[index] ?? frames[0];
        if (texture != null) GUI.DrawTexture(rect, texture);
        else
        {
            GUI.color = enabled ? new Color(.45f, .12f, .08f, .95f) : new Color(.2f, .2f, .2f, .9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        if (!string.IsNullOrEmpty(text))
            TX(rect.x + (rect.width - BitmapFont.Measure(text, font)) * .5f,
                rect.y + (rect.height - 12f * font) * .5f + 1f, text,
                enabled ? C(255, 240, 200) : C(150, 140, 130), font);
        return enabled && GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private bool Tab(Rect rect, string text, bool on, float font, float scale)
    {
        Box(rect, scale, on, .35f);
        TX(rect.x + (rect.width - BitmapFont.Measure(text, font)) * .5f,
            rect.y + (rect.height - 12f * font) * .5f + 1f, text,
            on ? C(255, 226, 138) : C(190, 190, 200), font);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }
}
}
