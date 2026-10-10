using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 WorldMapDlg 0x0600108C-0x0600109C (owner) and 0x0600109D-0x060010A1
// (nested layout types MapBtn / MapView / Land / Overview / Layout).
// Members and control flow come from the frozen arm64 + x86_64 GameAssembly exports in
// Build/WBB-016; NW/NH are byte-proven (-800.0 / -600.0 scale literals in OnGUI) and
// CLIENT is the .cctor literal at arm64 0x1127cc0 / x86_64 0x111d4c0 = Rect(96,70,616,474).
public class WorldMapDlg : MonoBehaviour
{
    [System.Serializable]
    private class MapBtn
    {
        public int id;
        public int x;
        public int y;
    }

    [System.Serializable]
    private class MapView
    {
        public int land;
        public int x;
        public int y;
    }

    [System.Serializable]
    private class Land
    {
        public int land;
        public string checkcover;
        public int ccx;
        public int ccy;
        public string adorn;
        public int adx;
        public int ady;
        public MapBtn[] maps;
    }

    [System.Serializable]
    private class Overview
    {
        public MapView[] mapviews;
    }

    [System.Serializable]
    private class Layout
    {
        public Overview overview;
        public Land[] lands;
    }

    public static WorldMapDlg I;

    private const float NW = 800f;
    private const float NH = 600f;
    private static readonly Rect CLIENT = new Rect(96f, 70f, 616f, 474f);

    private bool visible;
    private int view;
    private int viewMapId;
    private int backLand;
    private bool[] textMode;
    private float alpha;
    private Layout lay;
    private Texture2D bk;
    private Texture2D playerIcon;
    private Texture2D markSelf;
    private bool tried;
    private readonly Dictionary<string, Texture2D[]> btnTex;
    private float s;
    private float winX;
    private float winY;

    // 0x0600108F get_Visible: the whole-window visibility interception consumed by
    // GameHud.MouseOverHud (0x06000417, first hit) and UiEsc.CloseTop (0x06000FCA).
    public static bool Visible
    {
        get { return I != null && I.visible; }
    }

    // 0x0600108C Ensure.
    private static void Ensure()
    {
        if (I != null) return;
        GameObject owner = new GameObject("WorldMapDlg");
        I = owner.AddComponent<WorldMapDlg>();
        DontDestroyOnLoad(owner);
    }

    // 0x0600108D Toggle.
    public static void Toggle()
    {
        Ensure();
        if (!I.visible)
        {
            I.Open();
            return;
        }
        I.visible = false;
    }

    // 0x0600108E Hide.
    public static void Hide()
    {
        if (I != null) I.visible = false;
    }

    // 0x06001090 Open.
    private void Open()
    {
        visible = true;
        alpha = 0f;
        view = 0;
    }

