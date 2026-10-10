namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 UiEsc.CloseTop (06000fca): the whole ordered, synchronous decision body.
/// ARM64 0x3a830c / 3812 bytes, SHA256 46d21beaa7fa5984c16f20dfc5632671958d3c774c975c5d76ce0d515aeac2d9;
/// x86_64 0x3bb050 / 3760 bytes, SHA256 928358a18ef8b1b7b17f7e019afe6922cf33de231106bb6a9b94c4cb88feab1f.
/// Every hit returns immediately. Reserve-only slots leave cancellation/GUI closing to their own
/// original owner. The only added slot is the explicitly approved FAMILYMAIL-ESC-001 exception.
/// </summary>
public static class UiEsc
{
    public static bool CloseTop()
    {
        ReferenceFirstWorldEscapeDispatch.LastAction = ReferenceFirstWorldEscapeAction.None;

        // Original slot 1: reserve.
        if (SkillTargeting.Active)
        {
            return Handled(ReferenceFirstWorldEscapeAction.SkillTargeting);
        }

        // Original slot 2: reserve.
        if (AvatarFuse.Active)
        {
            return Handled(ReferenceFirstWorldEscapeAction.AvatarFusion);
        }

        // Original slot 3: reserve.
        if (ToolTarget.Active)
        {
            return Handled(ReferenceFirstWorldEscapeAction.ToolTarget);
        }

        // Original slot 4: reserve.
        if (InventoryDlg.SplitOpen)
        {
            return Handled(ReferenceFirstWorldEscapeAction.InventorySplit);
        }

        // Original slot 5: reserve.
        if (ShopDlg.Picking)
        {
            return Handled(ReferenceFirstWorldEscapeAction.ShopPicker);
        }

        // Original slot 6: reserve.
        if (RepairDlg.Picking)
        {
            return Handled(ReferenceFirstWorldEscapeAction.RepairSelection);
        }

        // Original slot 7: reserve.
        if (EquipDlg.TitleSubOpen)
        {
            return Handled(ReferenceFirstWorldEscapeAction.LocalEquipmentDialog);
        }

        // Original slot 8: close; 0x3a8690.
        if (UiInput.MenuOpen)
        {
            UiInput.Close();
            return Handled(ReferenceFirstWorldEscapeAction.InputMenu);
        }

        // Original slot 9: close; 0x3a86cc.
        if (ChatCopyOverlay.Visible)
        {
            ChatCopyOverlay.Hide();
            return Handled(ReferenceFirstWorldEscapeAction.ChatCopyOverlay);
        }

        // Original slot 10: reserve; 0x3a86f4.
        if (SysMsgBox.Visible)
        {
            return Handled(ReferenceFirstWorldEscapeAction.SystemMessageBox);
        }

        // Original slot 11: reserve; 0x3a8700.
        if (MoneyInputDlg.Visible)
        {
            return Handled(ReferenceFirstWorldEscapeAction.MoneyInput);
        }

        // Original slot 12: reserve; 0x3a8724.
        if (ColorDlg.Visible)
        {
            return Handled(ReferenceFirstWorldEscapeAction.ColorDialog);
        }

        // Original slot 13: reserve; 0x3a8730.
        if (FaceDlg.Visible)
        {
            return Handled(ReferenceFirstWorldEscapeAction.FaceDialog);
        }

        // Original slot 14: reserve; 0x3a873c.
        if (FindModeDlg.Visible)
        {
            return Handled(ReferenceFirstWorldEscapeAction.FindModeDialog);
        }

        // Original slot 15: close; 0x3a8748.
        if (SecCodeAskDlg.Visible)
        {
            SecCodeAskDlg.CloseNow();
            return Handled(ReferenceFirstWorldEscapeAction.SecurityCodePrompt);
        }

        // Original slot 16: close; 0x3a8760.
        if (AccountSecDlg.Visible)
        {
            AccountSecDlg.CloseNow();
            return Handled(ReferenceFirstWorldEscapeAction.AccountSecurity);
        }

        // Original slot 17: close; 0x3a8778.
        if (ArenaResultDlg.Showing)
        {
            ArenaResultDlg.Close();
            return Handled(ReferenceFirstWorldEscapeAction.ArenaResult);
        }

        // Original slot 18: reserve.
        if (ArenaDraftDlg.Visible)
        {
            return Handled(ReferenceFirstWorldEscapeAction.ArenaDraft);
        }

        // Original slot 19: reserve.
        if (ArenaDeathDlg.Visible)
        {
            return Handled(ReferenceFirstWorldEscapeAction.ArenaDeath);
        }

        // Original slot 20: close.
        if (ArenaEscDlg.Visible)
        {
            ArenaEscDlg.Close();
            return Handled(ReferenceFirstWorldEscapeAction.ArenaEsc);
        }

        // Original slot 21: close; 0x3a8838.
        if (ReferenceFirstWorldEscapeDispatch.DialogVisible)
        {
            ReferenceFirstWorldEscapeDispatch.CloseDialog();
            return Handled(ReferenceFirstWorldEscapeAction.NpcDialog);
        }

        // Original slot 22: close; 0x3a885c.
        if (TransportUI.VisibleS)
        {
            TransportUI.CloseS();
            return Handled(ReferenceFirstWorldEscapeAction.TransportDialog);
        }

        // Original slot 23: close; 0x3a8888.
        if (PlayerActionMenu.Visible)
        {
            PlayerActionMenu.Hide();
            return Handled(ReferenceFirstWorldEscapeAction.PlayerActionMenu);
        }

        // Original slot 24: close; 0x3a88c0.
        if (ShopDlg.Visible)
        {
            ShopDlg.Hide();
            return Handled(ReferenceFirstWorldEscapeAction.NativeShop);
        }

        // Original slot 25: close; 0x3a88e8.
        if (RepairDlg.Visible)
        {
            RepairDlg.Hide();
            return Handled(ReferenceFirstWorldEscapeAction.RepairDialog);
        }

        // Original slot 26: close; 0x3a8918.
        if (ComposeDlg.Visible)
        {
            ComposeDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.ComposeDialog);
        }

