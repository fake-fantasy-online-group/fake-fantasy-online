using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class TaskDlg
{
    // Official 1.5.8 token 06000e8c. Draws the detail card for one definition and returns the
    // content height. The two local functions below are the official members
    // <DetailLayout>g__L2|43_0 and <DetailLayout>g__W2|43_1; every line they draw is gated on the
    // same draw flag the caller passes, so the layout pass and the paint pass cannot diverge.
    public float DetailLayout(TaskDB.Def sel, bool draw)
    {
        // The column label and the wrapped body, both stopped at the same flag.
        void L2(float x, float y, string t, Color c)
        {
            if (draw && BitmapFont.Ready) BitmapFont.DrawOutline(x * s, y * s, t, c, s);
        }

        void W2(float x, float y, float wPx, string t, Color c)
        {
            if (draw) WrapLocal(x, y, wPx, t, c);
        }

        TaskClient.ActiveTask active = null;
        if (!selIsRec) active = TaskClient.GetActive(selId);

        L2(6f, 6f, sel.name, GOLD);

        float y = 24f;
        if (!string.IsNullOrEmpty(sel.publisher))
        {
            L2(6f, y, "发布：" + sel.publisher, new Color(0.8f, 0.9f, 0.7f, 1f));
            y += 16f;
        }

        if (!string.IsNullOrEmpty(sel.place))
        {
            L2(6f, y, "地点：" + sel.place, new Color(0.6f, 0.85f, 1f, 1f));
            y += 16f;
        }

        if (sel.minLevel >= 1)
        {
            string line = string.Format("等级：{0} 级以上", sel.minLevel);
            if (sel.maxLevel < 999 && sel.minLevel < sel.maxLevel)
                line += string.Format("（推荐 {0}~{1} 级）", sel.minLevel, sel.maxLevel);
            Color color = new Color(0.8f, 0.9f, 0.7f, 1f);
            if (CharData.level < sel.minLevel)
            {
                line += string.Format("（你 {0} 级，未达要求）", CharData.level);
                color = new Color(1f, 0.45f, 0.4f, 1f);
            }
            L2(6f, y, line, color);
            y += 16f;
        }

        W2(6f, y, 230f, sel.desc, WHITE);
        y += EstLines(sel.desc, 230f) * 14f + 8f;

        if (selIsDone)
        {
            W2(6f, y, 230f, "✔ 此任务已完成", new Color(0.65f, 1f, 0.65f, 1f));
            y += EstLines("✔ 此任务已完成", 230f) * 14f + 8f;
        }
        else if (selIsRec)
        {
            string line = "▸ 前往【" + sel.publisher + "】处接取此任务";
            W2(6f, y, 230f, line, new Color(1f, 0.85f, 0.4f, 1f));
            y += EstLines(line, 230f) * 14f + 8f;
        }
        else if (active != null)
        {
            if (active.total > 0)
            {
                // The official body clamps step+1 down to total before formatting.
                int shown = active.total;
                if (active.step + 1 < active.total) shown = active.step + 1;
                L2(6f, y, string.Format("进度 {0}/{1}", shown, active.total), GOLD);
                y += 18f;
            }

            if (!string.IsNullOrEmpty(active.text))
            {
                string line = "▸ " + active.text;
                W2(6f, y, 230f, line, new Color(0.7f, 0.95f, 0.7f, 1f));
                y += EstLines(line, 230f) * 14f + 6f;
            }

            if (!string.IsNullOrEmpty(active.progress))
            {
                L2(12f, y, active.progress, WHITE);
                y += 16f;
            }

            if (!string.IsNullOrEmpty(active.to))
            {
                string line = (active.ready == 1 ? "▸ 回到【" : "▸ 前往【") + active.to +
                    (active.ready == 1 ? "】复命领赏" : "】");
                L2(6f, y, line, new Color(0.6f, 0.85f, 1f, 1f));
                y += 16f;
            }

            if (active.remainSec >= 0)
            {
                int remain = active.remainSec - Mathf.FloorToInt(Time.realtimeSinceStartup - active.recvTime);
                if (remain < 0) remain = 0;
                Color color = remain > 60
                    ? new Color(1f, 0.8f, 0.4f, 1f)
                    : new Color(1f, 0.4f, 0.3f, 1f);
                L2(6f, y, string.Format("限时剩余：{0:00}:{1:00}", remain / 60, remain % 60), color);
                y += 18f;
            }

            y += 4f;
        }

        if (!string.IsNullOrEmpty(sel.award))
        {
            L2(6f, y, "奖励：", GOLD);
            W2(12f, y + 16f, 220f, sel.award, new Color(0.95f, 0.85f, 0.5f, 1f));
            y += 16f + EstLines(sel.award, 220f) * 14f;
        }

        return y + 6f;
    }
}

}
