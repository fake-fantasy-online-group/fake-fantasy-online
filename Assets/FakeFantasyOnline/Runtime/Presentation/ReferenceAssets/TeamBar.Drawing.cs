using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal sealed partial class TeamBar
    {
        // Official 0x06000e9e.
        private void OnGUI()
        {
            var mates = Mates();
            int[] indices = mateIdx.ToArray();
            slideX = Mathf.MoveTowards(slideX, mates.Count > 0 ? 0f : -SLOT_W, Time.unscaledDeltaTime * SLOT_W * 4f);
            if (slideX <= -43.5f) return;
            if (!tried) Load();
            float scale = Screen.height / 600f;
            Vector2 mouse = Event.current.mousePosition;
            GUI.depth = 2;
            for (int i = 0; i < mates.Count; i++)
            {
                var mate = mates[i];
                float x = slideX * scale;
                float y = (BASE_Y + i * SLOT_H) * scale;
                Rect rect = new Rect(x, y, SLOT_W * scale, SLOT_H * scale);
                bool online = mate.Item9;
                if (!online) GUI.color = new Color(0.45f, 0.45f, 0.45f, 0.9f);
                Texture2D head = Head(mate.Item6, mate.Item7, mate.Item8);
                if (head != null)
                    GUI.DrawTexture(new Rect(x + 5f * scale, y + 7f * scale, 22f * scale, 22f * scale), head);
                if (slotBg != null) GUI.DrawTexture(rect, slotBg);
                else
                {
                    GUI.color = new Color(0.08f, 0.07f, 0.11f, 0.7f);
                    GUI.DrawTexture(rect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    if (head == null && BitmapFont.Ready)
                        BitmapFont.DrawOutline(x + 3f * scale, y + 2f * scale, mate.Item2, new Color(0.95f, 0.95f, 0.88f, 1f), scale);
                }
                GUI.color = Color.white;
                if (BitmapFont.Ready && i < indices.Length)
                {
                    string profession = SocialClient.PartyProfName(indices[i]);
                    if (profession.Length > 0)
                        BitmapFont.DrawOutline(x + scale, y + 20f * scale, profession.Substring(0, 1),
                            online ? new Color(1f, 0.92f, 0.62f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f), scale);
                }
                float hp = mate.Item5 > 0 ? Mathf.Clamp01((float)mate.Item4 / mate.Item5) : 0f;
                if (hpFill != null && hp > 0f)
                {
                    if (!online) GUI.color = new Color(0.45f, 0.45f, 0.45f, 0.9f);
                    GUI.DrawTextureWithTexCoords(new Rect(x + 20f * scale, y, (24f * hp) * scale, SLOT_H * scale),
                        hpFill, new Rect(0f, 0f, hp, 1f));
                    if (!online) GUI.color = Color.white;
                }
                if (rect.Contains(mouse))
                {
                    string tip = mate.Item2;
                    if (!online) tip += "  不在线";
                    else if (mate.Item5 <= 0) tip += "  不在该线";
                    else tip += string.Format("  气血 {0}/{1}", mate.Item4, mate.Item5);
                    if (mate.Item3 > 0) tip += "\n等级 " + mate.Item3;
                    if (i < indices.Length)
                    {
                        string profession = SocialClient.PartyProfName(indices[i]);
                        if (profession.Length > 0) tip += "  " + profession;
                    }
                    UITip.Show(tip, scale);
                }
                if (rect.Contains(mouse) && Event.current.type == EventType.MouseDown)
                {
                    if (Event.current.button == 0)
                    {
                        Event.current.Use();
                        if (mate.Item1 == NetClient.SelfEntityId) SocialClient.SelectedMemberId = 0;
                        else
                        {
                            SocialClient.SelectedMemberId = mate.Item1;
                            if (!CombatWorld.SelectPlayer(mate.Item1)) TargetSelect.Clear();
                        }
                    }
                    else if (Event.current.button == 1)
                    {
                        Event.current.Use();
                        PlayerActionMenu.Open(mate.Item1, mate.Item2);
                    }
                }
            }
        }
    }
}
