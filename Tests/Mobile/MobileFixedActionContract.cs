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

        TestManualMountBindings();
        Check(GameHud.MobileFixedState(MobileFixedAction.Sit).Label == "坐下", "sit label reads native state");
        Check(GameHud.MobileFixedState(MobileFixedAction.Sit).Icon != null && Resources.lastPath == "FakeFantasyOnline/MobileControls/Sit" && Resources.loads == 1, "sit uses approved embedded icon and caches its load");
        GameHud.UseMobileFixedAction(MobileFixedAction.Sit);
        Check(GameHud.MobileFixedState(MobileFixedAction.Sit).Label == "起立" && PlayerController.I.SittingForIndependentRuntime, "sit dispatch uses player entry");
        MobileClientPresentation.GameplayInputBlocked = true; GameHud.UseMobileFixedAction(MobileFixedAction.Sit);
        Check(PlayerController.I.SittingForIndependentRuntime, "editing cannot toggle sit"); MobileClientPresentation.GameplayInputBlocked = false;
        Console.WriteLine("PASS fixed mobile mount/potion actual binding and dispatch: " + checks + " checks");
    }
    static void TestManualMountBindings()
    {
        CharData.PrefSuffix = "mount-character-a";
        CharData.riding = false; CharData.rideAnchorId = 0; CharData.petModel = 12; CharData.petRidable = true;
        Time.unscaledTime = 10;
        var up = new CharData.PetSkill { id = 321, name = "坐骑", icon = 80, active = true, level = 1 };
        var down = new CharData.PetSkill { id = 322, name = "主动攻击", icon = 81, active = true, level = 1 };
        var passive = new CharData.PetSkill { id = 323, name = "坐骑", active = false, level = 1 };
        var unlearned = new CharData.PetSkill { id = 324, name = "坐骑", active = true, level = 0 };
        CharData.petSkills = new[] { up, down, passive, unlearned };
        Check(!GameHud.MobileFixedState(Mount).Available && GameHud.MobileFixedState(Mount).Label == "上坐骑", "learned mount skill never auto-selects");
        GameHud.UseMobileFixedAction(Mount);
        Check(NetClient.I.mountUses == 0 && NetClient.I.petShows == 0 && NetClient.I.rideLeaves == 0, "unbound action dispatches nothing");
        Check(GameHud.CanBindMobileMountSkill(false, up) && !GameHud.CanBindMobileMountSkill(false, down), "up accepts mount skill only");
        Check(GameHud.CanBindMobileMountSkill(true, down) && GameHud.CanBindMobileMountSkill(true, up), "down accepts every learned active including mount");
        Check(!GameHud.CanBindMobileMountSkill(false, passive) && !GameHud.CanBindMobileMountSkill(true, passive), "passive cannot bind either role");
        Check(!GameHud.CanBindMobileMountSkill(false, unlearned) && !GameHud.CanBindMobileMountSkill(true, unlearned), "unlearned cannot bind either role");
        CharData.skills[999] = 1;
        Check(!GameHud.BindMobileMountSkill(false, 999) && !GameHud.BindMobileMountSkill(true, 999), "character skill cannot bind pet roles");
        Check(!GameHud.BindMobileMountSkill(false, down.id) && !GameHud.BindMobileMountSkill(true, passive.id), "binding API revalidates eligibility");
        int writes = NetClient.I.writes, uses = NetClient.I.itemUses;
        var ids = (int[])GameHud.hotSkill.Clone(); var kinds = (int[])GameHud.hotKind.Clone();
        var panel = new Win05(); panel.PetActions(up.id); GUI.labels.Clear(); GUI.click = "设为上坐骑"; panel.DrawPetActions();
        Check(GUI.labels.Contains("设为上坐骑") && GameHud.MobileFixedState(Mount).Available, "actual pet menu binds up role");
        panel.PetActions(down.id); GUI.labels.Clear(); GUI.click = "设为下坐骑"; panel.DrawPetActions();
        Check(GUI.labels.Contains("设为下坐骑"), "actual pet menu offers and binds down role");
        Check(NetClient.I.mountUses == 0 && NetClient.I.itemUses == uses && NetClient.I.writes == writes, "binding never executes or writes server hotbar");
        for (int i = 0; i < 30; i++) Check(GameHud.hotSkill[i] == ids[i] && GameHud.hotKind[i] == kinds[i], "mount binding preserves shortcut ids and kinds");
        GameHud.fixedCooldown = .5f;
        Check(!GameHud.MobileFixedState(Mount).Available && GameHud.MobileFixedState(Mount).CooldownFraction == .5f, "selected up skill respects shared cooldown");
        GameHud.fixedCooldown = 0;
        CharData.petRidable = false; Check(!GameHud.MobileFixedState(Mount).Available, "skill mounting respects server ridable state"); CharData.petRidable = true;
        CharData.petModel = 0; Check(!GameHud.MobileFixedState(Mount).Available, "selected skill needs current pet"); CharData.petModel = 12;
        PlayerController.I.IsDead = true; GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.mountUses == 0, "dead player cannot use mount binding"); PlayerController.I.IsDead = false;
        SocialClient.TradeActive = true; GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.mountUses == 0 && !GameHud.BindMobileMountSkill(false, up.id), "trade blocks use and binding"); SocialClient.TradeActive = false;
        MobileClientPresentation.GameplayInputBlocked = true; GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.mountUses == 0 && !GameHud.BindMobileMountSkill(true, down.id), "layout editing blocks mount use and binding"); MobileClientPresentation.GameplayInputBlocked = false;
        NetClient.I.ConnectedForIndependentRuntime = false; GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.mountUses == 0, "disconnection blocks mount use"); NetClient.I.ConnectedForIndependentRuntime = true;
        GameHud.UseMobileFixedAction(Mount);
        Check(NetClient.I.mountUses == 1 && NetClient.I.lastSkill == up.id && !CharData.riding, "up dispatches explicit id without local riding mutation");
        GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.mountUses == 1, "pending blocks repeat click");
        Time.unscaledTime += 2.49f; Check(!GameHud.MobileFixedState(Mount).Available, "pending lasts until 2.5 seconds");
        Time.unscaledTime += .02f; Check(GameHud.MobileFixedState(Mount).Available, "pending permits retry after bounded timeout");
        GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.mountUses == 2, "bounded retry executes selected skill");
        CharData.riding = true;
        Check(GameHud.MobileFixedState(Mount).Available && GameHud.MobileFixedState(Mount).Active && GameHud.MobileFixedState(Mount).Label == "下坐骑", "receipt immediately clears pending and selects down role");
        GameHud.fixedCooldown = .5f; Check(!GameHud.MobileFixedState(Mount).Available, "down skill cooldown is checked too"); GameHud.fixedCooldown = 0;
        CharData.petSkills = new[] { up }; Check(!GameHud.MobileFixedState(Mount).Available, "missing down skill never falls back to mount skill or pet show"); CharData.petSkills = new[] { up, down };
        GameHud.UseMobileFixedAction(Mount);
        Check(NetClient.I.mountUses == 3 && NetClient.I.lastSkill == down.id && CharData.riding, "down dispatches explicit active pet skill and leaves server state alone");
        Check(NetClient.I.petShows == 0 && NetClient.I.rideLeaves == 0, "no automatic pet show or ride leave");
        CharData.riding = false; CharData.rideAnchorId = 77;
        Check(GameHud.MobileFixedState(Mount).Label == "下坐骑" && GameHud.MobileFixedState(Mount).Available, "anchor alone selects down role");
        GameHud.UseMobileFixedAction(Mount);
        Check(NetClient.I.lastSkill == down.id && CharData.rideAnchorId == 77 && NetClient.I.rideLeaves == 0, "passenger uses selected skill, not forced leave");
        CharData.rideAnchorId = 0;
        CharData.petSkills = new[] { new CharData.PetSkill { id = 325, name = "坐骑", active = true, level = 1 } };
        Check(!GameHud.MobileFixedState(Mount).Available, "pet change cannot replace missing selected mount skill");
        int mountUses = NetClient.I.mountUses; GameHud.UseMobileFixedAction(Mount);
        Check(NetClient.I.mountUses == mountUses, "missing selected skill sends no replacement");
        CharData.petSkills = new[] { up, down };
        CharData.PrefSuffix = "mount-character-b";
        Check(!GameHud.MobileFixedState(Mount).Available, "other character has no default up binding");
        CharData.riding = true; Check(!GameHud.MobileFixedState(Mount).Available, "other character has no default down binding");
        Check(GameHud.BindMobileMountSkill(true, up.id), "down explicitly permits mount skill");
        CharData.riding = false; Check(GameHud.BindMobileMountSkill(false, up.id), "other character can independently bind up");
        CharData.PrefSuffix = "mount-character-a";
        GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.lastSkill == up.id, "first character up persists");
        CharData.riding = true; GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.lastSkill == down.id, "first character down persists independently");
        CharData.riding = false; CharData.rideAnchorId = 0; Time.unscaledTime += 3;
        TestManualSaddleBinding();
    }

    static void TestManualSaddleBinding()
    {
        const int saddleId = 25038;
        InventoryDlg.current = new InventoryDlg.Item { id = 987654, bagIdx = 0, count = 1, name = "鞍具", desc = "上坐骑" };
        Check(!GameHud.CanBindMobileSaddle(0, InventoryDlg.current) && !GameHud.BindMobileSaddle(0, 0, 987654), "spoofed saddle name and description cannot bind");
        InventoryDlg.current = new InventoryDlg.Item { id = saddleId, bagIdx = 0, count = 1, name = "鞍具" };
        Check(GameHud.CanBindMobileSaddle(0, InventoryDlg.current), "authoritative saddle id accepted");
        Check(GameHud.CanBindMobileSaddle(1, InventoryDlg.current) && GameHud.CanBindMobileSaddle(3, InventoryDlg.current), "saddle catalog eligibility covers both other inventory tabs");
        Check(!GameHud.CanBindMobileSaddle(2, InventoryDlg.current), "equipment tab cannot bind saddle");
        Check(!GameHud.CanBindMobileSaddle(-1, InventoryDlg.current) && !GameHud.CanBindMobileSaddle(4, InventoryDlg.current), "out-of-range tabs cannot bind saddle");
        InventoryDlg.current.locked = true; Check(!GameHud.BindMobileSaddle(0, 0, saddleId), "locked saddle cannot bind"); InventoryDlg.current.locked = false;
        InventoryDlg.current.blocked = 1; Check(!GameHud.BindMobileSaddle(0, 0, saddleId), "blocked saddle cannot bind"); InventoryDlg.current.blocked = 0;
        Check(!GameHud.BindMobileSaddle(0, 0, saddleId + 1), "stale item id cannot bind");
        int uses = NetClient.I.itemUses, writes = NetClient.I.writes, mountUses = NetClient.I.mountUses;
        var bag = new InventoryDlg(); bag.Select(); GUI.labels.Clear(); GUI.click = "设为上坐骑"; bag.Draw();
        Check(GUI.labels.Contains("设为上坐骑") && GameHud.MobileFixedState(Mount).ItemId == saddleId, "actual bag menu binds saddle up role");
        Check(NetClient.I.itemUses == uses && NetClient.I.writes == writes && NetClient.I.mountUses == mountUses, "saddle bind does not use item or change server hotbar");
        CharData.petRidable = false;
        Check(GameHud.MobileFixedState(Mount).Available, "saddle eligibility does not incorrectly require petRidable");
        CharData.petModel = 0; Check(!GameHud.MobileFixedState(Mount).Available, "saddle still needs a current pet"); CharData.petModel = 12;
        InventoryDlg.current.bagIdx = 7;
        GameHud.UseMobileFixedAction(Mount);
        Check(NetClient.I.itemUses == uses + 1 && NetClient.I.lastItem == saddleId && NetClient.I.lastSlot == 7 && !CharData.riding, "saddle dispatch resolves moved item and leaves riding state unchanged");
        GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.itemUses == uses + 1, "saddle request has repeat guard");
        Time.unscaledTime += 3;
        InventoryDlg.current.locked = true; Check(!GameHud.MobileFixedState(Mount).Available, "locked selected saddle blocks use"); InventoryDlg.current.locked = false;
        InventoryDlg.current.blocked = 1; Check(!GameHud.MobileFixedState(Mount).Available, "blocked selected saddle blocks use"); InventoryDlg.current.blocked = 0;
        InventoryDlg.current.count = 0; Check(!GameHud.MobileFixedState(Mount).Available, "depleted selected saddle blocks use");
        InventoryDlg.current = new InventoryDlg.Item { id = 987654, count = 1, name = "鞍具" };
        Check(!GameHud.MobileFixedState(Mount).Available && GameHud.MobileFixedState(Mount).ItemId == saddleId, "missing saddle stays selected with no replacement");
        GameHud.UseMobileFixedAction(Mount); Check(NetClient.I.itemUses == uses + 1, "missing saddle cannot consume other item");
        Check(NetClient.I.petShows == 0 && NetClient.I.rideLeaves == 0, "no saddle path invents pet show or leave ride");
    }

}
