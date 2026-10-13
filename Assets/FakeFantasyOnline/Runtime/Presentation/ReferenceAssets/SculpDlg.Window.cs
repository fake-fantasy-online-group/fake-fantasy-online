using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class SculpDlg
{
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        var frame = WinZ.Frame(this, L(0, 0, NW, NH), 10);
        Vector2 saved = Event.current.mousePosition;
        if (frame.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen))
            { winX = (Screen.width - NW * s) * 0.5f; winY = (Screen.height - NH * s) * 0.5f; }
            GUI.depth = frame.Item1;
            Vector2 mp = Event.current.mousePosition;
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else
            {
                GUI.color = new Color(0.09f, 0.08f, 0.12f, 0.98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture); GUI.color = Color.white;
            }
            if (BitmapFont.Ready) BitmapFont.CenterOutline(L(155, 45, 156, 12), title, CraftUI.GOLD, s);
            if (!SocketUI.HasTarget)
            {
                if (BitmapFont.Ready) BitmapFont.CenterOutline(L(168, 235, 132, 12), "从背包拖入装备", CraftUI.GRAY, s);
            }
            else
            {
                if (SocketUI.SelIcon != null) ItemIconUI.Draw(L(216, 128, 32, 32), SocketUI.SelIcon, s);
                if (BitmapFont.Ready) BitmapFont.CenterOutline(L(168, 235, 132, 12), SocketUI.SelName, CraftUI.GOLD, s);
                if (L(216, 128, 32, 32).Contains(mp)) UITip.Show(TooltipContent.Fmt(SocketUI.SelName, ""), s);
            }
            if (InventoryDlg.HasHeld && Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                L(200, 120, 64, 80).Contains(mp))
            {
                if (TryDropSculp((byte)InventoryDlg.HeldTab, InventoryDlg.HeldBagSlot)) InventoryDlg.ClearHeld();
                Event.current.Use();
            }
            int holes = SocketUI.HoleCount;
            for (int i = 0; i < 5; i++)
            {
                if (i >= holes) continue;
                Rect hole = HoleAbs(i);
                if (icoHole != null) GUI.DrawTexture(hole, icoHole);
                if (SocketUI.Sockets[i] <= 0)
                {
                    if (BitmapFont.Ready) BitmapFont.CenterOutline(new Rect(hole.x, hole.y + 14f * s, hole.width, 12f * s), "空", CraftUI.GRAY, s);
                }
                else
                {
                    var named = i < SocketUI.SocketNames.Count ? SocketUI.SocketNames[i] : default;
                    var icon = new Rect(hole.x + 4f * s, hole.y + 4f * s, 32f * s, 32f * s);
                    if (named.Item3 != null) ItemIconUI.Draw(icon, named.Item3, s);
                    else if (BitmapFont.Ready) BitmapFont.CenterOutline(new Rect(hole.x, hole.y + 14f * s, hole.width, 12f * s), "符", CraftUI.GOLD, s);
                    if (!string.IsNullOrEmpty(named.Item2) && icon.Contains(mp)) UITip.Show(TooltipContent.Fmt(named.Item2, ""), s);
                }
            }
            DrawInfo(L(122, 316, 224, 90));
            if (SocketUI.HasTarget && SocketUI.CanDrill)
            {
                int cost = SocketUI.NextCost;
                Coin(coinG, 170, cost / 1000000); Coin(coinS, 207, cost % 1000000 / 1000); Coin(coinC, 243, cost % 1000);
            }
            GUI.enabled = SocketUI.CanDrill;
            bool drillFurnace = title == "打孔炉";
            if (CraftUI.Btn(L(drillFurnace ? 100 : 116, 447, 90, 22), icoMake, "打孔", mp, s))
            { if (SocketUI.CanDrill) SocketUI.Drill(); else CraftUI.Deny(DrillDenyWhy()); }
            if (drillFurnace && SocketUI.FailDestroys)
                if (CraftUI.Btn(L(191, 447, 90, 22), icoKeep, "免爆打孔", mp, s))
                { if (SocketUI.CanDrill) SocketUI.Drill(true); else CraftUI.Deny(DrillDenyWhy()); }
            GUI.enabled = true;
            // The original drilling-furnace branch omits the lower Cancel button.
            if (!drillFurnace && CraftUI.Btn(L(284, 447, 70, 22), icoCancel, "取消", mp, s)) visible = false;
            if (icoClose != null) GUI.DrawTexture(L(338, 40, 20, 19), icoClose);
            if (CraftUI.Click(L(338, 40, 20, 19), mp)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 50), ref winX, ref winY);
            if (InventoryDlg.HasHeld && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                for (int i = 0; i < holes && i < 5; i++)
                    if (SocketUI.CanInlayHole(i) && HoleAbs(i).Contains(mp))
                    {
                        if (TryDropRune((byte)InventoryDlg.HeldTab, InventoryDlg.HeldBagSlot, i)) InventoryDlg.ClearHeld();
                        Event.current.Use(); break;
                    }
            }
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mp) && !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally { if (frame.Item2) Event.current.mousePosition = saved; }
    }
}
}
