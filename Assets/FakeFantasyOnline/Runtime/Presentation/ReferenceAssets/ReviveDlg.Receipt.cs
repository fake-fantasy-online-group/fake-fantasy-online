using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class ReviveDlg
{
    private WorldLocalPlayerLifeState lifeReceiptForIndependentRuntime;

    internal bool ObserveLifeReceiptForIndependentRuntime(WorldLocalPlayerLifeState receipt)
    {
        if (ReferenceEquals(lifeReceiptForIndependentRuntime, receipt)) return false;
        lifeReceiptForIndependentRuntime = receipt;
        return true;
    }
}
}
