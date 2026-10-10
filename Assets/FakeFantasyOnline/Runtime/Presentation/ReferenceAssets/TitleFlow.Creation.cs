using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    private Texture2D previewfrm, namelbl, hairbtnTex, genderLbl, typeLbl, figureLbl, hairLbl;
    private Texture2D male, female, maleSel, femaleSel, btnComplete;
    private GUIStyle fieldStyle, hintSt;
    private Action<string, int, int, int, int> createCharForIndependentRuntime;
    private Action returnToSelectionForIndependentRuntime;
    private Func<string> getMessageForIndependentRuntime;

    // Bind only existing page capabilities. Resources remain live on this same owner.
    internal void BindCreationForIndependentRuntime(
        Action<string, int, int, int, int> createChar, Action returnToSelection, Func<string> getMessage)
    {
        createCharForIndependentRuntime = createChar ?? throw new ArgumentNullException(nameof(createChar));
        returnToSelectionForIndependentRuntime = returnToSelection ?? throw new ArgumentNullException(nameof(returnToSelection));
        getMessageForIndependentRuntime = getMessage ?? throw new ArgumentNullException(nameof(getMessage));
    }

    internal void ClearCreationNameForIndependentRuntime() => charName = string.Empty;

    // Official 1.5.8 06000ef9. Resource nulls only affect their own drawing. There is no native
    // GUI.enabled/busy scope here; the existing serialized request boundary stays downstream.
    internal void DrawCharCreate()
    {
        int race = Mathf.Clamp(CharData.race, 0, 4);
        int gender = CharData.gender > 0 ? 1 : 0;
        DrawSprite(classpic[race, gender], new Rect(0f, 50f, 324f, 530f));
        if (genderLbl) TexScale.Draw(301f, 94f, genderLbl);
        if (typeLbl) TexScale.Draw(301f, 158f, typeLbl);
        if (figureLbl) TexScale.Draw(301f, 245f, figureLbl);
        if (hairLbl) TexScale.Draw(290f, 270f, hairLbl);
        if (previewfrm) TexScale.Draw(542f, 20f, previewfrm);
        DrawSprite(nudeAv[gender, Mathf.Clamp(CharData.head, 1, 8), CharData.hair >= 2 ? 2 : 1],
            new Rect(614f, 89f, 100f, 100f));
        if (namelbl) GUI.DrawTexture(new Rect(264f, 28f, 203f, 63f), namelbl);
        charName = GUI.TextField(new Rect(306f, 68f, 135f, 22f), charName, 30, fieldStyle);

        if (RadioImg(new Rect(285f, 115f, 51f, 31f), male, maleSel, CharData.gender == 1))
            CharData.gender = 1;
        if (RadioImg(new Rect(354f, 115f, 51f, 31f), female, femaleSel, CharData.gender == 0))
            CharData.gender = 0;
        int[,] positions = { { 275, 179 }, { 370, 179 }, { 460, 179 }, { 275, 209 }, { 370, 209 } };
        for (int i = 0; i < 5; i++)
        {
            if (RadioImg(new Rect(positions[i, 0], positions[i, 1], 91f, 29f),
                    typebtn[i], typebtnSel[i], race == i))
                CharData.race = i;
        }

        // Gender remains the entry snapshot; head and hair are read again at this draw site.
        int previewHead = Mathf.Clamp(CharData.head, 1, 8);
        int previewHair = CharData.hair >= 2 ? 2 : 1;
        Texture2D hairPreview = hairHead[gender, previewHead, previewHair];
        if (hairPreview)
            GUI.DrawTexture(new Rect(299f, 274f, 50f, 50f), hairPreview, ScaleMode.ScaleToFit);
        float hairWidth = hairbtnTex ? GsaDecoder.W(hairbtnTex) : 12f;
        float hairHeight = hairbtnTex ? GsaDecoder.H(hairbtnTex) : 18f;
        if (hairbtnTex) GUI.DrawTexture(new Rect(360f, 291f, hairWidth, hairHeight), hairbtnTex);
        if (GUI.Button(new Rect(348f, 281f, hairWidth + 24f, hairHeight + 20f),
                GUIContent.none, GUIStyle.none))
            CharData.head = CharData.head >= 8 ? 1 : CharData.head + 1;

        // Awake/ec6 now publish into the original one-based color slots.
        for (int hair = 1; hair <= 2; hair++)
        {
            if (RadioImg(new Rect(390f + (hair - 1) * 30f, 298f, 27f, 28f),
                    colorbtn[hair], colorbtnSel[hair], CharData.hair == hair))
                CharData.hair = hair;
        }

        if (proppanel) TexScale.Draw(282f, 343f, proppanel);
        int[] values = INIT[race];
        GUI.Label(new Rect(293f, 350f, 458f, 36f), CLASS[race] + " —— " + DESC[race], numStyle);
        LabelValue(342f, 393f, values[7]);
        LabelValue(532f, 393f, values[8]);
        LabelValue(702f, 393f, values[9]);
        LabelValue(342f, 409f, values[0]);
        LabelValue(532f, 409f, values[2]);
        LabelValue(702f, 409f, values[4]);
        LabelValue(342f, 425f, values[3]);
        LabelValue(532f, 425f, values[1]);
        LabelValue(702f, 425f, values[6]);

        if (ImgBtn(644f, 470f, btnComplete))
        {
            CharData.name = charName.Trim().Length == 0 ? "无名侠客" : charName.Trim();
            CharData.race = race;
            setMessage("创建中...");
            createCharForIndependentRuntime(CharData.name, CharData.race,
                CharData.gender, CharData.head, CharData.hair);
        }
        if (ImgBtn(644f, 504f, btnReturn))
        {
            _page = TitlePage.CharacterSelection;
            returnToSelectionForIndependentRuntime();
        }
        HintBox(484f, 452f, getMessageForIndependentRuntime(), 300f);
    }

    // The creation field/hint style slice stays at the existing viewport composition boundary;
    // this partial does not claim the common OnGUI owner or register another Unity callback.
    internal void EnsureCreationStylesForIndependentRuntime()
    {
        if (fieldStyle != null) return;
        fieldStyle = new GUIStyle(GUI.skin.textField)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 14,
            padding = new RectOffset(2, 2, 0, 0),
            overflow = new RectOffset(0, 0, 0, 0),
            contentOffset = Vector2.zero,
            fixedWidth = 0f,
            fixedHeight = 0f,
            wordWrap = false
        };
        ClearStyleBackgrounds(fieldStyle);
        SetAllTextColors(fieldStyle, Color.white);
    }

    private static bool RadioImg(
        Rect rect,
        Texture2D normal,
        Texture2D selected,
        bool isSelected)
    {
        Texture2D texture = isSelected && selected ? selected : normal;
        if (texture)
        {
            GUI.DrawTexture(rect, texture);
        }

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private void HintBox(float centerX, float y, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (hintSt == null)
        {
            hintSt = new GUIStyle(center)
            {
                fontSize = 13,
                wordWrap = false,
                clipping = TextClipping.Overflow
            };
        }

        System.Collections.Generic.List<string> lines = WrapEstimatedText(text, 13f, maxWidth - 30f);
        float textWidth = 0f;
        for (int index = 0; index < lines.Count; index++)
        {
            textWidth = Mathf.Max(textWidth, EstimateTextWidth(lines[index], 13f));
        }

        float width = Mathf.Clamp(textWidth + 30f, 60f, maxWidth);
        float height = Mathf.Max(24f, lines.Count * 19f + 10f);
        float x = Mathf.Round(centerX - width * 0.5f);
        float top = Mathf.Round(y);
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(x, top, width, height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        for (int index = 0; index < lines.Count; index++)
        {
            GUI.Label(
                new Rect(x, top + 5f + index * 19f, width, 19f),
                lines[index],
                hintSt);
        }

        GUI.color = previous;
    }

    private static float EstimateTextWidth(string text, float fontSize)
    {
        float width = 0f;
        foreach (char character in text)
        {
            width += character > 127 ? fontSize : fontSize * 0.55f;
        }

        return width;
    }

    private static System.Collections.Generic.List<string> WrapEstimatedText(string text, float fontSize, float maxWidth)
    {
        // Official 1.5.8 TitleFlow.WrapEst (0x06000ed6) strips CR before it splits: arm64
        // 06000ed6.c:44-46 calls String.Replace(text, metadata_usage_10438_kind5,
        // metadata_usage_10335_kind5) and only then String.Split(10, 0); the frozen index resolves
        // 10438 to "\r" and 10335 to "" (both decode_state exact), and x86_64 :42-43 is the same
        // pair. So CR never reaches the per-character width sum and only '\n' starts a new line.
        string[] paragraphs = text.Replace("\r", string.Empty).Split(new[] { '\n' }, StringSplitOptions.None);
        System.Collections.Generic.List<string> lines =
            new System.Collections.Generic.List<string>();
        float latinWidth = fontSize * 0.55f;
        for (int paragraphIndex = 0; paragraphIndex < paragraphs.Length; paragraphIndex++)
        {
            string paragraph = paragraphs[paragraphIndex];
            if (paragraph.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            int start = 0;
            float width = 0f;
            for (int index = 0; index < paragraph.Length; index++)
            {
                float characterWidth = paragraph[index] > 127 ? fontSize : latinWidth;
                if (width + characterWidth > maxWidth && start < index)
                {
                    lines.Add(paragraph.Substring(start, index - start));
                    start = index;
                    width = 0f;
                }

                width += characterWidth;
            }

            lines.Add(paragraph.Substring(start));
        }

        return lines;
    }

    private static void SetAllTextColors(GUIStyle style, Color color)
    {
        style.normal.textColor = color;
        style.hover.textColor = color;
        style.active.textColor = color;
        style.focused.textColor = color;
        style.onNormal.textColor = color;
        style.onHover.textColor = color;
        style.onActive.textColor = color;
        style.onFocused.textColor = color;
    }

}
}
