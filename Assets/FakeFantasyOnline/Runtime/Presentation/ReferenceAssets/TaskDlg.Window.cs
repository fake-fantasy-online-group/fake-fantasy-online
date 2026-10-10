using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class TaskDlg
{
    // Official 1.5.8 token 06000e86. The whole window composition: four tabs, one narrow scroll
    // view that stacks the active list above the offered/finished list, the find box, the abandon
    // button, the detail card in a second scroll view, the close affordances and the drag handle.
    //
    // Both architectures were read instruction by instruction at the two BeginScrollView call
    // sites (arm64 0x3731b8 / 0x374074, x86_64 0x3855cb / 0x3865fe). The list view is
    // position (winX+18s, winY+68s, 161s, 293s) with viewRect (0, 0, 161s-16, (leftH+rightH)*s);
    // the detail view is position (winX+218s, winY+63s, 265s, 305s) with viewRect
    // (0, 0, 265s-18s, h*s). The baked constants agree on both sides: 18/68, 161/293, -16,
    // 20/26/50/26, 122, 218/63, 265/305, -18, 187/69, 15/291, 24/370, 96/12, 124/365, 60/22,
    // 214/370, 90/22, 470/64, 15/303, 469/18, 20/19, 419/371, 70/19, 228/200.
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - s * 495f) * 0.5f;
            winY = (Screen.height - s * 397f) * 0.5f;
        }

        // Actual 1.6.0 keeps interaction coordinates from before Frame, and separately
        // saves the post-Frame position for restoration after the drag sentinel.
        Vector2 interactionMouse = Event.current.mousePosition;
        var stacking = WinZ.Frame(this, L(0f, 0f, 495f, 397f), 10);
        Vector2 savedMouse = Event.current.mousePosition;
        bool dragged = stacking.Item2;
        if (dragged) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            GUI.depth = stacking.Item1;

            if (bg != null) GUI.DrawTexture(L(0f, 0f, 495f, 397f), bg);
            else
            {
                GUI.color = new Color(0.09f, 0.08f, 0.12f, 0.98f);
                GUI.DrawTexture(L(0f, 0f, 495f, 397f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // The official body allocates a four-slot texture array and fills only the first
            // three, so the fourth tab always falls through to the text-only path below.
            Texture2D[] tabs = new Texture2D[4];
            tabs[0] = tabDoing;
            tabs[1] = tabWorld;
            tabs[2] = tabTale;
            string[] labels = { "进行中", "世界", "传说", "已完成" };

            int column = 14;
            for (int i = 0; i < 4; i++)
            {
                Rect r = L(column, 40f, 61f, 23f);
                bool dim = i != tab;
                if (tabs[i] != null)
                {
                    GUI.color = dim ? new Color(0.62f, 0.62f, 0.62f, 0.85f) : Color.white;
                    GUI.DrawTexture(r, tabs[i]);
                    GUI.color = Color.white;
                }
                else
                {
                    if (dim)
                    {
                        GUI.color = new Color(0f, 0f, 0f, 0.25f);
                        GUI.DrawTexture(r, Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }
                    if (BitmapFont.Ready)
                        BitmapFont.CenterOutline(r, labels[i], i == tab ? GOLD : GRAY, s);
                }
                if (Click(r, interactionMouse)) tab = i;
                column += 61;
            }

            GUI.color = new Color(0f, 0f, 0f, 0.18f);
            GUI.DrawTexture(L(18f, 68f, 161f, 293f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            List<TaskClient.ActiveTask> rows = new List<TaskClient.ActiveTask>();
            foreach (TaskClient.ActiveTask t in TaskClient.Active)
            {
                TaskDB.Def def = TaskDB.Get(t.id);
                int type = def != null ? def.type : 1;
                if (tab == 0 || (tab == 1 && type == 1) || (tab == 2 && type == 2)) rows.Add(t);
            }

            List<TaskDB.Def> right = new List<TaskDB.Def>();
            if (tab == 3)
            {
                foreach (int id in TaskClient.Done)
                {
                    TaskDB.Def def = TaskDB.Get(id);
                    if (def != null) right.Add(def);
                }
                // Official 06000e96 (TaskDlg+<>c.<OnGUI>b__36_0): Int32.CompareTo on the two
                // definitions' id fields, with a real null-reference throw when either is null.
                right.Sort((a, b) => a.id.CompareTo(b.id));
                rows.Clear();
            }
            else
            {
                foreach (TaskDB.Def def in TaskClient.Recommended(30))
                {
                    if (tab == 0 || (tab == 1 && def.type == 1) || (tab == 2 && def.type == 2))
                        right.Add(def);
                }
            }

            float titlePad = 6f;
            float rowH = 14f;
            float leftH = rows.Count != 0 ? rows.Count * 20f + 26f + 50f + 26f : 122f;
            float rightH = right.Count != 0 ? right.Count * 20f : 20f;

            // Captured by the local function below, exactly like the official
            // <>c__DisplayClass36_0 (titlePad / view / rowH).
            Rect view = new Rect(0f, 0f, 161f * s - 16f, (leftH + rightH) * s);

            // Official 1.5.8 token 06000e91.
            float SectionTitle(float yy, string txt)
            {
                yy += titlePad * s;
                GUI.color = new Color(0.3f, 0.24f, 0.1f, 0.9f);
                GUI.DrawTexture(new Rect(0f, yy, view.width, rowH * s), Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (BitmapFont.Ready) BitmapFont.DrawOutline(s * 4f, yy + s, txt, GOLD, s);
                return yy + rowH * s + s * titlePad;
            }

            listScroll = GUI.BeginScrollView(L(18f, 68f, 161f, 293f), listScroll, view,
                GUIStyle.none, GUIStyle.none);

            float y = SectionTitle(0f, tab == 3 ? "———" : "进行中");
            float rowPx = rowH * s;

            if (rows.Count == 0)
            {
                if (BitmapFont.Ready)
                {
                    string empty = tab == 0 ? "暂无进行中的任务"
                        : tab == 3 ? ""
                        : tab == 1 ? "暂无世界任务"
                        : "暂无传说任务";
                    BitmapFont.DrawOutline(s * 4f, y + s, empty, GRAY, s);
                }
                y += s * 20f;
            }
            else
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    TaskClient.ActiveTask t = rows[i];
                    TaskDB.Def def = DisplayDef(t.id);
                    string name = def != null ? def.name : "任务" + t.id.ToString();

                    // The terminal test is inlined here by the official body rather than calling
                    // ObjectivesMet: total >= 1, the ready byte is 1, and total-1 <= step.
                    bool done = t != null && t.total >= 1 && t.ready == 1 && t.total - 1 <= t.step;

                    if (!selIsRec && t.id == selId)
                    {
                        GUI.color = new Color(0.45f, 0.36f, 0.16f, 0.7f);
                        GUI.DrawTexture(new Rect(0f, y, view.width, rowPx), Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }

                    if (BitmapFont.Ready)
                    {
                        string text = (done ? "★" : "") + name;
                        BitmapFont.DrawOutline(s * 4f, y + s, text, done ? GOLD : WHITE, s);
                    }

                    // The row hit test re-reads Event.current.mousePosition, so it sees the
                    // -9999 sentinel the drag guard installed above.
                    if (new Rect(0f, y, view.width, rowPx).Contains(Event.current.mousePosition)
                        && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        selId = t.id;
                        selIsRec = false;
                        selIsDone = false;
                        Event.current.Use();
                    }

                    y += s * 20f;
                }
            }

            string rightTitle = tab == 3
                ? string.Format("已完成（{0}）", right.Count)
                : "可接任务（按接近你等级排序，最多30条）";
            y = SectionTitle(y + s * 50f, rightTitle);

            if (right.Count == 0)
            {
                if (BitmapFont.Ready)
                {
                    string empty = tab == 3 ? "你还没有完成任何任务" : "暂无适合的可接任务";
                    BitmapFont.DrawOutline(s * 4f, y + s, empty, GRAY, s);
                }
            }
            else
            {
                for (int i = 0; i < right.Count; i++)
                {
                    TaskDB.Def def = right[i];

                    // Opposite of the left list: the highlight here requires selIsRec.
                    if (selIsRec && def.id == selId)
                    {
                        GUI.color = new Color(0.45f, 0.36f, 0.16f, 0.7f);
                        GUI.DrawTexture(new Rect(0f, y, view.width, rowPx), Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }

                    string suffix = def.minLevel > 0 ? string.Format("  [{0}级]", def.minLevel) : "";
                    if (BitmapFont.Ready)
                        BitmapFont.DrawOutline(s * 4f, y + s, def.name + suffix,
                            new Color(0.72f, 0.88f, 1f, 1f), s);

                    if (new Rect(0f, y, view.width, rowPx).Contains(Event.current.mousePosition)
                        && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        selId = def.id;
                        selIsRec = true;
                        selIsDone = tab == 3;
                        Event.current.Use();
                    }

                    y += s * 20f;
                }
            }

            GUI.EndScrollView();
            // Third argument is the viewport height (293*s), fourth the content height.
            NativeScroll.BarAt(this, L(187f, 69f, 15f, 291f), 293f * s, (leftH + rightH) * s,
                ref listScroll);

            float findX = winX + 24f * s;
            float findY = winY + 370f * s;
            GUI.color = new Color(0f, 0f, 0f, 0.3f);
            GUI.DrawTexture(new Rect(findX, findY, 96f * s, 12f * s), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.SetNextControlName("task_find");
            // The +3 inset is unscaled in the official body, only the size is scaled.
            findText = GUI.TextField(new Rect(findX + 3f, findY, 96f * s - 6f, 12f * s),
                findText ?? "", 16, EditStyle());

            Btn(L(124f, 365f, 60f, 22f), icoFind, "查找", interactionMouse);

            if (!selIsRec && selId != 0 && TaskClient.IsActive(selId))
            {
                if (Btn(L(214f, 370f, 90f, 22f), icoStop, "放弃任务", interactionMouse))
                {
                    int aid = selId;
                    TaskDB.Def def = DisplayDef(aid);
                    string name = def != null && def.name != null
                        ? def.name
                        : "任务" + aid.ToString();
                    // Official 06000e98 (TaskDlg+<>c__DisplayClass36_1.<OnGUI>b__2): abandon and
                    // then clear the selection when it is still the abandoned id.
                    SysMsgBox.Show("确定要放弃任务【" + name + "】吗？", () =>
                    {
                        TaskClient.Abandon(aid);
                        if (selId == aid) selId = 0;
                    });
                }
            }

            TaskDB.Def sel = selId == 0 ? null : DisplayDef(selId);
            if (sel == null)
            {
                if (BitmapFont.Ready)
                    BitmapFont.DrawOutline(winX + s * 228f, winY + s * 200f,
                        "（选择任务查看详情）", GRAY, s);
            }
            else
            {
                Rect detailView = L(218f, 63f, 265f, 305f);
                float h = DetailLayout(sel, false);
                detailScroll = GUI.BeginScrollView(detailView, detailScroll,
                    new Rect(0f, 0f, detailView.width - 18f * s, h * s), GUIStyle.none, GUIStyle.none);
                DetailLayout(sel, true);
                GUI.EndScrollView();
                NativeScroll.BarAt(this, L(470f, 64f, 15f, 303f), detailView.height, h * s,
                    ref detailScroll);
            }

            Rect close = L(469f, 18f, 20f, 19f);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (Click(close, interactionMouse)) visible = false;
            if (Btn(L(419f, 371f, 70f, 19f), icoCancel, "关闭", interactionMouse)) visible = false;

            WinDrag.Handle(this, L(0f, 0f, 495f, 28f), ref winX, ref winY);

            // Swallow clicks inside the window unless a modal box is up. This test uses the
            // pre-Frame interaction mouse while the drag sentinel is installed.
            if (Event.current.type == EventType.MouseDown
                && L(0f, 0f, 495f, 397f).Contains(interactionMouse)
                && !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally
        {
            if (dragged) Event.current.mousePosition = savedMouse;
        }
    }
}

}
