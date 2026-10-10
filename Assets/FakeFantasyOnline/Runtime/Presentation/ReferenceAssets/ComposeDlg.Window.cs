using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ComposeDlg
{
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        var frame = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
        Vector2 saved = Event.current.mousePosition;
        if (frame.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen))
            {
                winX = (Screen.width - NW * s) * 0.5f; winY = (Screen.height - NH * s) * 0.5f;
            }
            Vector2 mp = Event.current.mousePosition;
            GUI.depth = frame.Item1;
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else
            {
                GUI.color = new Color(0.09f, 0.08f, 0.12f, 0.98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture); GUI.color = Color.white;
            }
            if (BitmapFont.Ready) BitmapFont.CenterOutline(L(132, 44, 177, 12),
                modtype == 1 ? "加工炉" : modtype == 2 ? "镶嵌炉" : "合成炉", CraftUI.GOLD, s);
            int cost = -1;
            if (modtype == 1)
            {
                CraftUpgradeUI.DrawRefineSlot(L(103, 78, 220, 215), s);
                DrawRefineMats(mp);
                if (CraftUpgradeUI.RefBagSlot >= 0 && CraftUpgradeUI.CurLevel >= 0) cost = CraftUpgradeUI.Cost;
            }
            else
            {
                DrawCombineList(L(103, 78, 220, 215), mp);
                if (sel >= 0 && sel < recipes.Count) { cost = recipes[sel].cost; DrawMatSlots(recipes[sel], mp); }
            }
            if (cost >= 0)
            {
                DrawCoin(coinG, 163, cost / 1000000); DrawCoin(coinS, 200, cost % 1000000 / 1000);
                DrawCoin(coinC, 236, cost % 1000);
            }
            bool can = modtype == 0 ? sel >= 0 && sel < recipes.Count && recipes[sel].canMake : CraftUpgradeUI.CanDo;
            if (modtype == 1)
            {
                GUI.enabled = can;
                if (CraftUI.Btn(L(100, 447, 90, 22), icoMake, "加工", mp, s))
                { if (can) CraftUpgradeUI.Execute(); else CraftUI.Deny(RefineDenyWhy()); }
                if (CraftUpgradeUI.FailDestroys || CraftUpgradeUI.CurLevel > CraftUpgradeUI.SafeLevel)
                    if (CraftUI.Btn(L(191, 447, 90, 22), icoKeep, "免爆加工", mp, s))
                    { if (can) CraftUpgradeUI.Execute(true); else CraftUI.Deny(RefineDenyWhy()); }
                GUI.enabled = true;
            }
            else
            {
                GUI.enabled = can;
                if (CraftUI.Btn(L(110, 447, 90, 22), icoMake, "合成", mp, s))
                {
                    if (can && NetClient.I != null)
                        NetClient.I.CraftForIndependentRuntime((byte)modtype, recipes[sel].result);
                    else CraftUI.Deny(CombineDenyWhy());
                }
                GUI.enabled = true;
                if (CraftUI.Btn(L(273, 447, 70, 22), icoCancel, "取消", mp, s)) visible = false;
            }
            CraftUI.DrawDeny(L(0, 488, NW, 14), s);
            Rect close = L(340, 40, 20, 19);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (CraftUI.Click(close, mp)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 50), ref winX, ref winY);
            if (modtype == 1 && InventoryDlg.HasHeld && Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 && L(103, 78, 220, 215).Contains(mp))
            {
                if (TryDropRefine((byte)InventoryDlg.HeldTab, InventoryDlg.HeldBagSlot, mp)) InventoryDlg.ClearHeld();
                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mp))
            {
                if (!SysMsgBox.Visible) Event.current.Use();
                PlayerController.SuppressWorldClick();
            }
        }
        finally { if (frame.Item2) Event.current.mousePosition = saved; }
    }
}
}
