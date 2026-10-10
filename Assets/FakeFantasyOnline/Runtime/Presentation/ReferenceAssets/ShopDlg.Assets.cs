namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    private void LoadAssets()
    {
        triedAssets = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) return;
        bg = loader.LoadGsa("res/uires/tradedlg/tradedlgbk.gsa");
        goldIc = loader.LoadGsa("res/uires/common/gold.gsa");
        silverIc = loader.LoadGsa("res/uires/common/silver.gsa");
        copperIc = loader.LoadGsa("res/uires/common/copper.gsa");
        closeIc = loader.LoadGsa("res/uires/common/miniclose.gsa");
        itemBg = loader.LoadGsa("res/uires/tradedlg/tradeiteml.gsa");
        itemBgR = loader.LoadGsa("res/uires/tradedlg/tradeitemr.gsa");
        buyLabelIc = loader.LoadGsa("res/uires/grocerydlg/buylabel.gsa");
        repairAll = loader.LoadGsa("res/uires/repairdlg/repairall.gsa");
        repairSpec = loader.LoadGsa("res/uires/repairdlg/repairspecial.gsa");
        repairOne = loader.LoadGsa("res/uires/repairdlg/repairbtn.gsa");
        tradeBtn = loader.LoadGsa("res/uires/tradedlg/tradebtn.gsa");
        clearBtn = loader.LoadGsa("res/uires/tradedlg/clearbtn.gsa");
        giveupBtn = loader.LoadGsa("res/uires/common/giveupbtn.gsa");
        numBg = loader.LoadGsa("res/uires/gameui/numinput.gsa");
        inputBtn = loader.LoadGsa("res/uires/gameui/inputbtn.gsa");
        minBtn = loader.LoadGsa("res/uires/common/minbtn.gsa");
        maxBtn = loader.LoadGsa("res/uires/common/maxbtn.gsa");
        spinL = loader.LoadGsa("res/uires/gameui/spinleft.gsa");
        spinR = loader.LoadGsa("res/uires/gameui/spinright.gsa");
        slide = loader.LoadGsa("res/uires/gameui/smallslide.gsa");
    }
}
}
