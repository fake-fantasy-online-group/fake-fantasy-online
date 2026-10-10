using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>Official 1.5.8 inspected-equipment owner (06000332..0600033b).</summary>
internal sealed partial class ExEquipDlg : MonoBehaviour
{
    public static ExEquipDlg I;
    private bool open;
    private bool loaded;
    private bool dragging;
    private float winX = -1f;
    private float winY = -1f;
    private Vector2 dragOff;
    private float s;
    private Texture2D bk;
    private Texture2D[] closeBtn;
    private PlayerAvatar doll;
    private int dollTry;
    private int dollVer = -1;

    private const float WW = 257f;
    private const float WH = 506f;
    private static string tgName = string.Empty;
    private static string tgTitle = string.Empty;
    private static int tgLevel, tgRace, tgGender, tgHead, tgHair;
    private static long tgRep;
    private static int[] tgAttrs = new int[0];
    private static int tgMaxAtk, tgMinAtk, tgDef, tgMatk, tgMdef, tgHit;
    private static int tgApBody, tgApCap, tgApBackpack, tgApWeaponR, tgApWeaponL, tgApFace;
    private static readonly List<EquipDlg.EquippedItem> tgEquip = new List<EquipDlg.EquippedItem>();
    private static int dataVer;
    private static readonly string[] RaceNames = { "战士", "剑客", "刺客", "药师", "术士" };

    public static bool IsOpen => I != null && I.open;

    public static void CloseNow()
    {
        if (I != null) I.open = false;
    }

    // WBC-033's paired native publication order. The same list root is cleared before AddRange;
    // a failure preserves every reached store. Neither Show nor CloseNow touches window/doll state.
    public static void Show(string name, int level, int race, int gender, int head, int hair,
        string title, long rep, int apBody, int apCap, int apBackpack, int apWeaponR,
        int apWeaponL, int apFace, int[] attrs, int maxAtk, int minAtk, int def,
        int matk, int mdef, int hit, List<EquipDlg.EquippedItem> equip)
    {
        tgName = name;
        tgLevel = level;
        tgRace = race;
        tgGender = gender;
        tgHead = head;
        tgHair = hair;
        tgTitle = title ?? string.Empty;
        tgRep = rep;
        tgApBody = apBody;
        tgApCap = apCap;
        tgApBackpack = apBackpack;
        tgApWeaponR = apWeaponR;
        tgApWeaponL = apWeaponL;
        tgApFace = apFace;
        tgAttrs = attrs ?? new int[0];
        tgMaxAtk = maxAtk;
        tgMinAtk = minAtk;
        tgDef = def;
        tgMatk = matk;
        tgMdef = mdef;
        tgHit = hit;
        tgEquip.Clear();
        if (equip != null) tgEquip.AddRange(equip);
        dataVer = unchecked(dataVer + 1);
        if (I == null) I = new GameObject("ExEquipDlg").AddComponent<ExEquipDlg>();
        I.open = true;
    }

    private static int At(int idx) =>
        idx >= 0 && tgAttrs != null && idx < tgAttrs.Length ? tgAttrs[idx] : 0;

    private void Awake() => I = this;

