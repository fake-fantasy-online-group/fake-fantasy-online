using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        public struct RefundRow
        {
            public int itemId;
            public int unitPrice;
            public int bundle;
            public int day;
            public int shares;
            public int have;
            public int gain;
            public int leftDays;
            public string name;
            public Texture2D icon;
        }

        private static bool refundLoaded;
        private int refundScroll;

        // Official 0x06000a9b.
        public static void SetRefundTab(bool on) => refundTabOn = on;

        // Official 0x06000a9c. Receiving an empty list still completes loading.
        public static void SetRefunds(int pct, int days, List<RefundRow> rows, string cats = "", bool usedOk = true)
        {
            refundPct = pct;
            refundDays = days;
            refundCats = cats ?? "";
            refundUsedOk = usedOk;
            refundRows = rows ?? new List<RefundRow>();
            refundLoaded = true;
        }

        // Official 0x06000ac2. Keep the server-authored refund eligibility and receipt fields.
        private void DrawRefund(Vector2 mouse)
        {
            Rect background = L(28f, 65f, 812f, 524f);
            FrameBox(background);
            tipText = null;
            Rect header = L(40f, 98f, 786f, 34f);
            FrameBox(header);
            FitLabel(L(46f, 104f, 774f, 22f), "货架回收 —— 按【购买时实付价】的 " + refundPct + "% 返还彩玉" +
                (refundDays > 0 ? "，" + refundDays + " 天内有效" : ""), GOLD);
            string policy = refundUsedOk
                ? "· 穿过、融合过（用分离卷轴拆回来）的照样可以回收，相当于只花 " + (100 - refundPct) + "% 租用；已上锁 / 已加工（精炼·打孔·镶嵌）的不收"
                : "· 只回收【还没动过】的东西：穿戴过 / 融合过（哪怕又分离出来）/ 解过绑 / 进过衣橱 / 已上锁 / 已加工的都不能回收";
            FitLabel(L(46f, 140f, 774f, 14f), policy, GRAY);
            FitLabel(L(46f, 156f, 774f, 14f), "· 一份买几个就要交回几个（如 2 彩玉买 10 个，要交回 10 个才能回收这一份）", GRAY);
            FitLabel(L(46f, 172f, 774f, 14f), string.IsNullOrEmpty(refundCats)
                ? "· 每日特价、限时抢购（金币购买）与宠物蛋暂不支持回收"
                : "· 只回收【" + refundCats.Replace(" / ", "、") + "】这几类；每日特价、限时抢购（金币购买）与宠物蛋不支持", GRAY);
            Rect panel = L(40f, 194f, 786f, 364f);
            FrameBox(panel);
            if (!refundLoaded)
            {
                LblC(L(40f, 296f, 786f, 14f), "载入中…", GRAY);
                return;
            }
            if (refundRows.Count == 0)
            {
                LblC(L(40f, 286f, 786f, 14f), refundDays > 0
                    ? "最近 " + refundDays + " 天没有可回收的购买记录" : "没有可回收的购买记录", GRAY);
                LblC(L(40f, 308f, 786f, 14f), "（只有本功能上线之后，从【商城】用彩玉买的" +
                    (string.IsNullOrEmpty(refundCats) ? "" : refundCats.Replace(" / ", "、")) + "才有回收记录）", GRAY);
                return;
            }
            refundScroll = Mathf.Clamp(refundScroll, 0, Mathf.Max(0, refundRows.Count - 12));
            for (int i = 0; i < 12; i++)
            {
                int index = i + refundScroll;
                if (index >= refundRows.Count) break;
                RefundRow row = refundRows[index];
                float y = 196f + i * 30f;
                Rect rowRect = L(44f, y, 764f, 28f);
                bool hover = rowRect.Contains(mouse);
                ArtworkSkin.Row(rowRect, s, hover);
                if (row.icon != null) ItemIconUI.Draw(L(48f, y + 2f, 26f, 26f), row.icon, s);
                FitLabel(L(80f, y + 7f, 166f, 14f), Trunc(row.name, 10) + (row.bundle > 1 ? "×" + row.bundle : ""), Color.white);
                FitLabel(L(250f, y + 7f, 116f, 14f), "实付 " + row.unitPrice + " 彩玉/份", GRAY);
                FitLabel(L(370f, y + 7f, 76f, 14f), "可退 " + row.shares + " 份", Color.white);
                int required = Mathf.Max(row.bundle, 1);
                FitLabel(L(450f, y + 7f, 96f, 14f), "背包 " + row.have + "/" + required,
                    row.have >= required ? new Color(0.55f, 1f, 0.55f, 1f) : new Color(1f, 0.45f, 0.45f, 1f));
                FitLabel(L(550f, y + 7f, 86f, 14f), "退 " + row.gain + " 彩玉", YEL);
                if (row.leftDays >= 0)
                    FitLabel(L(640f, y + 7f, 76f, 14f), "剩 " + row.leftDays + " 天",
                        row.leftDays < 4 ? new Color(1f, 0.6f, 0.35f, 1f) : GRAY);
                Rect button = L(720f, y + 3f, 86f, 22f);
                bool available = row.have >= required && row.shares > 0;
                TextButtonFace(button, "回收 1 份", mouse, false, available);
                if (hover)
                {
                    string description = "购买时实付：" + row.unitPrice + " 彩玉" +
                        (row.bundle > 1 ? "（这一份给 " + row.bundle + " 个）" : "") +
                        "\n回收返还：" + row.gain + " 彩玉/份（" + refundPct + "%）\n还能回收：" + row.shares + " 份" +
                        (row.have < required ? "\n\n背包里可回收的不够 " + required + " 个：已绑定/已上锁/已融合/已加工的不算。" : "");
                    tipText = TooltipContent.Fmt(row.name, description);
                }
                if (available && Click(button, mouse))
                {
                    RefundRow captured = row;
                    SysMsgBox.Show("回收【" + row.name + (row.bundle > 1 ? "】×" + row.bundle : "】") +
                        "，返还 " + row.gain + " 彩玉？\n（物品会被收回，不可撤销）",
                        () => NetClient.I?.RackRefundForIndependentRuntime(captured.itemId, captured.unitPrice, captured.bundle, captured.day, 1));
                }
            }
            NativeScroll.BarRows(this, L(810f, 196f, 12f, 360f), 12, refundRows.Count, ref refundScroll);
            Event e = Event.current;
            if (panel.Contains(mouse) && e.type == EventType.ScrollWheel)
            {
                refundScroll += e.delta.y < 0f ? -1 : 1;
                e.Use();
            }
            // The original method computes tipText but has no UITip.Show call.
        }
    }
}
