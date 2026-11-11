using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Mobile long-press rows for destinations that desktop reaches only by dragging a held
// item: the mail composer, the refining furnace, a change-set cell and the ground.
// Each row reuses the desktop drop function; rows appear only when that target exists.
internal partial class InventoryDlg
{
    internal enum MobileDestination { Mail, Refine, ChangeSet, Discard }
    private static readonly MobileDestination[] MobileDestinations =
        { MobileDestination.Mail, MobileDestination.Refine, MobileDestination.ChangeSet, MobileDestination.Discard };

    internal static string MobileDestinationLabel(MobileDestination destination) => destination switch
    {
        MobileDestination.Mail => "\u653e\u5165\u90ae\u4ef6",
        MobileDestination.Refine => "\u7cbe\u70bc",
        MobileDestination.ChangeSet => "\u653e\u5165\u5957\u88c5",
        _ => "\u4e22\u5f03",
    };

    private bool MobileDestinationShown(MobileDestination destination, Item item) => destination switch
    {
        MobileDestination.Mail => MailDlg.ComposeVisible,
        MobileDestination.Refine => ComposeDlg.RefineOpenForMobile && tab == 2 && !item.pet.HasValue,
        MobileDestination.ChangeSet => Win05.IsShowing("changeequipdlg") && !item.pet.HasValue,
        _ => !item.pet.HasValue,
    };

    private int MobileDestinationRows(Item item)
    {
        int rows = 0;
        foreach (MobileDestination destination in MobileDestinations)
            if (MobileDestinationShown(destination, item)) rows++;
        return rows;
    }

    private int DrawMobileDestinations(Rect box, float scale, Item item, bool available, int row)
    {
        bool plain = available && !DealDlg.Visible;
        foreach (MobileDestination destination in MobileDestinations)
        {
            if (!MobileDestinationShown(destination, item)) continue;
            bool enabled = plain && (destination != MobileDestination.Discard || !item.locked);
            if (MobilePanelLayout.Action(box, row++, MobileDestinationLabel(destination), scale, enabled))
                ExecuteMobileDestination(item, destination);
        }
        return row;
    }

    private void ExecuteMobileDestination(Item item, MobileDestination destination)
    {
        Item live = ItemAt(tab, mobileSlot);
        if (mobileSlot < 0 || mobileTab != tab || live.id <= 0 || live.id != mobileId || live.id != item.id ||
            HasHeldAny || splitOpen || DealDlg.IsOffered((byte)tab, live.bagIdx))
        { ClearMobileSelection(); return; }
        byte bagTab = (byte)tab;
        int bagSlot = live.bagIdx;
        switch (destination)
        {
            case MobileDestination.Mail:
                MailDlg.TryDropAttach(bagTab, bagSlot, MailDlg.SlotAbs(0).center);
                break;
            case MobileDestination.Refine:
                ComposeDlg.TryMobileRefine(bagTab, bagSlot);
                break;
            case MobileDestination.ChangeSet:
                Win05.BeginMobileChangeSet(bagTab, bagSlot, live.id);
                break;
            default:
                if (IsLocked(bagTab, bagSlot))
                    ChatUI.Add("\u7cfb\u7edf", "\u8be5\u7269\u54c1\u5df2\u52a0\u9501, \u65e0\u6cd5\u4e22\u5f03\u3002\u957f\u6309\u9009\u62e9\u201c\u89e3\u9501\u201d\u53ef\u89e3\u9501", new Color(1, 180f / 255f, 120f / 255f, 1));
                else
                {
                    (int x, int y) = PlayerMapPixel();
                    DropBagItemWithConfirmation(bagTab, bagSlot, live.id, live.name, live.bound, x, y, true);
                }
                break;
        }
        ClearMobileSelection();
    }

    // Touch has no pointer over the ground: drop where the player stands, in map pixels.
    private static (int, int) PlayerMapPixel()
    {
        Transform player = GameBootstrap.PlayerT;
        if (player == null) return MouseWorld();
        Vector3 world = player.position;
        return (Mathf.RoundToInt(world.x * GameBootstrap.CurPpu + GameBootstrap.CurWorldW * .5f),
            Mathf.RoundToInt(GameBootstrap.CurWorldH * .5f - world.y * GameBootstrap.CurPpu));
    }
}
}
