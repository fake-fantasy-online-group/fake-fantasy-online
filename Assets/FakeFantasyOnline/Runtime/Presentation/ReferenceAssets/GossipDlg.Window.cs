using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal sealed partial class GossipDlg
    {
        // Official 0x060005e1. Keep the original pre-layout WinZ registration order.
        private void OnGUI()
        {
            if (!visible) return;
            if (!tried) Load();
            (int depth, bool covered) = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
            Vector2 saved = Event.current.mousePosition;
            if (covered) Event.current.mousePosition = new Vector2(-9999f, -9999f);
            try
            {
                s = UIScale.Factor;
                if (WinDrag.Stale(winX, ref _wdGen))
                {
                    winX = (Screen.width - NW * s) * 0.5f;
                    winY = (Screen.height - NH * s) * 0.5f;
                }
                Vector2 mouse = Event.current.mousePosition;
                GUI.depth = depth;
                if (minimized) { DrawBar(mouse); return; }
                float height = showHistory ? NH_HIST : NH;
                if (bg != null) GUI.DrawTexture(L(0f, 0f, NW, NH), bg);
                else PopUI.Panel(L(0f, 0f, NW, NH));
                Rect title = L(46f, 22f, 150f, 12f);
                if (BitmapFont.Ready) BitmapFont.CenterOutline(title, peer, new Color(1f, 0.92f, 0.66f, 1f), s);
                if (peer.Length > 0 && Event.current.type == EventType.MouseDown && Event.current.button == 1 && title.Contains(mouse))
                {
                    PlayerActionMenu.Open(CombatWorld.FindPlayerIdByName(peer), peer);
                    Event.current.Use();
                }
                DrawSessionTabs(mouse);
                DrawMsgList(Cur, L(15f, 91f, 194f, 111f), L(212f, 110f, 15f, 72f), mouse);
                Rect input = L(15f, 216f, 214f, 58f);
                GUI.color = new Color(0f, 0f, 0f, 0.28f);
                GUI.DrawTexture(input, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.SetNextControlName("gossip_in");
                string oldText = msg ?? "";
                string edited = GUI.TextArea(new Rect(input.x + 3f, input.y + 2f, input.width - 6f, input.height - 4f), oldText, E());
                msg = ChatUI.GuardShareEdit(oldText, edited, pendShr);
                if (icoVoice != null)
                {
                    Rect voice = L(7f, 6f, 26f, 26f);
                    GUI.DrawTexture(voice, icoVoice);
                    if (PopUI.Click(voice, mouse)) ChatUI.Add("系统", "语音功能未开放", new Color(1f, 200f / 255f, 120f / 255f, 1f));
                }
                if (PopUI.Btn(L(11f, 278f, 58f, 22f), icoRecord, "记录", mouse, s)) showHistory = !showHistory;
                if (PopUI.Btn(L(88f, 278f, 58f, 22f), icoFace, "表情", mouse, s)) showFace = !showFace;
                if (PopUI.Btn(L(163f, 278f, 70f, 22f), icoSend, "发送", mouse, s)) Send();
                if (showHistory)
                {
                    Rect history = L(4f, 305f, 233f, 96f);
                    PopUI.Panel(history);
                    DrawLines(Cur, new Rect(history.x + 6f * s, history.y + 6f * s, history.width - 28f * s, history.height - 12f * s));
                }
                Rect close = L(212f, 17f, 20f, 19f);
                if (icoClose != null) GUI.DrawTexture(close, icoClose);
                if (PopUI.Click(close, mouse)) { visible = false; showHistory = false; }
                Rect minimize = L(192f, 17f, 20f, 19f);
                if (icoMin != null) GUI.DrawTexture(minimize, icoMin);
                if (PopUI.Click(minimize, mouse)) minimized = true;
                if (showFace)
                {
                    int emote = EmotePicker.Draw(FacePanel(), Event.current, s);
                    if (emote > 0)
                    {
                        msg = msg + "\\e" + emote.ToString("D2");
                        showFace = false;
                        GUI.FocusControl("gossip_in");
                    }
                }
                WinDrag.Handle(this, L(0f, 0f, NW, 36f), ref winX, ref winY);
                if (Event.current.type == EventType.MouseDown &&
                    (L(0f, 0f, NW, height).Contains(mouse) || (showFace && FacePanel().Contains(mouse)))) Event.current.Use();
            }
            finally
            {
                if (covered) Event.current.mousePosition = saved;
            }
        }
    }
}