    private void Load()
    {
        loaded = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) return;
        bk = loader.LoadGsa2021First("res/uires/equipdlg/equipdlgbk.gsa");
        closeBtn = new Texture2D[4];
        for (int index = 0; index < 4; index++)
        {
            closeBtn[index] = loader.LoadUI("ui/equipdlg05/miniclose_f" + index.ToString() + ".png");
            if (closeBtn[index] != null) closeBtn[index].filterMode = FilterMode.Point;
        }
    }

    public bool MouseOver()
    {
        if (!open || winX < 0f) return false;
        float scale = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        return new Rect(winX, winY, WW * scale, WH * scale).Contains(new Vector2(mx, my));
    }

    private void OnGUI()
    {
        if (!open) return;
        if (!loaded) Load();
        if (bk == null) return;
        GUI.depth = 9;
        s = UIScale.Factor;
        if (winX < 0f)
        {
            winX = 60f * s + (Screen.width - WW * s) * 0.5f;
            winY = (Screen.height - WH * s) * 0.5f;
        }

        Event ev = Event.current;
        // The native display class captures these coordinates before drag mutates the fields.
        // Every L/V in this invocation uses that origin, including the already-built close rect.
        float x = winX;
        float y = winY;
        Rect L(float dx, float dy, float w, float h) => new Rect(x + dx * s, y + dy * s, w * s, h * s);
        void V(float dx, float dy, string text)
        {
            if (BitmapFont.Ready) BitmapFont.DrawOutline(x + dx * s, y + dy * s, text, Color.white, s);
        }
        Rect close = L(230f, 18f, 20f, 19f);
        bool overClose = close.Contains(ev.mousePosition);
        if (ev.type == EventType.MouseDown && L(0f, 0f, WW, WH).Contains(ev.mousePosition) &&
            !L(0f, 0f, WW, 44f).Contains(ev.mousePosition))
            ev.Use();
        if (ev.type == EventType.MouseDown && L(0f, 0f, WW, 44f).Contains(ev.mousePosition) && !overClose)
        {
            dragging = true;
            dragOff = ev.mousePosition - new Vector2(x, y);
            ev.Use();
        }
        if (dragging && ev.type == EventType.MouseDrag)
        {
            winX = ev.mousePosition.x - dragOff.x;
            winY = ev.mousePosition.y - dragOff.y;
            ev.Use();
        }
        if (ev.type == EventType.MouseUp) dragging = false;

        GUI.DrawTexture(L(0f, 0f, WW, WH), bk);
        if (Btn(close, closeBtn, ev))
        {
            open = false;
            PlayerController.SuppressWorldClick();
            return;
        }
        if (BitmapFont.Ready)
        {
            string race = RaceNames[Mathf.Clamp(tgRace, 0, 4)];
            BitmapFont.CenterOutline(L(56f, 22f, 140f, 12f),
                string.Format("等级 {0} {1}:{2}", tgLevel, race, tgName), Color.white, s);
            if (!string.IsNullOrEmpty(tgTitle))
                BitmapFont.CenterOutline(L(27f, 48f, 166f, 12f), tgTitle, new Color32(255, 220, 120, 255), s);
        }
        if (dollVer != dataVer)
        {
            doll = null;
            dollTry = 0;
            dollVer = dataVer;
        }
        if ((doll == null || !doll.ok) && dollTry < 5)
        {
            var appearance = new PlayerAvatar.Appear
            {
                Body = tgApBody, Cap = tgApCap, Backpack = tgApBackpack,
                WeaponR = tgApWeaponR, WeaponL = tgApWeaponL, Face = tgApFace
            };
            doll = PlayerAvatar.Build(tgGender > 0 ? 1 : 0,
                Mathf.Clamp(tgHead, 1, 8), Mathf.Clamp(tgHair, 1, 8), appearance, 100f);
            dollTry++;
        }
        if (doll != null && doll.ok)
        {
            // Reuse WBC-035/036's proven single-Still equivalence: a fixed cached frame,
            // direction 4, with no animation clock and no disposal of the replaced doll.
            Sprite sprite = doll.Still(4);
            if (sprite != null)
            {
                Rect source = sprite.textureRect;
                float w = source.width * s;
                float h = source.height * s;
                float fit = Mathf.Min(1.3f, Mathf.Min(83f * s / w, 96f * s / h));
                // Native retains the first texture for drawing, then reads texture afresh for
                // each UV divisor. A missing texture raises on that read; it does not skip.
                Texture2D texture = sprite.texture;
                float u = source.x / sprite.texture.width;
                float v = source.y / sprite.texture.height;
                float uw = source.width / sprite.texture.width;
                Rect destination = new Rect(x + 131.5f * s - w * fit * 0.5f,
                    y + 192f * s - h * fit, w * fit, h * fit);
                float vh = source.height / sprite.texture.height;
                GUI.DrawTextureWithTexCoords(destination, texture, new Rect(u, v, uw, vh));
            }
        }

        Vector2[] positions = SlotPositions();
        for (int index = 0; index < 11; index++)
        {
            // The native loop inlines this fixed mapping; it has no static slot array.
            int part = SlotPart(index);
            int found = tgEquip.FindIndex(row => row.slot == part || (part == 4 && row.slot == 13));
            if (found < 0) continue;
            EquipDlg.EquippedItem item = tgEquip[found];
            Rect rect = L(positions[index].x, positions[index].y, 30f, 30f);
            if (item.icon != null) ItemIconUI.Draw(rect, item.icon, s);
            else if (BitmapFont.Ready)
                BitmapFont.Draw(rect.x + 3f * s, rect.y + 10f * s,
                    item.name.Substring(0, Mathf.Min(item.name.Length, 2)),
                    item.quality < 1 ? Color.white : (Color)UITip.QualityColor(item.quality), s);
            if (rect.Contains(ev.mousePosition))
                UITip.Show(TooltipContent.Fmt(TooltipContent.QualityName(item.name, item.quality), item.desc), s);
        }
        V(112f, 243f, tgMinAtk.ToString());
        V(112f, 257f, tgMaxAtk.ToString());
        V(92f, 270f, tgMatk.ToString());
        V(171f, 244f, tgDef.ToString());
        V(171f, 257f, tgHit.ToString());
        V(163f, 271f, tgMdef.ToString());
        V(53f, 341f, At(10).ToString());
        V(53f, 358f, At(12).ToString());
        V(53f, 377f, At(21).ToString());
        V(169f, 341f, At(11).ToString());
        V(169f, 358f, At(13).ToString());
        V(169f, 377f, At(14).ToString());
        V(66f, 442f, tgRep.ToString());
    }

    private static int SlotPart(int index) => index switch
    {
        0 => 2, 1 => 6, 2 => 8, 3 => 9, 4 => 4, 5 => 1,
        6 => 3, 7 => 10, 8 => 7, 9 => 5, 10 => 11, _ => -1
    };

    private static Vector2[] SlotPositions() => new[]
    {
        new Vector2(20f, 80f), new Vector2(20f, 120f), new Vector2(20f, 160f),
        new Vector2(20f, 200f), new Vector2(18f, 252f), new Vector2(212f, 80f),
        new Vector2(212f, 120f), new Vector2(212f, 160f), new Vector2(212f, 200f),
        new Vector2(213f, 252f), new Vector2(64f, 403f)
    };

    // WBB-040 whole-body reuse: null/short frames and null Event retain native throw order.
    private bool Btn(Rect r, Texture2D[] fr, Event ev)
    {
        if (fr == null || fr[0] == null) return false;
        int frame = r.Contains(ev.mousePosition) ? (Input.GetMouseButton(0) ? 2 : 1) : 0;
        GUI.DrawTexture(r, fr[frame] ?? fr[0]);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }
}

}
