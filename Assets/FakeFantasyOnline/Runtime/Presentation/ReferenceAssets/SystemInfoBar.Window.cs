using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class SystemInfoBar
    {
        private static readonly int[] FRAMESCHEME = { 0, 0, 1, 1, 2, 3, 3, 3 };
        private bool tried;

        // Official 0x06000e38. A modal does not change the native icon hit area.
        public static bool MouseOver()
        {
            if (I == null) return false;
            var rows = I.Merged();
            if (rows.Count == 0) return false;
            float scale = Screen.height / 600f;
            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            for (int i = 0; i < rows.Count; i++)
                if (IconRect(rows[i], i, scale).Contains(mouse)) return true;
            return false;
        }

        // Official 0x06000e39. Ordinary archive lookup, not the 2021-first variant.
        private void Load()
        {
            tried = true;
            ResLoader loader = ResLoader.I;
            if (loader == null) { tried = false; return; }
            frames[0] = loader.LoadGsaFrame("res/uires/gameui/systeminfobtn.gsa", 0);
            frames[1] = loader.LoadGsaFrame("res/uires/gameui/systeminfobtn.gsa", 1);
            frames[2] = loader.LoadGsaFrame("res/uires/gameui/systeminfobtn.gsa", 2);
            frames[3] = loader.LoadGsaFrame("res/uires/gameui/systeminfobtn.gsa", 3);
        }

        // Official 0x06000e3b. Unity owns the notification GUI, including before world entry.
        private void OnGUI()
        {
            var rows = Merged();
            if (rows.Count == 0) return;
            if (!tried) Load();
            float scale = Screen.height / 600f;
            Vector2 mouse = Event.current.mousePosition;
            GUI.depth = 1;
            int frame = FRAMESCHEME[Mathf.FloorToInt(Time.unscaledTime * 6f) % 8];
            Texture2D texture = frames[frame] ?? frames[0];
            string tip = null;
            for (int i = 0; i < rows.Count; i++)
            {
                Req q = rows[i];
                Rect rect = IconRect(q, i, scale);
                if (texture != null) GUI.DrawTexture(rect, texture);
                else
                {
                    GUI.color = new Color(220f / 255f, 180f / 255f, 60f / 255f, 1f);
                    GUI.DrawTexture(rect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                if (!SysMsgBox.Visible && rect.Contains(mouse))
                    tip = q.text + (q.kind == 4 ? "\n点击打开私聊" : "\n点击处理");
                if (!SysMsgBox.Visible && rect.Contains(mouse) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    Event.current.Use();
                    SoundManager.Play(1002);
                    PlayerController.SuppressWorldClick();
                    if (q.kind == 4) Respond(q, true);
                    else OpenConfirm(q);
                }
            }
            if (tip != null) UITip.Show(tip, scale);
        }

        internal void OpenConfirmForIndependentRuntime(Req q) => OpenConfirm(q);
    }
}
