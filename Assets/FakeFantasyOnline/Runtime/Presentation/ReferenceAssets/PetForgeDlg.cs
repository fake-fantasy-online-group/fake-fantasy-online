using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class PetForgeDlg : MonoBehaviour
{
    public static PetForgeDlg I;
    private bool visible, tried;
    private const float NW = 343f, NH = 470f, CX0 = 34f, CW = 256f,
        GROOVE_X = 295f, GROOVE_W = 15f, TXT_W = 236f;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;
    private Texture2D bg, icoClose;
    private int kind;
    private int petTab = -1;
    private int petSlot;
    private Texture2D petIcon;
    private PetBrief? petBrief;
    private int pet2Tab = -1;
    private int pet2Slot;
    private Texture2D pet2Icon;
    private PetBrief? pet2Brief;
    private bool pickB;
    private int itemTab = -1;
    private int itemSlot, itemId;
    private string itemName = "";
    private Texture2D itemIcon;
    private int selSkill;
    private Vector2 scroll;
    private string petName = "";
    private string petSpecies = "";
    private string tip = "";
    private string confirmText = "";
    private int petLevel, fUsed, fCap, lUsed, lCap, bookSkillId, bookSkillLv;
    private string bookSkillName = "";
    private int petTier, pet2Tier, pet2Level;
    private string pet2Name = "";
    private string pet2Species = "";
    private List<(int, int)> tierOdds = new List<(int, int)>();
    private bool ok;
    private List<(int, string, int, int, int, byte)> skills = new List<(int, string, int, int, int, byte)>();
    private static readonly List<string> _wrapBuf = new List<string>();

    private static void Ensure()
    {
        if (I != null) return;
        var go = new GameObject("PetForgeDlg");
        I = go.AddComponent<PetForgeDlg>();
        DontDestroyOnLoad(go);
    }

    public static bool Visible => I != null && I.visible;
    public static bool Picking => Visible;
    public static void CloseNow() { if (I != null) I.visible = false; }

    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        bg = loader.LoadGsa2021First("res/uires/transportdlg/transportdlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
    }

    public static void OnInfo(int kind, int petTab, int petSlot, int itemTab, int itemSlot,
        int itemId, string petName, string species, int petLevel, bool ok, string tip,
        string confirm, int fu, int fc, int lu, int lc,
        List<(int, string, int, int, int, byte)> skills, int bookSkillId, int bookSkillLv, string bookSkillName)
    {
        if (I == null) return;
        I.kind = kind;
        I.petName = petName ?? "";
        I.petSpecies = species ?? "";
        I.petLevel = petLevel;
        I.ok = ok;
        I.tip = tip ?? "";
        I.confirmText = confirm ?? "";
        I.fUsed = fu;
        I.fCap = fc;
        I.lUsed = lu;
        I.lCap = lc;
        I.skills = skills ?? new List<(int, string, int, int, int, byte)>();
        I.bookSkillId = bookSkillId;
        I.bookSkillLv = bookSkillLv;
        I.bookSkillName = bookSkillName ?? "";
        if (itemId == 0)
        {
            I.itemTab = -1;
            I.itemIcon = null;
            I.itemName = "";
        }
    }

    public static void OnFuseInfo(int petTier, string p2name, string p2species,
        int p2level, int p2tier, List<(int, int)> odds, int reset = 0)
    {
        if (I == null) return;
        I.petTier = petTier;
        I.pet2Name = p2name ?? "";
        I.pet2Species = p2species ?? "";
        I.pet2Level = p2level;
        I.pet2Tier = p2tier;
        I.tierOdds = odds ?? new List<(int, int)>();
        if (reset != 0)
        {
            I.petTab = -1;
            I.petBrief = null;
            I.petIcon = null;
            I.pet2Tab = -1;
            I.pet2Brief = null;
            I.pet2Icon = null;
            I.pickB = false;
            I.selSkill = 0;
            I.tierOdds = new List<(int, int)>();
        }
    }
}

}
