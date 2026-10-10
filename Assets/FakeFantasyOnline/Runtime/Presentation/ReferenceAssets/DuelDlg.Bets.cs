using System;
using System.Threading;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class DuelDlg
{
    // 官方签名：DrawBets(Rect p, float s, float f, Func<float,float,float,float,Rect> L)。
    private void DrawBets(Rect panel, float scale, float font,
        Func<float, float, float, float, Rect> L)
    {
        TX(panel.x + 22f * scale, panel.y + 70f * scale, "正在进行的擂台", C(220, 210, 180), font);
        if (Tab(L(287, 64, 60, 22), "刷新", false, font, scale))
            _ = commands?.RequestListAsync(CancellationToken.None);
        Rect list = L(20, 90, 327, 112);
        Box(list, scale);
        float inset = 6f * scale;
        if (rows.Count == 0)
            TX(list.x + inset + 4f * scale, list.y + inset * 2f,
                "现在没有擂台在进行；签约后会全服播报。", C(150, 150, 160), font);
        for (int i = 0; i < 5 && i < rows.Count; i++)
        {
            Row row = rows[i];
            Rect rect = UIScale.Snap(new Rect(list.x + inset, list.y + inset + i * 20f * scale,
                list.width - 12f * scale, 20f * scale));
            bool rowSelected = _sel == i;
            bool hover = rect.Contains(Event.current.mousePosition);
            if (rowSelected && rowSel != null) GUI.DrawTexture(rect, rowSel);
            else if (hover)
            {
                GUI.color = new Color(1, 1, 1, .08f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (rowLine != null && i > 0)
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, Mathf.Max(1f, scale)), rowLine);
            string phase = row.phase == 0 ? "等人进场" : row.phase == 1 ? "倒计时" : row.phase == 2 ? "激战中" : "已结束";
            string mode = row.mode == 1 ? string.Format("金币场{0}金", row.entryGold) : "装备场";
            string text = string.Format("{0} VS {1}  {2}  {3}  {4}  观战{5}", row.a, row.b,
                mode, phase, row.betsOpen ? "可下注" : "停注", row.spectators);
            TX(rect.x + inset, rect.y + 4f * scale, text,
                rowSelected ? C(255, 240, 200) : C(210, 210, 220), font);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) _sel = i;
        }
        TX(panel.x + 22f * scale, panel.y + 214f * scale,
            string.Format("下注金额（金，{0}~{1}）", betMin, betMax), C(220, 210, 180), font);
        Rect amountRect = L(190, 208, 80, 22);
        Box(amountRect, scale);
        _betText = UiInput.Field(UIScale.Snap(new Rect(amountRect.x + 4f * scale,
            amountRect.y + 2f * scale, amountRect.width - 8f * scale, amountRect.height - 4f * scale)),
            "duel_bet", _betText ?? string.Empty, 7, editSt, scale);
        TX(panel.x + 278f * scale, panel.y + 214f * scale,
            string.Format("手续费{0}%", feePct), C(150, 150, 160), font);
        TX(panel.x + 22f * scale, panel.y + 238f * scale,
            "赢方按本金比例分彩池；对面无人押只退 90%；作废全退。", C(160, 160, 172), font);
        bool hasSelected = _sel >= 0 && _sel < rows.Count;
        Row selected = hasSelected ? rows[_sel] : default;
        long amount = 0;
        bool canBet = hasSelected && selected.betsOpen &&
            long.TryParse((_betText ?? string.Empty).Trim(), out amount) && amount >= betMin && amount <= betMax;
        if (SkinBtn(L(20, 268, 100, 39), redBtn,
            !hasSelected ? "押  A" : "押 " + selected.a, canBet, font))
            _ = commands?.BetAsync(selected.id, 0, (int)long.Parse(_betText.Trim()), CancellationToken.None);
        if (SkinBtn(L(133.5f, 268, 100, 39), redBtn,
            !hasSelected ? "押  B" : "押 " + selected.b, canBet, font))
            _ = commands?.BetAsync(selected.id, 1, (int)long.Parse(_betText.Trim()), CancellationToken.None);
        if (SkinBtn(L(247, 268, 100, 39), redBtn, "前往观战", hasSelected, font))
        {
            _ = commands?.SpectateAsync(selected.id, CancellationToken.None);
            Close();
        }
    }
}
}
