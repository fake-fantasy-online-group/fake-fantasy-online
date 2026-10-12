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
    private ResLoader assetOwner;
    private object assetPackage;
    private GUIStyle titleStyle, descStyle;
    private readonly GUIContent titleContent = new GUIContent();
    private readonly GUIContent normalContent = new GUIContent();
    private readonly GUIContent hardContent = new GUIContent();

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
        I.titleContent.text = "【" + title + "】选择难度";
        I.normalContent.text = normalDesc;
        I.hardContent.text = hardDesc;
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
        ResLoader loader = ResLoader.I;
        object package = loader != null ? loader.Pkg : null;
        if (!ReferenceEquals(assetOwner, loader) || !ReferenceEquals(assetPackage, package))
        {
            assetOwner = loader;
            assetPackage = package;
            triedAssets = false;
            bg = null;
        }
        if (loader == null || package == null || triedAssets) return;
        triedAssets = true;
        try
        {
            bg = loader.LoadGsa("res/uires/common/msgboxbk.gsa");
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
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = NpcChatter.CnFont(), wordWrap = true
            };
            titleStyle.normal.textColor = Color.white;
            descStyle = new GUIStyle(titleStyle);
            descStyle.normal.textColor = new Color(0.78f, 0.85f, 1f, 1f);
        }
        titleStyle.fontSize = Mathf.RoundToInt(s * 13f);
        descStyle.fontSize = Mathf.RoundToInt(s * 11f);
        float w = BG_W * s;
        float pad = PAD * s;
        float innerW = w - pad * 2f;
        // Keep the existing layout for short text; grow only the text rows for wrapping.
        float titleH = Mathf.Max(20f * s, titleStyle.CalcHeight(titleContent, innerW));
        float normalH = Mathf.Max(pad, descStyle.CalcHeight(normalContent, innerW));
        float hardH = Mathf.Max(pad, descStyle.CalcHeight(hardContent, innerW));
        float h = Mathf.Max(BG_H * s, pad * 2f + titleH + normalH + hardH + 58f * s);
        float x = (screenWidth - w) * 0.5f;
        float y = (screenHeight - h) * 0.5f;
        if (bg != null)
        {
            // Preserve the frame caps when a long description makes the dialog taller.
            float cap = 44f * s;
            const float uvCap = 44f / 128f;
            GUI.DrawTextureWithTexCoords(new Rect(x, y, w, cap), bg, new Rect(0f, 1f - uvCap, 1f, uvCap));
            GUI.DrawTextureWithTexCoords(new Rect(x, y + cap, w, h - cap * 2f), bg,
                new Rect(0f, uvCap, 1f, 1f - uvCap * 2f));
            GUI.DrawTextureWithTexCoords(new Rect(x, y + h - cap, w, cap), bg, new Rect(0f, 0f, 1f, uvCap));
        }
        else ArtworkSkin.Panel(new Rect(x, y, w, h), s);
        float innerX = x + pad;
        GUI.Label(new Rect(innerX, y + pad, innerW, titleH), titleContent, titleStyle);
        float rowY = y + pad + titleH + 2f * s;
        GUI.Label(new Rect(innerX, rowY, innerW, normalH), normalContent, descStyle);
        rowY += normalH;
        GUI.Label(new Rect(innerX, rowY, innerW, hardH), hardContent, descStyle);
        rowY += hardH + 6f * s;
        float btnW = (innerW - 8f * s) * 0.5f;
        if (ArtworkSkin.Button(new Rect(innerX, rowY, btnW, BTN_H * s), "普通", s))
        {
            Action callback = onNormal;
            Close();
            if (callback != null) callback();
        }
        if (ArtworkSkin.Button(new Rect(innerX + btnW + 8f * s, rowY, btnW, BTN_H * s), "困难", s))
        {
            Action callback = onHard;
            Close();
            if (callback != null) callback();
        }
        rowY += 30f * s;
        if (ArtworkSkin.Button(new Rect(innerX + innerW * 0.25f, rowY, innerW * 0.5f, 20f * s), "再想想", s))
        {
            Close();
        }
    }
}
}
