using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;

static class MobileFixedActionContract
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    static readonly MobileFixedAction HP = MobileFixedAction.HealthPotion;
    static readonly MobileFixedAction MP = MobileFixedAction.ManaPotion;
    static readonly MobileFixedAction Mount = MobileFixedAction.Mount;
    internal static void Run()
    {
        foreach (string text in new[] { "5秒内恢复生命725", "5秒内恢复2700生命", "瞬间恢复生命4000点，没有冷却时间", "美味的喜糖，可以在5秒内恢复生命1325点" })
            Check(MobilePotionEligibility.Allows(HP, text) && !MobilePotionEligibility.Allows(MP, text), "known direct HP wording");
        foreach (string text in new[] { "5秒内恢复法力725", "5秒内恢复2700法力", "瞬间恢复法力4000点，没有冷却时间" })
            Check(MobilePotionEligibility.Allows(MP, text) && !MobilePotionEligibility.Allows(HP, text), "known direct MP wording");
        string dual = "瞬间恢复50％生命和50％法力，使用间隔5秒";
        Check(MobilePotionEligibility.Allows(HP, dual) && MobilePotionEligibility.Allows(MP, dual), "explicit dual recovery can bind either key");
        foreach (string text in new[] { "生命药水", "超强生命礼包，双击获得药水", "宠物5秒内恢复生命500", "目标瞬间恢复生命500", "瞬间恢复城门和结界石生命值20万点", "装备后5秒内恢复生命500", "5秒内恢复生命上限500", "" })
            Check(!MobilePotionEligibility.Allows(HP, text), "name or unrelated targets cannot auto-select recovery");

        NetClient.I = new NetClient(); PlayerController.I = new PlayerController();
        CharData.prefs.Clear(); CharData.PrefSuffix = "character-a";
        Array.Clear(GameHud.hotSkill, 0, 30); Array.Clear(GameHud.hotKind, 0, 30);
        InventoryDlg.current = new InventoryDlg.Item { id = 801, bagIdx = 0, count = 4, name = "fixture", desc = "5秒内恢复生命725" };
        Check(!GameHud.MobileFixedState(HP).Available && GameHud.MobileFixedState(HP).ItemId == 0, "never chooses first bag item by itself");
        GameHud.UseMobileFixedAction(HP);
        Check(NetClient.I.itemUses == 0 && ChatUI.lastMessage.Contains("尚未配置"), "unconfigured gives useful hint without consuming");
        GameHud.hotSkill[3] = 801; GameHud.hotKind[3] = 0;
        Check(GameHud.MobileFixedState(HP).ItemId == 0, "skill ID cannot be mistaken for potion");
        GameHud.hotKind[3] = 2;
        Check(GameHud.MobileFixedState(HP).ItemId == 801 && GameHud.MobileFixedState(HP).Count == 4, "uses existing explicit item shortcut");
        GameHud.UseMobileFixedAction(HP);
        Check(NetClient.I.itemUses == 1 && NetClient.I.lastItem == 801 && NetClient.I.lastTab == 0 && NetClient.I.lastSlot == 0, "uses actual item and current bag coordinates");
        Check(GameHud.fixedCooldownStarts == 1 && InventoryDlg.current.count == 4, "starts original cooldown without faking inventory");

        GameHud.fixedCooldown = .5f;
        Check(!GameHud.MobileFixedState(HP).Available && GameHud.MobileFixedState(HP).CooldownFraction == .5f, "cooldown is exact shared hotbar cooldown");
        GameHud.UseMobileFixedAction(HP); Check(NetClient.I.itemUses == 1, "cooldown prevents second use"); GameHud.fixedCooldown = 0;
        InventoryDlg.current.locked = true; GameHud.UseMobileFixedAction(HP); Check(NetClient.I.itemUses == 1, "locked item cannot consume"); InventoryDlg.current.locked = false;
        InventoryDlg.current.blocked = 1; GameHud.UseMobileFixedAction(HP); Check(NetClient.I.itemUses == 1, "blocked item cannot consume"); InventoryDlg.current.blocked = 0;
        SocialClient.TradeActive = true; GameHud.UseMobileFixedAction(HP); Check(NetClient.I.itemUses == 1, "hidden active trade prevents use"); SocialClient.TradeActive = false;
        ItemActions.handled = true; GameHud.UseMobileFixedAction(HP); Check(NetClient.I.itemUses == 1 && GameHud.fixedCooldownStarts == 1, "original ItemActions keeps first refusal"); ItemActions.handled = false;
        PlayerController.I.IsDead = true; GameHud.UseMobileFixedAction(HP); Check(NetClient.I.itemUses == 1, "dead player cannot use"); PlayerController.I.IsDead = false;
        NetClient.I.ConnectedForIndependentRuntime = false; GameHud.UseMobileFixedAction(HP); Check(NetClient.I.itemUses == 1, "disconnected cannot use"); NetClient.I.ConnectedForIndependentRuntime = true;
        MobileClientPresentation.GameplayInputBlocked = true; GameHud.UseMobileFixedAction(HP); Check(NetClient.I.itemUses == 1, "editing hard-blocks action entry even if caller is stale"); MobileClientPresentation.GameplayInputBlocked = false;

        var before = (int[])GameHud.hotSkill.Clone(); int writes = NetClient.I.writes;
        var bag = new InventoryDlg(); bag.Select(); GUI.click = "设为红药"; bag.Draw();
        Check(GameHud.MobileFixedState(HP).ItemId == 801 && CharData.prefs.Count == 1, "actual bag action binds character-local fixed key");
        Check(NetClient.I.itemUses == 1 && NetClient.I.writes == writes, "binding sends no use or hotbar request");
        for (int i = 0; i < 30; i++) Check(before[i] == GameHud.hotSkill[i], "fixed button never occupies or rewrites shortcuts");
        InventoryDlg.current.count = 0;
        Check(!GameHud.MobileFixedState(HP).Available && GameHud.MobileFixedState(HP).ItemId == 801, "depleted explicit choice stays selected");
        InventoryDlg.current = new InventoryDlg.Item { id = 802, bagIdx = 0, count = 3, desc = "5秒内恢复生命875" };
        GameHud.hotSkill[3] = 802;
        Check(GameHud.MobileFixedState(HP).ItemId == 801 && !GameHud.MobileFixedState(HP).Available, "cannot silently substitute stronger item after depletion");
        Check(!GameHud.BindMobilePotion(HP, 0, 0, 801), "stale selection cannot bind replacement item");
        Check(!GameHud.BindMobilePotion(MP, 0, 0, 802), "HP-only description cannot bind MP");
        CharData.PrefSuffix = "character-b";
        Check(GameHud.MobileFixedState(HP).ItemId == 802, "other character does not inherit explicit fixed binding");
        CharData.PrefSuffix = "character-a";
        Check(GameHud.MobileFixedState(HP).ItemId == 801, "original character binding retained");

        CharData.riding = false; CharData.rideAnchorId = 0; CharData.petModel = 0; CharData.petRidable = false;
        CharData.petSkills = new CharData.PetSkill[0]; Time.unscaledTime = 10;
        Check(!GameHud.MobileFixedState(Mount).Available && GameHud.MobileFixedState(Mount).Label == "上坐骑", "no pet has unavailable up action");
        GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.mountUses == 0, "no pet never sends mount command");
        CharData.petModel = 12; CharData.petRidable = true;
        Check(!GameHud.MobileFixedState(Mount).Available && GameHud.MobileFixedState(Mount).Reason.Contains("鞍具"), "saddle-only cannot invent a skill");
        CharData.petSkills = new[] { new CharData.PetSkill { id = 321, name = "坐骑", icon = 80, active = true, level = 1 } };
        Check(GameHud.MobileFixedState(Mount).Available && !GameHud.MobileFixedState(Mount).Active, "server learned mount skill enables up action");
        GameHud.UseMobileFixedAction(Mount);
        Check(NetClient.I.mountUses == 1 && NetClient.I.lastSkill == 321 && !CharData.riding, "uses received mount skill id without faking riding");
        GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.mountUses == 1, "repeat tap while awaiting server cannot toggle twice");
        CharData.riding = true; CharData.mountModel = 12;
        Check(GameHud.MobileFixedState(Mount).Active && GameHud.MobileFixedState(Mount).Label == "下坐骑", "server receipt changes action label and active state");
        GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.petShows == 1 && CharData.riding, "own dismount requests original pet show without local state change");
        CharData.riding = false; GameHud.MobileFixedState(Mount);
        CharData.riding = true; CharData.rideAnchorId = 77;
        GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.rideLeaves == 1 && CharData.rideAnchorId == 77, "passenger dismount uses leave ride route and waits for receipt");
        Time.unscaledTime += 3; Check(GameHud.MobileFixedState(Mount).Available, "server rejection or missed receipt permits bounded retry");
        CharData.riding = false; CharData.rideAnchorId = 0; CharData.petRidable = false;
        Check(!GameHud.MobileFixedState(Mount).Available, "server non-ridable state wins over skill presence");
        Check(GameHud.MobileFixedState(MobileFixedAction.Sit).Label == "坐下", "sit label reads native state");
        Check(GameHud.MobileFixedState(MobileFixedAction.Sit).Icon != null && Resources.lastPath == "FakeFantasyOnline/MobileControls/Sit" && Resources.loads == 1, "sit uses approved embedded icon and caches its load");
        GameHud.UseMobileFixedAction(MobileFixedAction.Sit);
        Check(GameHud.MobileFixedState(MobileFixedAction.Sit).Label == "起立" && PlayerController.I.SittingForIndependentRuntime, "sit dispatch uses player entry");
        MobileClientPresentation.GameplayInputBlocked = true; GameHud.UseMobileFixedAction(MobileFixedAction.Sit);
        Check(PlayerController.I.SittingForIndependentRuntime, "editing cannot toggle sit"); MobileClientPresentation.GameplayInputBlocked = false;
        Console.WriteLine("PASS fixed mobile mount/potion actual binding and dispatch: " + checks + " checks");
    }
}
