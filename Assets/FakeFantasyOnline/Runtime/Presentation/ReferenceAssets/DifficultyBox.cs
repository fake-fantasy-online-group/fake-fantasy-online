using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 DifficultyBox 0x06000269-0x0600026f.
public partial class DifficultyBox : MonoBehaviour
{
    private const float BG_W = 346f;
    private const float BG_H = 150f;
    private const float BTN_H = 26f;
    private const float PAD = 18f;

    public static DifficultyBox I;
    private bool shown;
    private string title = "";
    private string normalDesc = "";
    private string hardDesc = "";
    private Action onNormal;
    private Action onHard;
    private Texture2D bg;
    private bool triedAssets;

    public static bool Visible
    {
        get { return I != null && I.shown; }
    }

    // Official MouseOver compiles byte-identical to get_Visible: it performs no hit test at all.
    public static bool MouseOver()
    {
        return Visible;
    }

    public static void Show(string title, string normalDesc, string hardDesc, Action onNormal, Action onHard)
    {
        if (I == null)
        {
            GameObject owner = new GameObject("DifficultyBox");
            I = owner.AddComponent<DifficultyBox>();
        }
        I.title = title;
        I.normalDesc = normalDesc;
        I.hardDesc = hardDesc;
        I.onNormal = onNormal;
        I.onHard = onHard;
        I.shown = true;
    }

    public static void Close()
    {
        if (I == null) return;
        I.shown = false;
        I.onNormal = null;
        I.onHard = null;
    }

    private void EnsureAssets()
    {
        if (triedAssets) return;
        triedAssets = true;
        try
        {
            bg = ResLoader.I?.LoadGsa("res/uires/common/msgboxbk.gsa");
        }
        catch
        {
            bg = null;
        }
    }

    private void OnGUI()
    {
        if (!shown) return;
        EnsureAssets();
        float s = UIScale.Factor;
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        Event current = Event.current;
        if ((current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape) ||
            (current.type == EventType.MouseDown && current.button == 1))
        {
            Close();
            current.Use();
            return;
        }
        float w = BG_W * s;
        float h = BG_H * s;
        float x = (screenWidth - w) * 0.5f;
        float y = (screenHeight - h) * 0.5f;
        if (bg != null)
        {
            GUI.DrawTexture(new Rect(x, y, w, h), bg);
        }
        else
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = previous;
        }
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.RoundToInt(s * 13f);
        titleStyle.wordWrap = true;
        titleStyle.normal.textColor = Color.white;
        GUIStyle descStyle = new GUIStyle(titleStyle);
        descStyle.fontSize = Mathf.RoundToInt(s * 11f);
        descStyle.normal.textColor = new Color(0.78f, 0.85f, 1f, 1f);
        float pad = PAD * s;
        float innerX = x + pad;
        float innerW = w - pad * 2f;
        GUI.Label(new Rect(innerX, y + pad, innerW, 20f * s), "【" + title + "】选择难度", titleStyle);
        float rowY = y + pad + 22f * s;
        GUI.Label(new Rect(innerX, rowY, innerW, pad), normalDesc, descStyle);
        rowY += pad;
        GUI.Label(new Rect(innerX, rowY, innerW, pad), hardDesc, descStyle);
        rowY += 24f * s;
        float btnW = (innerW - 8f * s) * 0.5f;
        if (GUI.Button(new Rect(innerX, rowY, btnW, BTN_H * s), "普通"))
        {
            Action callback = onNormal;
            Close();
            if (callback != null) callback();
        }
        if (GUI.Button(new Rect(innerX + btnW + 8f * s, rowY, btnW, BTN_H * s), "困难"))
        {
            Action callback = onHard;
            Close();
            if (callback != null) callback();
        }
        rowY += 30f * s;
        if (GUI.Button(new Rect(innerX + innerW * 0.25f, rowY, innerW * 0.5f, 20f * s), "再想想"))
        {
            Close();
        }
    }
}
}