    // 0x06001091 Load.
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null)
        {
            tried = false;
            return;
        }
        string text = GameTables.LoadText("worldmap_layout");
        if (text != null) lay = JsonUtility.FromJson<Layout>(text);
        bk = loader.LoadGsaFrameA("res/uires/sketchmap/worldbar.gsa", 0);
        playerIcon = loader.LoadGsaFrameA("res/uires/gameui/playericon.gsa", 0);
        markSelf = loader.LoadGsa("res/buddy/sketch_wanjia.gsa");
    }

    // 0x06001092 Frames.
    private Texture2D[] Frames(string path)
    {
        Texture2D[] cached;
        if (btnTex.TryGetValue(path, out cached)) return cached;
        ResLoader loader = ResLoader.I;
        int count = loader.GsaFrameCount(path);
        Texture2D[] frames = new Texture2D[count < 1 ? 1 : count];
        if (count < 1)
        {
            frames[0] = null;
        }
        else
        {
            for (int i = 0; i < count; i++) frames[i] = loader.LoadGsaFrameA(path, i);
        }
        btnTex[path] = frames;
        return frames;
    }

    // 0x06001093 Pick.
    private static Texture2D Pick(Texture2D[] f, bool hover, bool enabled)
    {
        if (f.Length > 3)
        {
            int i = 1;
            if (!hover) i = 2;
            if (!enabled) i = 3;
            return f[i];
        }
        if (f.Length > 1) return f[hover && enabled ? 0 : 1];
        return f[0];
    }

    // 0x06001094 CurMapId.
    private int CurMapId()
    {
        int id = -1;
        string name = GameBootstrap.CurMapName;
        if (name != null && name.Length > 2 && int.TryParse(name.Substring(2), out id)) return id;
        return -1;
    }

    // 0x06001095 CurLand.
    private int CurLand(int mapId)
    {
        if (mapId >= 0 && lay != null)
        {
            Land[] lands = lay.lands;
            for (int i = 0; i < lands.Length; i++)
            {
                Land land = lands[i];
                MapBtn[] maps = land.maps;
                for (int j = 0; j < maps.Length; j++)
                {
                    if (maps[j].id == mapId) return land.land;
                }
            }
        }
        return 0;
    }

    // 0x06001096 L.
    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    // 0x06001097 ImgBtn.
    private bool ImgBtn(float x, float y, string path, bool enabled = true)
    {
        Texture2D[] f = Frames(path);
        if (f[0] == null) return false;
        float w = GsaDecoder.W(f[0]);
        float h = GsaDecoder.H(f[0]);
        Rect r = L(x, y, w, h);
        bool hover = false;
        if (enabled) hover = r.Contains(Event.current.mousePosition);
        Texture2D tex = Pick(f, hover, enabled);
        if (tex != null) GUI.DrawTexture(r, tex);
        if (enabled) return GUI.Button(r, GUIContent.none, GUIStyle.none);
        return false;
    }

    // 0x06001098 AlphaAt.
    private float AlphaAt(Texture2D t, Rect r, Vector2 m)
    {
        if (t != null && r.Contains(m))
        {
            int px = Mathf.Clamp((int)((m.x - r.x) / r.width * t.width), 0, t.width - 1);
            int py = Mathf.Clamp((int)((m.y - r.y) / r.height * t.height), 0, t.height - 1);
            try
            {
                return t.GetPixel(px, t.height - 1 - py).a;
            }
            catch
            {
                return 1f;
            }
        }
        return 0f;
    }

    // 0x06001099 PickPuzzle.
    private int PickPuzzle(List<(int id, Rect rect, Texture2D tex)> items, Vector2 m)
    {
        int best = -1;
        float bestAlpha = 0.1f;
        foreach (var item in items)
        {
            float a = AlphaAt(item.tex, item.rect, m);
            if (bestAlpha < a)
            {
                best = item.id;
                bestAlpha = a;
            }
        }
        return best;
    }

    // 0x0600109B .ctor.
    public WorldMapDlg()
    {
        backLand = 1;
        textMode = new bool[5];
        btnTex = new Dictionary<string, Texture2D[]>();
    }

    // Inlined in the frozen OnGUI body wherever the drawn item is the player's current
    // map: playerIcon at (winX + (W * 0.5 + x - 22) * s, winY + (H * 0.5 + y - 27) * s).
    private void DrawSelf(MapView item, Texture2D[] frames, int curLand)
    {
        if (item.land != curLand || playerIcon == null) return;
        int x = item.x;
        float width = GsaDecoder.W(frames[0]);
        int y = item.y;
        float height = GsaDecoder.H(frames[0]);
        float selfWidth = GsaDecoder.W(playerIcon);
        float selfHeight = GsaDecoder.H(playerIcon);
        GUI.DrawTexture(L(width * 0.5f + x - 22f, height * 0.5f + y - 27f,
            selfWidth, selfHeight), playerIcon);
    }

    private void DrawSelf(MapBtn item, Texture2D[] frames, int curMapId)
    {
        if (item.id != curMapId || playerIcon == null) return;
        int x = item.x;
        float width = GsaDecoder.W(frames[0]);
        int y = item.y;
        float height = GsaDecoder.H(frames[0]);
        float selfWidth = GsaDecoder.W(playerIcon);
        float selfHeight = GsaDecoder.H(playerIcon);
        GUI.DrawTexture(L(width * 0.5f + x - 22f, height * 0.5f + y - 27f,
            selfWidth, selfHeight), playerIcon);
    }

    // 0x0600109A OnGUI.
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        if (lay == null) return;

        s = UIScale.Factor;
        winX = Mathf.Round((Screen.width - NW * s) * 0.5f);
        winY = Mathf.Round((Screen.height - NH * s) * 0.5f);
        GUI.depth = 3;
        if (Event.current.type == EventType.Repaint) alpha = Mathf.Min(alpha + 0.05f, 1f);
        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        Rect window = new Rect(winX, winY, NW * s, NH * s);
        Event ev = Event.current;

        if (ev.type == EventType.MouseDown)
        {
            if (!window.Contains(ev.mousePosition))
            {
                if (ev.button == 0) visible = false;
                ev.Use();
                GUI.color = prev;
                return;
            }
        }
        if (ev.type == EventType.MouseDown && ev.button == 1 && window.Contains(ev.mousePosition))
        {
            if (view == 5) view = backLand;
            else if (view > 0) view = 0;
            ev.Use();
        }

        if (bk != null) GUI.DrawTexture(L(0f, 0f, GsaDecoder.W(bk), GsaDecoder.H(bk)), bk);

        int curMapId = CurMapId();
        int curLand = CurLand(curMapId);

        if (view == 0)
        {
            List<(int id, Rect rect, Texture2D tex)> items = new List<(int id, Rect rect, Texture2D tex)>();
            MapView[] views = lay.overview.mapviews;
            for (int i = 0; i < views.Length; i++)
            {
                MapView mv = views[i];
                Texture2D[] frames = Frames(MapViewPath(mv.land));
                if (frames[0] == null) continue;
                items.Add((mv.land,
                    L(mv.x, mv.y, GsaDecoder.W(frames[0]), GsaDecoder.H(frames[0])),
                    frames.Length >= 2 ? frames[1] : frames[0]));
            }

            int hover = PickPuzzle(items, ev.mousePosition);
            views = lay.overview.mapviews;
            for (int i = 0; i < views.Length; i++)
            {
                MapView mv = views[i];
                if (mv.land == hover) continue;
                Texture2D[] frames = Frames(MapViewPath(mv.land));
                if (frames[0] == null) continue;
                Rect r = L(mv.x, mv.y, GsaDecoder.W(frames[0]), GsaDecoder.H(frames[0]));
                GUI.DrawTexture(r, Pick(frames, false, true));
                DrawSelf(mv, frames, curLand);
            }
            if (hover > -1)
            {
                views = lay.overview.mapviews;
                for (int i = 0; i < views.Length; i++)
                {
                    MapView mv = views[i];
                    if (mv.land != hover) continue;
                    Texture2D[] frames = Frames(MapViewPath(mv.land));
                    if (frames[0] != null)
                    {
                        Rect r = L(mv.x, mv.y, GsaDecoder.W(frames[0]), GsaDecoder.H(frames[0]));
                        GUI.DrawTexture(r, Pick(frames, true, true));
                        DrawSelf(mv, frames, curLand);
                    }
                    break;
                }
            }

            if (ev.type == EventType.MouseDown && ev.button == 0 && hover > 0)
            {
                view = hover;
                ev.Use();
            }
            ImgBtn(392f, 20f, "res/uires/sketchmap/homebtn.gsa", false);
            ImgBtn(502f, 20f, "res/uires/sketchmap/textbtn.gsa", false);
        }
        else if (view < 1 || view >= 5)
        {
            Texture2D map = ResLoader.I.LoadGsa("res/sketchmap/qw" + viewMapId.ToString("D4") + ".gsa");
            if (map != null)
            {
                Rect client = L(CLIENT.x, CLIENT.y, CLIENT.width, CLIENT.height);
                GUI.BeginGroup(client);
                float mw = GsaDecoder.W(map) * s;
                float mh = GsaDecoder.H(map) * s;
                float mx = Mathf.Round((client.width - mw) * 0.5f);
                float my = Mathf.Round((client.height - mh) * 0.5f);
                GUI.DrawTexture(new Rect(mx, my, mw, mh), map);
                // Official OnGUI (0600109a) guards the self marker on markSelf != null and on
                // GameHud.I (GameHud static slot +0x18, arm64 lines 746-768) before it calls
                // GameHud.DiagramFrac (06000426). The I null test is the official one; it is not
                // replaced by the existence of any other object.
                float fx;
                float fy;
                if (viewMapId == curMapId && markSelf != null && GameHud.I != null &&
                    GameHud.I.DiagramFrac(out fx, out fy))
                {
                    float sw = GsaDecoder.W(markSelf) * s;
                    float sh = GsaDecoder.H(markSelf) * s;
                    GUI.DrawTexture(new Rect(
                        Mathf.Round(mx + mw * fx - sw * 0.5f),
                        Mathf.Round(my + mh * fy - sh * 0.5f),
                        Mathf.Round(sw), Mathf.Round(sh)), markSelf);
                }
                GUI.EndGroup();
            }
            if (ImgBtn(392f, 20f, "res/uires/sketchmap/homebtn.gsa")) view = backLand;
            ImgBtn(502f, 20f, "res/uires/sketchmap/textbtn.gsa", false);
        }
        else
        {
            Land land = lay.lands[view - 1];
            // Official OnGUI draws land.adorn (Land+0x28) unconditionally at land.adx/ady before it
            // reads textMode[view]; the text/checkcover overlay only comes later in the same branch.
            Texture2D adorn = ResLoader.I.LoadGsaFrameA(land.adorn, 0);
            if (adorn != null)
            {
                GUI.DrawTexture(L(land.adx, land.ady, GsaDecoder.W(adorn), GsaDecoder.H(adorn)), adorn);
            }
            bool text = textMode[view];
            List<(int id, Rect rect, Texture2D tex)> items = new List<(int id, Rect rect, Texture2D tex)>();
            MapBtn[] maps = land.maps;
            for (int i = 0; i < maps.Length; i++)
            {
                MapBtn mb = maps[i];
                Texture2D[] frames = Frames(MapBtnPath(mb.id));
                if (frames[0] == null) continue;
                items.Add((mb.id,
                    L(mb.x, mb.y, GsaDecoder.W(frames[0]), GsaDecoder.H(frames[0])),
                    frames.Length >= 2 ? frames[1] : frames[0]));
            }

            int hover = text ? -1 : PickPuzzle(items, ev.mousePosition);
            maps = land.maps;
            for (int i = 0; i < maps.Length; i++)
            {
                MapBtn mb = maps[i];
                if (mb.id == hover) continue;
                Texture2D[] frames = Frames(MapBtnPath(mb.id));
                if (frames[0] == null) continue;
                Rect r = L(mb.x, mb.y, GsaDecoder.W(frames[0]), GsaDecoder.H(frames[0]));
                GUI.DrawTexture(r, Pick(frames, false, !text));
                DrawSelf(mb, frames, curMapId);
            }
            if (hover > -1)
            {
                maps = land.maps;
                for (int i = 0; i < maps.Length; i++)
                {
                    MapBtn mb = maps[i];
                    if (mb.id != hover) continue;
                    Texture2D[] frames = Frames(MapBtnPath(mb.id));
                    if (frames[0] != null)
                    {
                        Rect r = L(mb.x, mb.y, GsaDecoder.W(frames[0]), GsaDecoder.H(frames[0]));
                        GUI.DrawTexture(r, Pick(frames, true, !text));
                        DrawSelf(mb, frames, curMapId);
                    }
                    break;
                }
            }

            if (ev.type == EventType.MouseDown && ev.button == 0 && hover > -1)
            {
                viewMapId = hover;
                backLand = view;
                view = 5;
                ev.Use();
            }

            if (text)
            {
                Texture2D cover = ResLoader.I.LoadGsaFrameA(land.checkcover, 0);
                if (cover != null)
                {
                    GUI.DrawTexture(L(land.ccx, land.ccy, GsaDecoder.W(cover), GsaDecoder.H(cover)), cover);
                }
            }

            if (ImgBtn(392f, 20f, "res/uires/sketchmap/homebtn.gsa")) view = 0;
            if (ImgBtn(502f, 20f, "res/uires/sketchmap/textbtn.gsa")) textMode[view] = !text;
        }

        if (ImgBtn(580f, 20f, "res/uires/common/closebtn.gsa")) visible = false;
        GUI.color = prev;
        if (ev.type == EventType.MouseDown && window.Contains(ev.mousePosition)) ev.Use();
    }

    private static string MapViewPath(int land)
    {
        return "res/uires/sketchmap/mapview" + land + ".gsa";
    }

    private static string MapBtnPath(int id)
    {
        return "res/uires/sketchmap/mapbtn" + id + ".gsa";
    }

}
}
