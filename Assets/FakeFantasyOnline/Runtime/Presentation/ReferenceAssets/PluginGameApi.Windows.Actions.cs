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
        entry.ReceiptKind = "mail";
        entry.ReceiptAfter = ServerTextMark();
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
        if (!RackShopDlg.CatalogLoadedForPlugins)
            throw new ActionFailure("rack.catalog_not_loaded", RackShopDlg.Visible
                ? "The rack catalog is still loading. Observe ui, then retry rack.buy."
                : "The rack catalog is not loaded. Run ui.open {\"window\":\"rack\"}, wait for it to load, then retry rack.buy.");
        if (!RackShopDlg.HasGoodForPlugins(itemId))
            throw new ActionFailure("rack.item_not_found", "itemId " + itemId + " is not in the loaded rack catalog.");
        string error;
        entry.ReceiptKind = "rack";
        entry.ReceiptAfter = ServerTextMark();
        if (!RackShopDlg.TryBuyForPlugins(itemId, count, source, out error))
            throw new ActionFailure("rack.buy_failed", error ?? "The native rack purchase could not start.");
        AttachNativeWrite(entry, "rack", itemId, "The official BuyRack command did not produce an observed gateway task.");
    }

    private void DispatchFamilyCreate(PluginAction entry)
    {
        string name = ActionString(entry.Request.Arguments, "name");
        string declaration = ActionString(entry.Request.Arguments, "declaration", false);
        string error;
        entry.ReceiptKind = "family";
        entry.ReceiptAfter = ServerTextMark();
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

    // The server reports mail/rack/family outcomes only as ordered 0x8008 server texts
    // (session.sendGMText / scene ServerNotice). Correlate the first matching text received
    // after this action's write, in arrival order.
    private long ServerTextMark()
    {
        WorldNpcDialogTextEdgeWindow window = Session?.NpcDialogTextEdgeWindow;
        if (window == null) return 0;
        return window.Edges.Count > 0 ? window.Edges[window.Edges.Count - 1].Revision : window.DroppedThroughRevision;
    }

    // true = server success, false = server refusal, null = unrelated text.
    private static bool? ClassifyServerReceipt(string kind, string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        switch (kind)
        {
            case "mail":
                if (text == "邮件发送成功") return true;
                if (text.StartsWith("邮件发送", StringComparison.Ordinal) || text.StartsWith("邮件系统暂不可用", StringComparison.Ordinal) ||
                    text.StartsWith("邮件内容、金额或附件无效", StringComparison.Ordinal) || text == "不能给自己发送邮件") return false;
                return null;
            case "rack":
                if (text.StartsWith("购买成功", StringComparison.Ordinal)) return true;
                if (text.StartsWith("购买失败", StringComparison.Ordinal) || text.StartsWith("货架操作", StringComparison.Ordinal) ||
                    text.StartsWith("货架购买暂不可用", StringComparison.Ordinal)) return false;
                return null;
            case "family":
                if (text.StartsWith("家族创建成功", StringComparison.Ordinal)) return true;
                if (text.StartsWith("创建家族失败", StringComparison.Ordinal) || text.StartsWith("家族功能暂不可用", StringComparison.Ordinal) ||
                    text.StartsWith("操作失败", StringComparison.Ordinal) || text.StartsWith("操作超时", StringComparison.Ordinal)) return false;
                return null;
        }
        return null;
    }

    private void PollServerReceipt(PluginAction entry)
    {
        if (!entry.ReceiptWaiting) return;
        WorldNpcDialogTextEdgeWindow window = Session?.NpcDialogTextEdgeWindow;
        if (window != null)
        {
            foreach (WorldNpcDialogTextEdge edge in window.Edges)
            {
                if (edge.Revision <= entry.ReceiptAfter) continue;
                entry.ReceiptAfter = edge.Revision;
                bool? ok = ClassifyServerReceipt(entry.ReceiptKind, edge.Text);
                if (!ok.HasValue) continue;
                entry.ReceiptWaiting = false;
                var data = new Dictionary<string, object> { { "serverOutcomeKnown", true }, { "completionScope", "server" }, { "serverText", edge.Text } };
                if (ok.Value)
                {
                    if (entry.ReceiptKind == "mail")
                    {
                        MailDlg.Hide();
                        data["mailWindowClosed"] = !MailDlg.Visible;
                    }
                    ActionSet(entry, GameActionState.Completed, entry.ReceiptKind + ".completed", edge.Text, data);
                }
                else ActionSet(entry, GameActionState.Rejected, entry.ReceiptKind + ".failed", edge.Text, data);
                return;
            }
        }
        if (Session == null || Session.State != ClientSessionState.InWorld)
        {
            entry.ReceiptWaiting = false;
            ActionSet(entry, GameActionState.Unknown, "scene.changed", "The session left the world before the server receipt arrived. Observe state before retrying.");
            return;
        }
        if (ActionNow >= entry.Deadline)
        {
            entry.ReceiptWaiting = false;
            ActionSet(entry, GameActionState.Unknown, "receipt.timeout", "The write succeeded but no matching server receipt arrived in time. Observe state before retrying.");
        }
    }

    // Same call as SysOptionDlg 选择角色 / 选择服务器: GameBootstrap.ReturnToTitle.
    private void DispatchLogout(PluginAction entry)
    {
        var args = entry.Request.Arguments;
        string target = args != null && args.ContainsKey("target") ? ActionString(args, "target") :
            args != null && args.ContainsKey("characterSelection") && !ActionBool(args, "characterSelection", true) ? "server" : "character";
        if (target != "character" && target != "server" && target != "login")
            throw new ActionFailure("session.invalid_target", "target must be character, server or login.");
        bool characterSelection = target == "character";
        entry.LogoutTarget = target;
        ClientSessionState? current = Session?.State;
        if (current != ClientSessionState.InWorld)
        {
            // Already on a title page: only the account switch (native server-page return to login) applies.
            if (target != "login" || !TitleReturnableToLogin)
                throw new ActionFailure("session.unavailable", "An active world session is required, or use target login from the server or character selection page.");
            entry.Kind = "logout";
            entry.Deadline = ActionNow + 20d;
            entry.LogoutLoginRequested = true;
            Session.ReturnToLogin();
            ActionSet(entry, GameActionState.Running, "session.returning", "The native return to the login page started.");
            return;
        }
        if (GameBootstrap.I == null) throw new ActionFailure("session.unavailable", "The native world bootstrap is unavailable.");
        if (SysMsgBox.Visible) throw new ActionFailure("session.modal_open", "A native confirmation box is open; resolve it before leaving the world.");
        entry.Kind = "logout";
        entry.LogoutCharacterSelection = characterSelection;
        entry.Deadline = ActionNow + 20d;
        SysOptionDlg.Hide();
        GameBootstrap.I.ReturnToTitle(characterSelection);
        ActionSet(entry, GameActionState.Running, "session.returning", "The official return-to-title flow started.");
    }

    private bool TitleReturnableToLogin
    {
        get
        {
            ClientSessionState? state = Session?.State;
            var title = TitleBridge?.ReadState();
            return (state == ClientSessionState.ServerSelection || state == ClientSessionState.CharacterSelection) &&
                title != null && !title.LoginLoading && !title.Entering && !title.ModalOpen && !titleSubmissionInProgress;
        }
    }

    private void PollLogout(PluginAction entry)
    {
        if (!ActionActive(entry)) return;
        ClientSessionState? state = Session?.State;
        if (entry.LogoutTarget == "login")
        {
            if (!entry.LogoutLoginRequested && state == ClientSessionState.ServerSelection && TitleBridge?.ReadState()?.Page == "ServerSelection")
            {
                entry.LogoutLoginRequested = true;
                Session.ReturnToLogin();
            }
            if (state == ClientSessionState.Offline && TitleBridge?.ReadState()?.Page == "Login")
            {
                ActionSet(entry, GameActionState.Completed, "session.returned", "The native session reached the login page.",
                    new Dictionary<string, object> { { "sessionState", state.ToString() }, { "titlePage", "Login" }, { "completionScope", "title" } });
                return;
            }
        }
        bool done = entry.LogoutTarget != "login" && entry.LogoutCharacterSelection
            ? state == ClientSessionState.CharacterSelection
            : state == ClientSessionState.ServerSelection || state == ClientSessionState.Offline;
        if (done)
        {
            ActionSet(entry, GameActionState.Completed, "session.returned", "The native session left the world and reached the requested title page.",
                new Dictionary<string, object> { { "sessionState", state.ToString() }, { "completionScope", "title" } });
            return;
        }
        if (state == ClientSessionState.Failed)
        {
            ActionSet(entry, GameActionState.Rejected, "session.failed", "The native session failed while returning to the title.");
            return;
        }
        if (ActionNow >= entry.Deadline)
            ActionSet(entry, GameActionState.TimedOut, "action.timeout", "The title page was not reached before the deadline. Observe title.");
    }
}
}
