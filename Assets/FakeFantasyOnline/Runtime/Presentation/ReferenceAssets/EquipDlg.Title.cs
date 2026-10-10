using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipDlg
{
    // 030d ARM3429..3987 / x863233..3841. Rows precede wheel/clamp/buttons.
    private void DrawTitles(GuiFrame f, Event ev)
    {
        Rect panel = f.L(284,12,98,112);
        Patch9(panel);
        for (int index = 0; index < 5; index++)
        {
            int category = index + 1;
            float y = 20f + index * 20f;
            Texture2D radio = category == titleCat && radioBtn[1] != null ? radioBtn[1] : radioBtn[0];
            if (radio != null) GUI.DrawTexture(f.L(292,y + 1,14,13),radio);
            if (BitmapFont.Ready) CenterPlain(f.L(312,y + 2,54,12),TitleCats[index],Color.white);
            if (titleLine != null) GUI.DrawTexture(f.L(288,y + 14,90,2),titleLine);
            if (GUI.Button(f.L(288,y,90,16),GUIContent.none,GUIStyle.none))
            {
                titleCat = category;
                if (category == 2)
                {
                    taskOpen = true;
                    taskSel = 0;
                    taskScroll = 0;
                }
                else
                {
                    taskOpen = false;
                    ApplyTitle(category,0);
                }
            }
        }
        GUI.Button(panel,GUIContent.none,GUIStyle.none);
        if (!taskOpen) return;
        Rect ownedPanel = f.L(384,36,126,179);
        Patch9(ownedPanel);
        int count = CharData.ownedTitles.Count;
        for (int index = 0; index < Math.Min(7,count - taskScroll); index++)
        {
            int titleIndex = taskScroll + index;
            float y = 43f + index * 20f;
            if (titleIndex == taskSel && itemSel != null)
                GUI.DrawTexture(f.L(391,y + 3,87,14),itemSel);
            if (BitmapFont.Ready)
                CenterPlain(f.L(391,y + 4,100,12),CharData.ownedTitles[titleIndex],Color.white);
            if (titleLine != null) GUI.DrawTexture(f.L(391,y + 18,90,2),titleLine);
            if (GUI.Button(f.L(391,y,100,20),GUIContent.none,GUIStyle.none)) taskSel = titleIndex;
        }
        if (count < 1)
        {
            if (BitmapFont.Ready)
                CenterPlain(f.L(384,100,126,12),"暂无称号",new Color(150f/255f,150f/255f,150f/255f,1f));
            if (okBtn[3] != null) GUI.DrawTexture(f.L(390,188,44,22),okBtn[3]);
            if (delBtn[3] != null) GUI.DrawTexture(f.L(436,188,44,22),delBtn[3]);
        }
        else
        {
            if (count > 7)
            {
                int maximum = count - 7;
                // Original re-reads Event.current here, rather than the outer ev capture.
                if (Event.current.type == EventType.ScrollWheel && ownedPanel.Contains(Event.current.mousePosition))
                {
                    taskScroll = Mathf.Clamp(taskScroll + (Event.current.delta.y < 0f ? -1 : 1),0,maximum);
                    Event.current.Use();
                }
                taskScroll = Mathf.Clamp(taskScroll,0,maximum);
                // The real arrows are 15x10; retain the forgiving 14px-high hit area.
                Rect up = f.L(494,40,15,14), down = f.L(494,174,15,14);
                if (TitleArrow(up, "res/uires/common/miniupbtn.gsa", "▲", taskScroll > 0, ev))
                    taskScroll = Math.Max(0,taskScroll - 1);
                if (TitleArrow(down, "res/uires/common/minidnbtn.gsa", "▼", taskScroll < maximum, ev))
                    taskScroll = Math.Min(maximum,taskScroll + 1);
            }
            if (Btn(f.L(390,188,44,22),okBtn,ev))
            {
                ApplyTitle(2,taskSel);
                taskOpen = false;
            }
            if (Btn(f.L(436,188,44,22),delBtn,ev))
            {
                CharData.SetCurrentTitleForIndependentRuntime(string.Empty);
                NetClient.I?.SetTitleForIndependentRuntime(string.Empty);
            }
        }
        GUI.Button(ownedPanel,GUIContent.none,GUIStyle.none);
    }

    private bool TitleArrow(Rect hit, string resource, string fallback, bool enabled, Event current)
    {
        Texture2D[] frames = ArtworkSkin.Frames(resource, 3);
        bool hover = enabled && GUI.enabled && hit.Contains(current.mousePosition);
        if (frames != null && frames.Length != 0 && frames[0] != null)
            ArtworkSkin.IconFace(new Rect(hit.x, hit.y + 2f * s, 15f * s, 10f * s),
                frames, hover, hover && Input.GetMouseButton(0), enabled);
        else if (BitmapFont.Ready)
            CenterPlain(new Rect(hit.x, hit.y + 2f * s, hit.width, 12f * s), fallback,
                enabled ? Color.white : new Color(110f/255f,110f/255f,110f/255f,1f));
        bool previous = GUI.enabled;
        try
        {
            GUI.enabled = previous && enabled;
            return GUI.Button(hit, GUIContent.none, GUIStyle.none);
        }
        finally { GUI.enabled = previous; }
    }

    // Reuses 0311's original callback/store/membership/send order against its actual roots.
    private void ApplyTitle(int category, int sub)
    {
        OnSetTitle?.Invoke(category,sub);
        string race = RaceNames[Mathf.Clamp(CharData.race,0,4)];
        if (category == 1)
            CharData.SetCurrentTitleForIndependentRuntime("初出茅庐的" + race);
        else if (category == 2)
        {
            if (sub >= 0 && sub < CharData.ownedTitles.Count)
                CharData.SetCurrentTitleForIndependentRuntime(CharData.ownedTitles[sub]);
        }
        else CharData.SetCurrentTitleForIndependentRuntime(string.Empty);
        if (NetClient.I == null) return;
        if (CharData.title.Length != 0 && !CharData.ownedTitles.Contains(CharData.title)) return;
        NetClient.I.SetTitleForIndependentRuntime(CharData.title);
    }
}
}
