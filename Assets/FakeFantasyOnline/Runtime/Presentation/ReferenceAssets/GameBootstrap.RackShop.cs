namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameBootstrap
{
    private ReferenceFirstWorldRackShopDialog persistentRackShop;

    internal ReferenceFirstWorldRackShopDialog BorrowRackShopForIndependentRuntime()
    {
        if (persistentRackShop == null) persistentRackShop = new ReferenceFirstWorldRackShopDialog();
        return persistentRackShop;
    }

    private void ReleaseRackShopForIndependentRuntime(bool detachOnly = false)
    {
        if (persistentRackShop != null)
        {
            persistentRackShop.Dispose(detachOnly);
            persistentRackShop = null;
        }
        else if (!detachOnly)
        {
            // A failed first build can still have created native owners/resources.
            RackShopDlg.ReleaseIndependentRuntime();
        }
    }
}
}
