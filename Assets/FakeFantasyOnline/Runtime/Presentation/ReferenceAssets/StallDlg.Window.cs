using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    /// <summary>
    /// WBC-038 lane C: official <c>StallDlg</c> window state, publish and close lifecycle.
    /// Covers 06000db3 get_Visible, 06000db4 Ensure, 06000db5 Load, 06000db6 SetContents,
    /// 06000db7 CloseIf, 06000db8 CloseNow, 06000dbe .ctor and 06000dbf .cctor, read on both frozen
    /// architectures (Build/ReferenceReverseGhidra-1.5.8/queries/wbc038-arm64-StallDlg and
    /// wbc038-x86_64-StallDlg).
    /// </summary>
    /// <remarks>
    /// This is the same official type the existing Row/Owners/MoneyWords partials already carry; no
    /// second stall window is introduced. The instance field order below is the official layout
    /// (visible 32, tried 33, ownerId 36, ownerName 40, shopName 48, type 56, rows 64, bg 72,
    /// closeIc 80, itemBg 88, goldIc 96, silverIc 104, copperIc 112, winX 120, winY 124, s 128,
    /// scroll 132, _wdGen 140).
    /// </remarks>
    public partial class StallDlg
    {
        // Official StallDlg.NW / NH are private const float (field flags 0x8051). Neither constructor
        // stores them; the arm64 06000dbc export sizes the frame as WinZ.Frame(x, y, s * 569.0,
        // s * 426.0, this, 10, ...), so NW = 569f and NH = 426f.
        private const float NW = 569f;
        private const float NH = 426f;

        private static StallDlg I;

        // 06000dbf .cctor: the static block writes Owners at +0x00 (StallDlg.Owners.cs) and the three
        // 16-byte Color literals at +0x10, +0x20 and +0x30 in that order. Both architectures read the
        // same literals: GOLD (1f, 0.9f, 0.6f, 1f), GRAY (0.62f, 0.6f, 0.52f, 1f) and
        // YEL (1f, 1f, 0.35f, 1f).
        private static readonly Color GOLD = new Color(1f, 0.9f, 0.6f, 1f);
        private static readonly Color GRAY = new Color(0.62f, 0.6f, 0.52f, 1f);
        private static readonly Color YEL = new Color(1f, 1f, 0.35f, 1f);

        // 06000dbe .ctor: ownerName and shopName take the empty string literal, rows is a fresh
        // List<Row>, winX is float.NaN (0x7fc00000) and _wdGen is -1; every other instance field keeps
        // its CLR default. The base MonoBehaviour ctor is the last emitted call on both architectures.
        private bool visible;
        private bool tried;
        private int ownerId;
        private string ownerName = "";
        private string shopName = "";
        private byte type;
        private readonly List<Row> rows = new List<Row>();
        private Texture2D bg;
        private Texture2D closeIc;
        private Texture2D itemBg;
        private Texture2D goldIc;
        private Texture2D silverIc;
        private Texture2D copperIc;
        private float winX = float.NaN;
        private float winY;
        private float s;
        private Vector2 scroll;
        private int _wdGen = -1;

        // 06000db3: on both architectures the body is the class-init guard, the static I read,
        // Object.op_Inequality(I, null) and then I.visible at +0x20; a null I returns 0 and a null
        // instance after a successful inequality test throws instead of falling through.
        public static bool Visible => I != null && I.visible;

        // 06000db4: returns immediately when I is already set, then builds the GameObject named
        // "StallDlg" (string literal 13420), adds the StallDlg component into static I and keeps the
        // GameObject across scene loads. The Ensure call inside SetContents is the only official
        // producer of I.
        private static void Ensure()
        {
            if (I != null) return;
            GameObject go = new GameObject("StallDlg");
            I = go.AddComponent<StallDlg>();
            DontDestroyOnLoad(go);
        }

        // 06000db5: tried is set before the guard and cleared again when ResLoader.I is missing, so a
        // failed attempt leaves the window eligible for another Load. The six textures are loaded in
        // the order bg, closeIc, itemBg, goldIc, silverIc, copperIc (offsets 0x48, 0x50, 0x58, 0x60,
        // 0x68, 0x70) from these original package paths on both architectures.
        private void Load()
        {
            tried = true;
            if (ResLoader.I == null)
            {
                tried = false;
                return;
            }
            bg = ResLoader.I.LoadGsa("res/uires/tradedlg/tradedlgbk.gsa");
            closeIc = ResLoader.I.LoadGsa("res/uires/common/miniclose.gsa");
            itemBg = ResLoader.I.LoadGsa("res/uires/tradedlg/tradeiteml.gsa");
            goldIc = ResLoader.I.LoadGsa("res/uires/common/gold.gsa");
            silverIc = ResLoader.I.LoadGsa("res/uires/common/silver.gsa");
            copperIc = ResLoader.I.LoadGsa("res/uires/common/copper.gsa");
        }

        /// <summary>
        /// 06000db6: publishes the stall contents window for one owner.
        /// </summary>
        /// <remarks>
        /// The body is Ensure, then a first-visit Load guarded by <c>tried</c>, then the four scalar
        /// fields, then rows.Clear() followed by rows.AddRange(items), then visible and finally
        /// scroll = Vector2.zero. The rows list is the readonly list the .ctor allocated, so the
        /// reference is never replaced. A null singleton after Ensure still throws rather than
        /// returning silently.
        /// The two frozen architectures emit complementary store orders for the scalar pairs -
        /// arm64 writes ownerName before ownerId and shopName before type, x86_64 writes ownerId
        /// before ownerName and type before shopName - so the binary does not pin a source order for
        /// those four assignments; no other member can observe it because nothing reads a field
        /// between them.
        /// </remarks>
        public static void SetContents(int oid, string oname, byte t, string sname, List<Row> items)
        {
            Ensure();
            if (!I.tried) I.Load();
            I.ownerId = oid;
            I.ownerName = oname;
            I.type = t;
            I.shopName = sname;
            I.rows.Clear();
            I.rows.AddRange(items);
            I.visible = true;
            I.scroll = Vector2.zero;
        }

        // 06000db7: only the window owned by oid closes; any other owner id leaves it open.
        public static void CloseIf(int oid)
        {
            if (I == null || I.ownerId != oid) return;
            I.visible = false;
        }

        // 06000db8: unconditional hide of the singleton, used by UiEsc.CloseTop.
        public static void CloseNow()
        {
            if (I != null) I.visible = false;
        }
    }
}
