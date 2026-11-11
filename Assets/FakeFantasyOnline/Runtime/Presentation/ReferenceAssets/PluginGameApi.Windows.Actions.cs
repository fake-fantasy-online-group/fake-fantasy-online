using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.PluginApi;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class PluginGameApi
{
    private static Dictionary<string, object> PluginWindowSnapshot(int mapId)
    {
        var window = GameBootstrap.I?.Window;
        return new Dictionary<string, object>
        {
            { "mapId", mapId },
            { "dialogueVisible", DialogUI.VisibleS },
            { "mail", MailDlg.Visible },
            { "family", FamilyDlg.Visible },
            { "familyCreate", FamilyCreateDlg.Visible },
            { "rack", RackShopDlg.Visible },
            { "powerBook", EndgamePanel.PowerBookViewVisible },
            { "inventory", InventoryDlg.IsOpen },
            { "store", window?.StoreDialog?.Visible ?? false },
            { "transport", TransportUI.VisibleS }
        };
    }

    private void DispatchWindow(PluginAction entry, bool close)
    {
        string window = ActionString(entry.Request.Arguments, "window");
        if (string.IsNullOrEmpty(window)) throw new ArgumentException("window is required.");
        bool ok;
        switch (window)
        {
            case "mail":
                ok = SetToggled(MailDlg.Visible, close, MailDlg.Toggle);
                break;
            case "family":
                if (close) { FamilyDlg.Hide(); ok = !FamilyDlg.Visible; }
                else { FamilyDlg.OpenSelf(); ok = FamilyDlg.Visible; }
                break;
            case "familyCreate":
                ok = SetToggled(FamilyCreateDlg.Visible, close, FamilyCreateDlg.Toggle);
                break;
            case "rack":
                ok = SetToggled(RackShopDlg.Visible, close, RackShopDlg.Toggle);
                break;
            case "inventory":
                ok = SetToggled(InventoryDlg.IsOpen, close, InventoryDlg.Toggle);
                break;
            case "powerBook":
                if (close)
                {
                    ok = !EndgamePanel.PowerBookViewVisible || EndgamePanel.TryClosePowerBookForPlugins();
                }
                else ok = EndgamePanel.TryOpenPowerBookFromWorld();
                if (!ok && !close) throw new ActionFailure("ui.unavailable", "万象秘典 is not in the bag, or the endgame catalog is not negotiated.");
                break;
            default:
                throw new ActionFailure("ui.unknown", "Supported windows: mail, family, familyCreate, rack, powerBook, inventory.");
        }
        if (!ok) throw new ActionFailure("ui.failed", "The native window could not be opened or closed.");
        var data = PluginWindowSnapshot(GameBootstrap.CurMapId);
        data["window"] = window;
        ActionLocal(entry, close ? "ui.closed" : "ui.opened", close ? "The named native window was closed." : "The named native window was opened.", data);
    }

    private static bool SetToggled(bool visible, bool close, Action toggle)
    {
        if (visible == !close) return true;
        toggle();
        return true;
    }

    private void DispatchTransportChoose(PluginAction entry)
    {
        if (!TransportUI.VisibleS || TransportUI.I == null)
            throw new ActionFailure("transport.closed", "The native transport window is not open.");
        int index = ActionInt(entry.Request.Arguments, "index", 0, int.MaxValue);
        string expected = ActionString(entry.Request.Arguments, "expectedName");
        int startMap = GameBootstrap.CurMapId;
        entry.PriorWindows = PriorWindowFlags(PluginWindowSnapshot(startMap));
        if (!TransportUI.I.TryChooseDestinationForPlugins(index, expected))
            throw new ActionFailure("transport.stale", "The requested destination is not at this index with this name.");
        entry.Kind = "dialogue";
        entry.NativeId = startMap;
        entry.Deadline = ActionNow + 8d;
        var data = PluginWindowSnapshot(startMap);
        data["option"] = expected;
        data["index"] = index;
        ActionSet(entry, GameActionState.Running, "transport.dispatched", "The native Teleport command was issued. Waiting for a map change.", data);
    }

    private void PollDialogueOutcome(PluginAction entry)
    {
        var now = PluginWindowSnapshot(GameBootstrap.CurMapId);
        bool mapChanged = GameBootstrap.CurMapId != entry.NativeId && GameBootstrap.CurMapId != 0;
        bool windowOpened = WindowNewlyOpened(entry.PriorWindows, now, "transport") ||
            WindowNewlyOpened(entry.PriorWindows, now, "store") ||
            WindowNewlyOpened(entry.PriorWindows, now, "familyCreate") ||
            WindowNewlyOpened(entry.PriorWindows, now, "mail") ||
            WindowNewlyOpened(entry.PriorWindows, now, "rack") ||
            WindowNewlyOpened(entry.PriorWindows, now, "powerBook");
        if (mapChanged || (windowOpened && ActionNow + 0.2d < entry.Deadline) || ActionNow >= entry.Deadline)
        {
            now["serverOutcomeKnown"] = mapChanged;
            now["mapChanged"] = mapChanged;
            string code = mapChanged ? "dialogue.world_changed" : windowOpened ? "dialogue.window_opened" : "dialogue.no_server_change";
            string message = mapChanged
                ? "The native option produced a map change."
                : windowOpened
                    ? "The native option opened a function window. No map change was observed."
                    : "The native option dispatched, but no map change or new function window was observed before the wait ended.";
            ActionSet(entry, mapChanged || windowOpened ? GameActionState.Completed : GameActionState.Completed, code, message, now);
        }
    }

    private static bool IsTrue(Dictionary<string, object> data, string key) =>
        data.TryGetValue(key, out object value) && value is bool && (bool)value;

    private static Dictionary<string, bool> PriorWindowFlags(Dictionary<string, object> snapshot)
    {
        var flags = new Dictionary<string, bool>(StringComparer.Ordinal);
        if (snapshot == null) return flags;
        foreach (var pair in snapshot)
            if (pair.Value is bool) flags[pair.Key] = (bool)pair.Value;
        return flags;
    }

    private static bool WindowNewlyOpened(Dictionary<string, bool> prior, Dictionary<string, object> now, string key) =>
        IsTrue(now, key) && (prior == null || !prior.TryGetValue(key, out bool wasOpen) || !wasOpen);

    private void DispatchMailSend(PluginAction entry)
    {
        string to = ActionString(entry.Request.Arguments, "to");
        string title = ActionString(entry.Request.Arguments, "title");
        string body = ActionString(entry.Request.Arguments, "body");
        int paper = ActionInt(entry.Request.Arguments, "paper", 1, int.MaxValue);
        System.Threading.Tasks.Task<FakeFantasyOnline.Foundation.Result> pending;
        string error;
        if (!MailDlg.TrySendForPlugins(to, title, body, paper, out error, out pending))
            throw new ActionFailure("mail.send_failed", error ?? "The native mail composer could not send.");
        entry.Pending = pending;
        ActionSet(entry, GameActionState.Queued, "send.pending", "Native DoSend is pending; no successful write is asserted yet.");
    }

    private void DispatchRackBuy(PluginAction entry)
    {
        int itemId = ActionInt(entry.Request.Arguments, "itemId", 1, int.MaxValue);
        int count = 1;
        if (entry.Request.Arguments != null && entry.Request.Arguments.ContainsKey("count"))
            count = ActionInt(entry.Request.Arguments, "count", 1, 999);
        byte source = 0;
        if (entry.Request.Arguments != null && entry.Request.Arguments.ContainsKey("source"))
            source = (byte)ActionInt(entry.Request.Arguments, "source", 0, 255);
        string error;
        if (!RackShopDlg.TryBuyForPlugins(itemId, count, source, out error))
            throw new ActionFailure("rack.buy_failed", error ?? "The native rack purchase could not start.");
        AttachNativeWrite(entry, "rack", itemId, "The official BuyRack command did not produce an observed gateway task.");
    }

    private void DispatchFamilyCreate(PluginAction entry)
    {
        string name = ActionString(entry.Request.Arguments, "name");
        string declaration = ActionString(entry.Request.Arguments, "declaration", false);
        string error;
        if (!FamilyCreateDlg.TryCreateForPlugins(name, declaration, out error))
            throw new ActionFailure("family.create_failed", error ?? "The native family creation could not start.");
        AttachNativeWrite(entry, "family", 0, "The official FamilyCreate command did not produce an observed gateway task.");
    }

    private void AttachNativeWrite(PluginAction entry, string kind, int id, string missing)
    {
        var write = entry.Native != null ? entry.Native.ReadWriteForPlugins(kind, id) : null;
        if (write == null || write.Sequence <= entry.WriteSequence || write.Pending == null)
            throw new ActionFailure(kind + ".write_unobserved", missing);
        entry.Pending = write.Pending;
        ActionSet(entry, GameActionState.Queued, "send.pending", "The official native command is pending; no successful write is asserted yet.");
    }
}
}
