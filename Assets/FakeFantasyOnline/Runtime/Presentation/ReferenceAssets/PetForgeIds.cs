namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class PetForgeIds
{
    public static bool IsPot(int itemId) => itemId == 0x18065;
    public static bool IsTalisman(int itemId) => itemId == 0x18060;
    public static bool IsBook(int itemId) => unchecked((uint)(itemId - 0x1806a)) < 74u;
    public static bool IsGreenBook(int itemId) =>
        (itemId & 1) != 0 && unchecked((uint)(itemId - 0x1806a)) < 74u;

    public static bool IsGrayBook(int itemId) =>
        (itemId & 1) == 0 && unchecked((uint)(itemId - 98410)) < 74u;
}

}
