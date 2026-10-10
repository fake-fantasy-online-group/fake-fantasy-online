using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceWorldReceiveOwner
{
    // Official Dispatch 0x8007, after InventoryDlg.MaxWeight. Short arrays leave
    // the previous four fields intact; the packet values are not scaled or clamped.
    public void ReceivePlayerRingFields(IReadOnlyList<int> attributes)
    {
        if (attributes == null || attributes.Count < 29) return;
        CharData.nianli = attributes[25];
        CharData.maxNianli = attributes[26];
        CharData.naili = attributes[27];
        CharData.maxNaili = attributes[28];
    }
}

}
