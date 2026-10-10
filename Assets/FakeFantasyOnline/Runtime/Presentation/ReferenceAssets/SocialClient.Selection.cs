namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class SocialClient
{
    // Official 1.5.8 token 06000d27: writes the selected party row id (+0x6c), the value
    // the party and family windows read to know which row is active.
    public static void SelectMember(int id)
    {
        SelectedMemberId = id;
    }

    // Official 1.5.8 token 06000d2d: clears the trade block in field order - the four
    // flags, the id and name, both money fields and the four item lists. The pet maps
    // MyItemPets/OtherItemPets sit outside the official reset, so they are left alone.
    public static void ResetTrade()
    {
        TradeActive = false;
        OtherId = 0;
        OtherName = string.Empty;
        MyLocked = false;
        OtherLocked = false;
        MyConfirmed = false;
        OtherConfirmed = false;
        MyMoney = 0;
        OtherMoney = 0;
        MyItems.Clear();
        OtherItems.Clear();
        MyItemInfo.Clear();
        OtherItemInfo.Clear();
    }
}

}
