using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class StallConfigDlg
{
    private static StallConfigDlg I;
    private bool visible;
    private bool tried;
    private byte shoptype;
    private string shopName = "";
    private readonly List<Staged> staged = new List<Staged>();
    private Texture2D bg, beginBtn, cancelBtn, closeIc, helpIc, rowBk, slideBtn;
    private float winX = float.NaN;
    private float winY, s;
    private int _wdGen = -1;
    private Vector2 scroll;
    private GUIStyle nameSt;
    private bool draggingSb;
    private float sbGrabOff;
    private const float NW = 287f, NH = 421f;
    private static readonly Color GOLD;
    private static readonly Color GRAY;
    private static readonly Color YEL;
    private static readonly Dictionary<string, string> _tpl;

    static StallConfigDlg()
    {
        GOLD = new Color(1f, 0.92f, 0.66f, 1f);
        GRAY = new Color(0.62f, 0.6f, 0.52f, 1f);
        YEL = new Color(1f, 1f, 0.35f, 1f);
        _tpl = new Dictionary<string, string>();
        LocalName = "";
    }

    public struct Staged
    {
        public byte bagTab;
        public int bagSlot, itemId, count, quality;
        public long price;
        public string name, desc;
        public Texture2D icon;
    }

    // Official 0x06000d84: reads the instance bool field visible at +0x20 only while the static
    // instance exists. arm64 rva 0x32ea2c and x86_64 rva 0x341240 agree.
    public static bool Active => I != null && I.visible;
    // Official 0x06000d8c: reads the instance byte field shoptype at +0x22 and is true while it is
    // 0, false otherwise, and false when the static instance is absent. arm64 rva 0x32fb58 and
    // x86_64 rva 0x342370 agree.
    public static bool IsSell => I != null && I.shoptype == 0;
    public static void Close() { if (I != null) I.visible = false; }

    public static void AddStaged(byte bagTab, int bagSlot, int itemId, int count, long price,
        string name, Texture2D icon, int quality = 0, string desc = "")
    {
        if (I == null) return;
        I.staged.Add(new Staged
        {
            bagTab = bagTab, bagSlot = bagSlot, itemId = itemId, count = count, price = price,
            name = name, icon = icon, quality = quality, desc = desc
        });
        I.SaveTemplate();
    }

    private string TplKey(int type) => "stall_tpl_" + CharData.name + "_" + type.ToString();

    private void SaveTemplate()
    {
        var text = new StringBuilder();
        foreach (Staged item in staged)
        {
            if (text.Length > 0) text.Append(';');
            text.Append(item.itemId).Append(':').Append(item.price).Append(':').Append(item.count);
        }
        _tpl[TplKey(shoptype)] = text.ToString();
    }
}
}
