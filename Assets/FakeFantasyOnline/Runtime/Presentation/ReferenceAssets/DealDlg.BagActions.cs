using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class DealDlg
{
    // Both pointer-held desktop items and explicit mobile actions use this admission.
    private int offerSession;
    internal static bool CanEditBagOffer => Visible && SocialClient.TradeActive &&
        !SocialClient.MyLocked && !SocialClient.MyConfirmed && !SocialClient.OtherConfirmed;
    private bool CanEditOffer => I == this && CanEditBagOffer;

    internal static void ChangeBagOffer(byte tab, int slot)
    {
        if (I == null || !CanEditBagOffer || SysMsgBox.Visible) return;
        int index = I.myOffer.FindIndex(offer => offer.Item1 == tab && offer.Item2 == slot);
        if (index >= 0) { I.myOffer.RemoveAt(index); I.SendOffer(); }
        else I.OfferBagItem(tab, slot);
    }

    private void OfferBagItem(byte tab, int slot)
    {
        if (!CanEditOffer || SysMsgBox.Visible || tab >= InventoryDlg.Items.Length || slot < 0) return;
        var item = InventoryDlg.ItemAt(tab, slot);
        if (item.id <= 0 || item.count <= 0 || myOffer.Count >= 20 ||
            myOffer.Exists(offer => offer.Item1 == tab && offer.Item2 == slot)) return;
        if (InventoryDlg.WarnIfLocked(tab, slot, "交易")) return;
        if (item.bound)
        {
            ChatUI.Add("系统", "该物品已绑定, 不能交易(可用天工剪解绑)", new Color(1, 180f / 255, 120f / 255, 1));
            return;
        }
        int session = offerSession, partner = SocialClient.OtherId;
        int receipt = InventoryDlg.ReceiptRevision;
        FusedGuard.Confirm(item, "放到交易栏", () =>
        {
            if (!CanEditOffer || session != offerSession || partner != SocialClient.OtherId ||
                receipt != InventoryDlg.ReceiptRevision) return;
            var live = InventoryDlg.ItemAt(tab, slot);
            if (live.id != item.id || live.count != item.count || live.bound || live.locked ||
                !live.Equals(item) || myOffer.Count >= 20 ||
                myOffer.Exists(offer => offer.Item1 == tab && offer.Item2 == slot)) return;
            myOffer.Add((tab, slot, live.count));
            SendOffer();
        });
        LastFusedMessageForIndependentRuntime = FusedGuard.Has(item) && SysMsgBox.I != null
            ? SysMsgBox.I.MessageForIndependentRuntime : string.Empty;
    }
}
}
