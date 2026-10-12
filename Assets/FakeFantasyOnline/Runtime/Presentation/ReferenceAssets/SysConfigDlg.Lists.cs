using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysConfigDlg
{
    private void DrawPickList(Vector2 mouse)
    {
        Rect bounds = L(104f, 303f, 122f, 108f);
        ArtworkSkin.Panel(bounds, s);
        for (int quality = 0; quality < 5; quality++)
        {
            Rect row = new Rect(bounds.x + 2f * s, bounds.y + (4f + quality * 20f) * s,
                bounds.width - 4f * s, 20f * s);
            bool hover = row.Contains(mouse);
            ArtworkSkin.Row(row, s, hover || quality == pickQ);
            if (BitmapFont.Ready)
            {
                float labelX = row.x + 8f * s;
                float labelY = row.y + 4f * s;
                string label = (quality == pickQ ? "◆" : "  ") + PickName(quality);
                Color color = quality == pickQ
                    ? new Color32(255, 235, 170, 255)
                    : new Color32(206, 200, 180, 255);
                BitmapFont.Draw(labelX, labelY, label, color, s);
            }
            if (hover && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                pickQ = quality;
                pickOpen = false;
                Event.current.Use();
                return;
            }
        }
        if (Event.current.type == EventType.MouseDown && !bounds.Contains(mouse)) pickOpen = false;
    }
    private void DrawResList(Vector2 mouse)
    {
        var options = GameSettings.ResOpts;
        float height = options.Length * 20f + 8f;
        Rect bounds = L(104f, RESY - height - 2f, 122f, height);
        ArtworkSkin.Panel(bounds, s);
        for (int index = 0; index < options.Length; index++)
        {
            Rect row = new Rect(bounds.x + 2f * s, bounds.y + (4f + index * 20f) * s,
                118f * s, 20f * s);
            bool hover = row.Contains(mouse);
            ArtworkSkin.Row(row, s, hover || index == curRes);
            if (BitmapFont.Ready)
            {
                Color selectedColor = new Color32(221, 198, 149, 255);
                if (index == curRes)
                    BitmapFont.Draw(winX + 106f * s, row.y + 4f * s, "◆", selectedColor, s);
                float labelX = winX + 118f * s;
                float labelY = row.y + 4f * s;
                string label = string.Format("{0}×{1}", options[index].Item1, options[index].Item2);
                BitmapFont.Draw(labelX, labelY, label,
                    index == curRes ? selectedColor : Color.white, s);
            }
            if (hover && !SysMsgBox.Visible && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                curRes = index;
                resOpen = false;
                GameSettings.ApplyRes(index);
                Event.current.Use();
                return;
            }
        }
        if (Event.current.type == EventType.MouseDown && !bounds.Contains(mouse)) resOpen = false;
    }

}

}
