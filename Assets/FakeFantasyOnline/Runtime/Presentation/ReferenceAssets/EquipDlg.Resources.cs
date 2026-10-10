using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipDlg
{
    // Original instance resource slots +0x50..+0x128, in native declaration order.
    // Arrays belong to this window; resource/decoder texture lifetimes are unchanged.
    private Texture2D bk;
    private Texture2D vtab0;
    private Texture2D vtab1;
    private Texture2D vtabBlue;
    private Texture2D attrPage;
    private Texture2D titleLine;
    private Texture2D itemSel;
    private Texture2D pUL;
    private Texture2D pUM;
    private Texture2D pUR;
    private Texture2D pLM;
    private Texture2D pRM;
    private Texture2D pDL;
    private Texture2D pDM;
    private Texture2D pDR;
    private Texture2D[] checkbox;
    private Texture2D[] radioBtn;
    private Texture2D[] okBtn;
    private Texture2D[] delBtn;
    private Texture2D[] titleBtn;
    private Texture2D[] assignBtn;
    private Texture2D[] affirmBtn;
    private Texture2D[] restoreBtn;
    private Texture2D[] plusBtn;
    private Texture2D[] minusBtn;
    private Texture2D[] closeBtn;
    private Texture2D[] helpBtn;
    private Texture2D[] pointFire;

    // Official 0600030a. Publication is incremental: no rollback, retry or helper-return
    // continuation. Reuse the captured native loader throughout this one load attempt.
    private void Load()
    {
        loaded = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) return;

        bk = loader.LoadGsa2021First("res/uires/equipdlg/equipdlgbk.gsa");
        attrPage = loader.LoadGsa2021First("res/uires/equipdlg/secattrbk.gsa");
        vtab0 = loader.LoadUI("ui/equipdlg05/vtab_f0.png");
        vtab1 = loader.LoadUI("ui/equipdlg05/vtab_f1.png");
        vtabBlue = loader.LoadUI("ui/equipdlg05/vtabblue.png");

        Texture2D[] textures = { bk, vtab0, vtab1, vtabBlue, attrPage };
        for (int index = 0; index < textures.Length; index++)
        {
            Texture2D texture = textures[index];
            if (texture != null) texture.filterMode = FilterMode.Point;
        }

        titleBtn = Frames(loader, "titlesetbtn", 4);
        assignBtn = Frames(loader, "assignbtn", 4);
        affirmBtn = Frames(loader, "affirmbtn", 4);
        restoreBtn = Frames(loader, "restorebtn", 4);
        plusBtn = Frames(loader, "plusbtn", 4);
        minusBtn = Frames(loader, "minusbtn", 4);
        closeBtn = Frames(loader, "miniclose", 4);
        helpBtn = Frames(loader, "helpicon", 4);
        pointFire = Frames(loader, "pointfire", 8);
        radioBtn = Frames(loader, "radiobtn", 2);
        okBtn = Frames(loader, "okbtn", 4);
        delBtn = Frames(loader, "delbtn", 4);

        titleLine = loader.LoadUI("ui/equipdlg05/titleline.png");
        itemSel = loader.LoadUI("ui/equipdlg05/itemsel.png");
        pUL = loader.LoadUI("ui/equipdlg05/pul.png");
        pUM = loader.LoadUI("ui/equipdlg05/pum.png");
        pUR = loader.LoadUI("ui/equipdlg05/pur.png");
        pLM = loader.LoadUI("ui/equipdlg05/plm.png");
        pRM = loader.LoadUI("ui/equipdlg05/prm.png");
        pDL = loader.LoadUI("ui/equipdlg05/pdl.png");
        pDM = loader.LoadUI("ui/equipdlg05/pdm.png");
        pDR = loader.LoadUI("ui/equipdlg05/pdr.png");

        // ReadRes is deliberately not used: it adds a Read24Res step that this member
        // does not have. Only a null primary result admits the native Pkg24 fallback.
        byte[] encoded = loader.Pkg == null
            ? null
            : loader.Pkg.Read("res/uires/common/checkbox.gsa");
        if (encoded == null)
        {
            if (loader.Pkg24 == null) return;
            encoded = loader.Pkg24.Read("res/uires/common/checkbox.gsa");
            if (encoded == null) return;
        }

        DimgDecoder.Anim anim = DimgDecoder.DecodeAnim(encoded);
        if (!anim.ok) return;
        if (anim.f == null) return;

        // Publish before filling, even for an empty source. Ordinary array stores retain
        // null/bounds exceptions and the already-published prefix if an operation throws.
        checkbox = new Texture2D[anim.f.Length];
        for (int index = 0; index < anim.f.Length; index++)
        {
            checkbox[index] = anim.f[index].tex;
        }
    }

    // The existing manual GUI host is the sole caller/borrower. No second OnGUI or cache.
    internal void LoadForIndependentRuntime() => Load();
    internal Texture2D BackgroundForIndependentRuntime => bk;
    internal Texture2D VerticalTabNormalForIndependentRuntime => vtab0;
    internal Texture2D VerticalTabHoverForIndependentRuntime => vtab1;
    internal Texture2D VerticalTabBlueForIndependentRuntime => vtabBlue;
    internal Texture2D AttributeBackgroundForIndependentRuntime => attrPage;
    internal Texture2D TitleLineForIndependentRuntime => titleLine;
    internal Texture2D TitleSelectionForIndependentRuntime => itemSel;
    internal Texture2D TitlePatchUpperLeftForIndependentRuntime => pUL;
    internal Texture2D TitlePatchUpperMiddleForIndependentRuntime => pUM;
    internal Texture2D TitlePatchUpperRightForIndependentRuntime => pUR;
    internal Texture2D TitlePatchLeftMiddleForIndependentRuntime => pLM;
    internal Texture2D TitlePatchRightMiddleForIndependentRuntime => pRM;
    internal Texture2D TitlePatchDownLeftForIndependentRuntime => pDL;
    internal Texture2D TitlePatchDownMiddleForIndependentRuntime => pDM;
    internal Texture2D TitlePatchDownRightForIndependentRuntime => pDR;
    internal Texture2D[] EquipmentEffectCheckboxFramesForIndependentRuntime => checkbox;
    internal Texture2D[] TitleRadioFramesForIndependentRuntime => radioBtn;
    internal Texture2D[] TitleOkFramesForIndependentRuntime => okBtn;
    internal Texture2D[] TitleDeleteFramesForIndependentRuntime => delBtn;
    internal Texture2D[] TitleButtonFramesForIndependentRuntime => titleBtn;
    internal Texture2D[] AssignButtonFramesForIndependentRuntime => assignBtn;
    internal Texture2D[] AffirmButtonFramesForIndependentRuntime => affirmBtn;
    internal Texture2D[] RestoreButtonFramesForIndependentRuntime => restoreBtn;
    internal Texture2D[] PlusButtonFramesForIndependentRuntime => plusBtn;
    internal Texture2D[] MinusButtonFramesForIndependentRuntime => minusBtn;
    internal Texture2D[] CloseFramesForIndependentRuntime => closeBtn;
    internal Texture2D[] HelpFramesForIndependentRuntime => helpBtn;
    internal Texture2D[] PointFireFramesForIndependentRuntime => pointFire;
}
}
