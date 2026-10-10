using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerController
{
    public static bool WorkTipOpen;
    private static int workTipKind;
    private static Vector2 workTipPos;
    private static int workTipOpenFrame;
    private static Texture2D workMenuBtn;
    private static bool workMenuBtnTried;
    public List<MapLink.GatherZone> gatherZones;
    public static void CloseWorkTip() { WorkTipOpen = false; workTipKind = 0; }

    internal int GatherKindForIndependentRuntime(float px, float py, bool selectableAllowsGather)
    {
        int kind = 0;
        if (!selectableAllowsGather) return kind;
        if (maskBytes != null && maskW > 0)
        {
            int x = unchecked((int)(px / (float)maskCW));
            int y = unchecked((int)(py / (float)maskCH));
            if (x >= 0 && y >= 0 && x < maskW && y < maskH)
            {
                byte flags = maskBytes[unchecked(maskOff + y * maskW + x)];
                if ((flags & 4) != 0) kind = 2;
                else if ((flags & 2) != 0) kind = 1;
            }
        }
        if (kind == 0 && gatherZones != null)
            foreach (MapLink.GatherZone zone in gatherZones)
                if (MapLink.PointInPoly(new Vector2(px, py), zone.poly)) { kind = zone.kind == 2 ? 2 : 1; break; }
        return kind;
    }
    internal void OpenWorkTipForIndependentRuntime(int kind)
    {
        WorkTipOpen = true;
        workTipKind = kind;
        workTipOpenFrame = Time.frameCount;
        workTipPos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
    }
    private void OnGUI()
    {
        if (!WorkTipOpen) return;
        float s = UIScale.Factor;
        float w = 98f * s;
        float x = Mathf.Clamp(workTipPos.x, 0f, Screen.width - w);
        float h = 26f * s;
        float y = Mathf.Clamp(workTipPos.y, 0f, Screen.height - h);
        Rect outer = new Rect(x, y, w, h);
        Event e = Event.current;
        Color saved = GUI.color;
        GUI.color = (Color)new Color32(26, 27, 22, 196);
        GUI.DrawTexture(outer, Texture2D.whiteTexture);
        GUI.color = new Color(0.55f, 0.52f, 0.42f, 0.8f);
        GUI.DrawTexture(new Rect(x, y, w, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + h - 1f, w, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, 1f, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + w - 1f, y, 1f, h), Texture2D.whiteTexture);
        GUI.color = saved;
        Rect button = new Rect(x + 4f * s, y + 2f * s, 90f * s, 22f * s);
        bool hover = button.Contains(e.mousePosition);
        if (!workMenuBtnTried)
        {
            workMenuBtnTried = true;
            if (ResLoader.I != null) workMenuBtn = ResLoader.I.LoadGsa("res/uires/gameui/menubtn.gsa");
        }
        if (workMenuBtn != null)
        {
            GUI.color = new Color(1f, 1f, 1f, hover ? 1f : 0.85f);
            GUI.DrawTexture(button, workMenuBtn);
        }
        else
        {
            GUI.color = hover ? new Color(0.42f, 0.4f, 0.3f, 0.95f) : new Color(0.28f, 0.27f, 0.2f, 0.95f);
            GUI.DrawTexture(button, Texture2D.whiteTexture);
        }
        GUI.color = saved;
        if (BitmapFont.Ready) BitmapFont.CenterOutline(new Rect(button.x, button.y + 5f * s, button.width, 12f * s), workTipKind == 2 ? "挖 矿" : "钓 鱼", Color.white, s);
        if (e.type != EventType.MouseDown || Time.frameCount == workTipOpenFrame) return;
        if (e.button == 0 && button.Contains(e.mousePosition))
        { int skill = workTipKind == 2 ? 11007 : 11006; CloseWorkTip(); StartWork(skill); }
        else CloseWorkTip();
        e.Use();
    }
}
}