        // Original slot 27: close; 0x3a8958.
        if (SculpDlg.Visible)
        {
            SculpDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.SculpDialog);
        }

        // Original slot 28: close; 0x3a8998.
        if (HairSalonDlg.Active)
        {
            HairSalonDlg.CloseNow();
            return Handled(ReferenceFirstWorldEscapeAction.HairSalon);
        }

        // Original slot 29: close; 0x3a89c0.
        if (QueryDlg.Visible)
        {
            QueryDlg.CloseNow();
            return Handled(ReferenceFirstWorldEscapeAction.QueryDialog);
        }

        // Original slot 30: close; 0x3a89d8.
        if (SelectPopupDlg.Visible)
        {
            SelectPopupDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.SelectPopupDialog);
        }

        // Original slot 31: close; 0x3a89ec.
        if (TradeDlg.Visible)
        {
            TradeDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.TradeDialog);
        }

        // Original slot 32: close; 0x3a89fc.
        if (DealDlg.Visible)
        {
            DealDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.DealDialog);
        }

        // Original slot 33: close; 0x3a8a20.
        if (InventoryDlg.IsOpen)
        {
            InventoryDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.InventoryDialog);
        }

        // Original slot 34: close; 0x3a8a58.
        if (EquipDlg.Opened)
        {
            EquipDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.LocalEquipmentDialog);
        }

        // Original slot 35: close; 0x3a8a98.
        if (ReferenceFirstWorldEscapeDispatch.InspectedEquipmentVisible)
        {
            ReferenceFirstWorldEscapeDispatch.CloseInspectedEquipment();
            return Handled(ReferenceFirstWorldEscapeAction.InspectedEquipmentDialog);
        }

        // Original slot 36: close; 0x3a8ad8.
        if (Win05.AnyOpen)
        {
            Win05.CloseNow();
            return Handled(ReferenceFirstWorldEscapeAction.SkillDialog);
        }

        // Original slot 37: close; 0x3a8b18.
        if (StoreDlg.Visible)
        {
            StoreDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.StoreDialog);
        }

        // Original slot 38: close; 0x3a8b40.
        if (GarageDlg.Visible)
        {
            GarageDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.GarageDialog);
        }

        // Original slot 39: close; 0x3a8b58.
        if (PasswdDlg.Visible)
        {
            PasswdDlg.CloseNow();
            return Handled(ReferenceFirstWorldEscapeAction.PasswordDialog);
        }

        // Original slot 40: close; 0x3a8b88.
        if (TaskDlg.Visible)
        {
            TaskDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.TaskDialog);
        }

        // Original slot 41: close; 0x3a8bc8.
        if (MailDlg.Visible)
        {
            MailDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.MailDialog);
        }

        // Original slot 42: close; 0x3a8c04.
        if (MailConfigDlg.Visible)
        {
            MailConfigDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.MailConfigDialog);
        }

        // Original slot 43: close; 0x3a8c3c.
        if (MailResistDlg.Visible)
        {
            MailResistDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.MailResistDialog);
        }

        // FAMILYMAIL-ESC-001, not an original slot: close the composer before any family parent.
        // Hide preserves its title/body draft, and this exception does not extend to Duel.
        if (FamilyMailDlg.get_Visible())
        {
            FamilyMailDlg.Hide();
            return Handled(ReferenceFirstWorldEscapeAction.FamilyMail);
        }

        // Original slot 44: close; 0x3a8c78.
        if (FamilyDlg.get_Visible())
        {
            FamilyDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.FamilyDialog);
        }

        // Original slot 45: close; 0x3a8ca0.
        if (FamilyCreateDlg.get_Visible())
        {
            FamilyCreateDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.FamilyCreateDialog);
        }

        // Original slot 46: close; 0x3a8cb8.
        if (FamilyDetailDlg.get_Visible())
        {
            FamilyDetailDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.FamilyDetailDialog);
        }

        // Original slot 47: close; 0x3a8cd0.
        if (FamilyRelationDlg.get_Visible())
        {
            FamilyRelationDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.FamilyRelationDialog);
        }

        // Original slot 48: close; 0x3a8d00.
        if (TeamDlg.Visible)
        {
            TeamDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.TeamDialog);
        }

        // Original slot 49: close; 0x3a8d40.
        if (RackShopDlg.Visible)
        {
            RackShopDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.RackShopDialog);
        }

        // Original slot 50: close; 0x3a8d80.
        if (GroceryStatusDlg.Visible)
        {
            GroceryStatusDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.GroceryStatus);
        }

        // Original slot 51: close; 0x3a8da8.
        if (GroceryConfigDlg.Visible)
        {
            GroceryConfigDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.GroceryConfig);
        }

        // Original slot 52: close; 0x3a8dc0.
        if (GroceryDetailDlg.Visible)
        {
            GroceryDetailDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.GroceryDetail);
        }

        // Original slot 53: close; 0x3a8df0.
        if (StallConfigDlg.Active)
        {
            StallConfigDlg.Close();
            return Handled(ReferenceFirstWorldEscapeAction.StallConfig);
        }

        // Original slot 54: close; 0x3a8e30.
        if (StallDetailDlg.Active)
        {
            StallDetailDlg.CloseNow();
            return Handled(ReferenceFirstWorldEscapeAction.StallDetail);
        }

        // Original slot 55: close; 0x3a8e70.
        if (StallDlg.Visible)
        {
            StallDlg.CloseNow();
            return Handled(ReferenceFirstWorldEscapeAction.StallDialog);
        }

        // Original slot 56: close; 0x3a8e98.
        if (JobCustomDlg.get_Visible())
        {
            JobCustomDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.JobCustomDialog);
        }

        // Original slot 57: close; 0x3a8eb0.
        if (JobOfferDlg.get_Visible())
        {
            JobOfferDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.JobOfferDialog);
        }

        // Original slot 58: close; 0x3a8ec8.
        if (JobRightDlg.get_Visible())
        {
            JobRightDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.JobRightDialog);
        }

        // Original slot 59: close; 0x3a8ee0.
        if (FriendInfoDlg.Visible)
        {
            FriendInfoDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.FriendInfoDialog);
        }

        // Original slot 60: close; 0x3a8ef8.
        if (MasterInfoDlg.Visible)
        {
            MasterInfoDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.MasterInfoDialog);
        }

        // Original slot 61: close; 0x3a8f28.
        if (TeamConfigDlg.Visible)
        {
            TeamConfigDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.TeamConfigDialog);
        }

        // Original slot 62: close; 0x3a8f68.
        if (FriendConfigDlg.Visible)
        {
            FriendConfigDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.FriendConfigDialog);
        }

        // Original slot 63: close; 0x3a8f90.
        if (MasterListDlg.Visible)
        {
            MasterListDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.MasterListDialog);
        }

        // Original slot 64: close; 0x3a8fc0.
        if (GossipDlg.Visible)
        {
            GossipDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.GossipDialog);
        }

        // Original slot 65: close; 0x3a8fe8.
        if (FigureDlg.Visible)
        {
            FigureDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.FigureDialog);
        }

        // Original slot 66: close; 0x3a9018.
        if (HelpWinDlg.Visible)
        {
            HelpWinDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.HelpWin);
        }

        // Original slot 67: close; 0x3a9040.
        if (ReportDlg.Visible)
        {
            ReportDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.ReportDialog);
        }

        // Original slot 68: close; 0x3a9058.
        if (VoiceSetDlg.Visible)
        {
            VoiceSetDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.VoiceSetDialog);
        }

        // Original slot 69: close; 0x3a9088.
        if (PetArousalDlg.Visible)
        {
            PetArousalDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.PetAwakening);
        }

        // Original slot 70: close; 0x3a90c8.
        if (PetForgeDlg.Visible)
        {
            PetForgeDlg.CloseNow();
            return Handled(ReferenceFirstWorldEscapeAction.PetForge);
        }

        // Original slot 71: close; 0x3a90f0.
        if (AttrWashDlg.Visible)
        {
            AttrWashDlg.CloseNow();
            return Handled(ReferenceFirstWorldEscapeAction.AttributeWash);
        }

        // Original slot 72: close; 0x3a9120.
        if (SysConfigDlg.Visible)
        {
            SysConfigDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.SystemSettings);
        }

        // Original slot 73: close; 0x3a9160.
        if (WorldMapDlg.Visible)
        {
            WorldMapDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.WorldMapDialog);
        }

        // Original slot 74: close; 0x3a91a0.
        if (ReferenceFirstWorldEscapeDispatch.DiagramOpen)
        {
            ReferenceFirstWorldEscapeDispatch.CloseDiagram();
            return Handled(ReferenceFirstWorldEscapeAction.LocalMapDiagram);
        }

        // Original slot 75: close; 0x3a91c8.
        if (SysOptionDlg.Visible)
        {
            SysOptionDlg.Toggle();
            return Handled(ReferenceFirstWorldEscapeAction.SystemMenu);
        }

        // ARM64 0x3a91cc -> 0x3a91dc: only the all-false path returns false.
        // UIManager.Update owns the subsequent generic-stack / ArenaEsc / SysOption fallback.
        return false;
    }

    private static bool Handled(ReferenceFirstWorldEscapeAction action)
    {
        ReferenceFirstWorldEscapeDispatch.LastAction = action;
        return true;
    }
}

}
