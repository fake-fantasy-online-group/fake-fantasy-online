using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered family stash window (familystashdlg) state and lifecycle.
/// </summary>
/// <remarks>
/// Unity invokes the original drawing body in the companion partial file on this lazy owner.
/// Unqualified original identities below retain 1.5.8 provenance; the three adapted operation/input
/// members explicitly cite their independently matched official 1.6.0 identities.
/// </remarks>
public partial class FamilyStashDlg : MonoBehaviour
{
    // Official nested value type FamilyStashDlg+Entry. It is a struct: the official SetData copies
    // 0x38 bytes per element and strides the backing array by the same amount.
    public struct Entry
    {
        public int id;
        public int count;
        public int curDur;
        public int maxDur;
        public int quality;
        public string name;
        public string desc;
        public string by;
        public Texture2D icon;
    }

    // Official private consts (+0x00 in the static table, flags 0x8051). They are inlined at every
    // use site, which is why the frozen bodies carry the literal 470 / 360.
    private const float NW = 470f;
    private const float NH = 360f;

    // Official static +0x00. The lazy root persists across maps; final world teardown purges it.
    public static FamilyStashDlg I;

    // Official static +0x08 / +0x1c / +0x2c / +0x3c, written by the official .cctor (0600035c).
    public static readonly List<Entry> Stash;
    public static readonly Color GOLD;
    public static readonly Color GRAY;
    public static readonly Color WHITE;

    // Official static +0x10 / +0x14 / +0x18 / +0x19. TakePos starts at 2, not 0.
    public static int Cap;
    public static int TakePos;
    public static bool CanTake;
    public static bool IsLeader;

    // Official instance layout. visible(+0x20) is the flag the window tests; winX(+0x34) and
    // _wdGen(+0x3c) are the two fields the official .ctor (0600035b) initialises.
    private bool visible;
    private Vector2 scL;
    private Vector2 scR;
    private float winX;
    private float winY;
    private int _wdGen;
    private float s;

    // Official 0600035c. Creates the stash list, seeds TakePos with 2 and bakes the three
    // read-only palette colours out of the constant blobs at 0x1127390 / 0x11273d0 / 0x1127790.
    static FamilyStashDlg()
    {
        Stash = new List<Entry>();
        TakePos = 2;
        GOLD = new Color(1f, 0.92f, 0.66f, 1f);
        GRAY = new Color(0.66f, 0.64f, 0.55f, 1f);
        WHITE = new Color(0.95f, 0.95f, 0.92f, 1f);
    }

    // Official 0600035b. Only winX and the drag generation get a non-default value; winY is left
    // at 0 and is recentred together with winX on the first NaN check.
    public FamilyStashDlg()
    {
        winX = float.NaN;
        _wdGen = -1;
    }

    // Official 06000351. Lazily hosts the component on a GameObject named "FamilyStashDlg" and
    // keeps it alive across scene loads.
    public static void Ensure()
    {
        if (I != null)
        {
            return;
        }
        GameObject go = new GameObject("FamilyStashDlg");
        I = go.AddComponent<FamilyStashDlg>();
        Object.DontDestroyOnLoad(go);
    }

    // Official 06000352.
    public static bool Visible => I != null && I.visible;

    // Official 1.6.0 0600035a. Opening asks for a fresh snapshot when a NetClient receiver exists.
    public static void Open()
    {
        Ensure();
        I.visible = true;
        NetClient.I?.FamilyStashForIndependentRuntime(0, 0, 0, 0);
    }

    // Official 06000354.
    public static void Close()
    {
        if (I != null)
        {
            I.visible = false;
        }
    }

    // Official 1.6.0 0600035c. Sample the two mouse coordinates independently, and test
    // winX again for the vertical origin. Preserve the ordered, NaN-rejecting bounds.
    public static bool MouseOver()
    {
        if (I == null || !I.visible)
        {
            return false;
        }
        float s = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float left = float.IsNaN(I.winX) ? (Screen.width - NW * s) * 0.5f : I.winX;
        float top = float.IsNaN(I.winX) ? (Screen.height - NH * s) * 0.5f : I.winY;
        return mx >= left && mx < left + NW * s && my >= top && my < top + NH * s;
    }

    // Official 1.6.0 0600035d. Store the scalars and clear the stash before enumerating items.
    // A null input fails here; enumeration retains List version checks and disposal.
    public static void SetData(int cap, int takePos, bool canTake, bool isLeader, List<Entry> items)
    {
        Cap = cap;
        TakePos = takePos;
        CanTake = canTake;
        IsLeader = isLeader;
        Stash.Clear();
        foreach (Entry e in items)
        {
            Entry copy = e;
            copy.icon = NetClient.LoadItemIcon(e.id);
            Stash.Add(copy);
        }
    }

    // Official 06000357. Window-local rect helper: the origin is offset by the window position and
    // every component is scaled by s.
    public Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    // Official 1.6.0 0600035f. A click only counts while no message box is up, and it swallows the
    // event plus the world click. Event.current is read again for each type/button/Use operation.
    public static bool Click(Rect r, Vector2 mp)
    {
        if (!r.Contains(mp))
        {
            return false;
        }
        if (SysMsgBox.Visible)
        {
            return false;
        }
        if (Event.current.type != EventType.MouseDown || Event.current.button != 0)
        {
            return false;
        }
        Event.current.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }

    // Official 1.6.0 06000361. One permission button in the bottom row: width follows the label length
    // (13 px per character plus 12 px of padding), the selected slot takes the brighter plate and
    // GOLD text. Every hit consumes the click; only a different sampled slot with a live
    // NetClient receiver asks the server to move the permission there.
    public void DrawPosBtn(float x, string label, int pos, Vector2 mp)
    {
        float rx = winX + x * s;
        float ry = winY + 326f * s;
        float rw = s * (label.Length * 13 + 12);
        float rh = s * 20f;
        bool sel = TakePos == pos;
        Rect r = new Rect(rx, ry, rw, rh);
        ArtworkSkin.ButtonFace(r, label, s, !SysMsgBox.Visible, sel || r.Contains(mp),
            pressed: r.Contains(mp) && Input.GetMouseButton(0));
        if (Click(r, mp) && !sel)
        {
            NetClient.I?.FamilyStashPermForIndependentRuntime(pos);
        }
    }
}

}
