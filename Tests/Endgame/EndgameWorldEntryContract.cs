using System;
using System.Collections.Generic;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

// Executes real panel entry partials; native bag and NPC menu wiring are checked separately.
class EndgameWorldEntryContract
{
    static int assertions;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); assertions++; }
    sealed class Client : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld { get; set; } = true;
        public List<EndgameRequest> Sent = new List<EndgameRequest>();
        public bool SendEndgame(EndgameRequest request)
        { if (!Endgame.Begin(request)) return false; Sent.Add(request); return true; }
        public int Retries; public EndgameRequest Retried;
        public bool RetryEndgame() { Retries++; Retried = Endgame.PendingRequest; return Endgame.Pending; }
        public bool EquipEndgameItem(EndgameItemView item) => false;
        public bool UnequipEndgameItem(EndgameItemView item) => false;
    }
    static object Get(EndgamePanel p, string name) => typeof(EndgamePanel).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(p);
    static void Update(EndgamePanel p) => typeof(EndgamePanel).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(p, null);
    static void Negotiate(Client c)
    {
        c.Endgame.Begin(new EndgameRequest("hello"));
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.cube-inputs.v1\"],\"result\":{\"cubeItemId\":\"1900028301\"}}"));
    }
    const string Entrance = "{\"mapId\":7,\"npcId\":83,\"npc\":\"八仙使者\",\"script\":\"Nbaxian\"}";
    static void EntranceHello(Client c, string entrance = Entrance, bool capability = true)
    {
        c.Endgame.Begin(new EndgameRequest("hello"));
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"" + (capability ? ",\"endgame.rift-entrance.v1\"" : "") + "],\"riftEntrances\":[" + entrance + "]}"));
    }
    static void Reject(string json, string label)
    {
        bool rejected = false; try { EndgameResponse.Parse(json); } catch (FormatException) { rejected = true; }
        Check(rejected, label);
    }
    static void EntranceContracts()
    {
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); GameBootstrap.CurMapId = 7;
        var npc = new WorldNpcState { EntityId = -1, DisplayName = "八仙使者", Script = "Nbaxian" };
        GameBootstrap.I = new GameBootstrap(); GameBootstrap.I.Window.Current = npc;
        var dialog = new DialogUI(); Negotiate(c); dialog.OpenForTest(npc);
        Check(dialog.Options.Count == 1, "legacy hello preserves original services without Rift entry");
        EntranceHello(c, capability: false); dialog.OpenForTest(npc);
        Check(dialog.Options.Count == 1 && c.Endgame.RiftEntrances.Count == 0, "unnegotiated bindings grant no entry");
        EntranceHello(c); dialog.OpenForTest(npc);
        Check(dialog.Options.Count == 2 && dialog.Options[0].Label == "原生任务和服务", "configured entrance appends Rift without replacing services");
        Check(dialog.RiftEntranceEntityForIndependentRuntime(c.Endgame) == -1, "native and plugin share unsigned live entity identity");
        int before = c.Sent.Count; dialog.Options[1].Action();
        Check(dialog.Closed && (bool)Get(p, "open") && c.Sent.Count > before && !c.Sent.Exists(r => r.Mutates), "native dialog opens Rift as read-only view");
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"rift.state\",\"ok\":true,\"rift\":{\"phase\":\"closed\",\"keyCount\":1}}"));
        ArtworkSkin.click = "消耗一钥 · 开启洞天";
        typeof(EndgamePanel).GetMethod("DrawRiftActions", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(p, new object[] { c.Endgame.Rift, true });
        Check(c.Endgame.PendingRequest != null && c.Endgame.PendingRequest.Json.Contains("\"npcEntityId\":4294967295"), "paid entry sends exact selected live uint32 NPC ID");
        string frozen = c.Endgame.PendingRequest.Json;
        GameBootstrap.CurMapId = 8; Update(p);
        Check((int)Get(p, "riftEntranceEntityID") == 0 && c.Endgame.PendingRequest.Json == frozen, "map departure discards entrance but preserves uncertain request");
        GameBootstrap.CurMapId = 7;
        Check(!EndgamePanel.RiftEntranceAvailable(8, -1, npc.DisplayName, npc.Script), "mismatched map cannot expose entry");
        Check(!EndgamePanel.RiftEntranceAvailable(7, 0, npc.DisplayName, npc.Script), "missing native entity cannot expose entry");
        Check(!EndgamePanel.RiftEntranceAvailable(7, -1, "妖魔结界守卫", npc.Script), "old guard is not the configured entrance");
        Check(!EndgamePanel.RiftEntranceAvailable(7, -1, npc.DisplayName + "分身", npc.Script), "name comparison is exact");
        Check(!EndgamePanel.RiftEntranceAvailable(7, -1, npc.DisplayName, "nbaxian"), "native script comparison is exact");
        Check(!EndgamePanel.RiftEntranceAvailable(7, -1, npc.DisplayName, "Nbaxian_ffo"), "portrait alias cannot replace raw network NPC script identity");
        dialog.OpenForTest(npc); var oldOption = dialog.Options[1];
        GameBootstrap.I.Window.Current = new WorldNpcState { EntityId = -1, DisplayName = npc.DisplayName, Script = npc.Script };
        before = c.Sent.Count; oldOption.Action();
        Check(c.Sent.Count == before && dialog.Message != null && dialog.RiftEntranceEntityForIndependentRuntime(c.Endgame) == 0, "replacement actor with reused ID invalidates old dialog before click");
        GameBootstrap.I.Window.Current = npc; dialog.OpenForTest(npc); oldOption = dialog.Options[1];
        c.Endgame.Begin(new EndgameRequest("hello"));
        Check(c.Endgame.RiftEntrances.Count == 0 && dialog.RiftEntranceEntityForIndependentRuntime(c.Endgame) == 0, "new hello immediately discards entrance authority");
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":false,\"error\":\"storage\"}"));
        Check(c.Endgame.Negotiated && c.Endgame.RiftEntrances.Count == 0, "failed refresh preserves renderer but not entrance binding");
        EntranceHello(c); before = c.Sent.Count; oldOption.Action();
        Check(c.Sent.Count == before && dialog.RiftEntranceEntityForIndependentRuntime(c.Endgame) == 0, "renegotiation cannot revive old dialog authority");
        dialog.OpenForTest(npc); dialog.Options[1].Action();
        EndgamePanel.TryOpenRiftFromWorld();
        Check((int)Get(p, "riftEntranceEntityID") == 0, "HUD review never inherits NPC admission identity");
        c.Endgame.Reset();
        Check(c.Endgame.RiftEntrances.Count == 0 && dialog.RiftEntranceEntityForIndependentRuntime(c.Endgame) == 0, "reset clears bindings and source identity");
        string generic = Entrance.Replace("八仙使者", "测试使者").Replace("Nbaxian", "Ncustom");
        EntranceHello(c, generic); npc.DisplayName = "测试使者"; npc.Script = "Ncustom"; dialog.OpenForTest(npc);
        Check(dialog.Options.Count == 2, "any exact server-configured NPC can expose entrance without a name special case");
        foreach (string invalid in new[] { Entrance.Replace("7", "0"), Entrance.Replace("83", "0"), Entrance.Replace("Nbaxian", ""), Entrance.Replace("八仙使者", ""), Entrance.Replace("83", "\"83\""), Entrance.Replace("{", "{\"unknown\":1,") })
            Reject("{\"kind\":\"hello\",\"riftEntrances\":[" + invalid + "]}", "malformed entrance rejects entire DTO");
        Reject("{\"kind\":\"hello\",\"riftEntrances\":[" + string.Join(",", System.Linq.Enumerable.Repeat(Entrance, 33)) + "]}", "entrance list is bounded");
        bool invalidCommand = false; try { new EndgameRequest("rift.claim", npcEntityID: 5); } catch (ArgumentException) { invalidCommand = true; }
        Check(invalidCommand && !new EndgameRequest("rift.state").Json.Contains("npcEntityId"), "rift review and unrelated mutation cannot carry NPC identity");
        p.Unbind(c); ArtworkSkin.click = null;
    }
    static void CubeRecoveryContracts()
    {
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); GameBootstrap.CurMapId = 7;
        InventoryDlg.rows.Remove(7);
        var npc = new WorldNpcState { EntityId = -1, DisplayName = "游方巧匠", Script = "Ncustom" };
        GameBootstrap.I = new GameBootstrap(); GameBootstrap.I.Window.Current = npc;
        var dialog = new DialogUI();
        Negotiate(c); dialog.OpenForTest(npc);
        Check(dialog.Options.Count == 1, "no server recovery NPC means no invented recovery menu");
        c.Endgame.Begin(new EndgameRequest("hello"));
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.cube-inputs.v1\"],\"result\":{\"cubeItemId\":\"1900028301\",\"cubeNPC\":\"游方巧匠\",\"cubeLore\":\"旧鼎随身，星河入袖。\\n二曰辨物。\\n三曰借火。\\n四曰归真。\"}}"));
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"recover\",\"chunks\":1,\"revision\":\"42\"}"));
        dialog.OpenForTest(npc);
        Check(dialog.Options.Count == 3 && dialog.Options[0].Label == "原生任务和服务" && dialog.Options[1].Label == "关于周天鼎" && dialog.Options[2].Label == "寻回周天鼎",
            "configured NPC appends lore and recovery without a scene-workbench entry");
        dialog.Options[1].Action();
        Check(dialog.Message == "旧鼎随身，星河入袖。" && !(bool)Get(p,"open"), "recovery NPC shows exact server-authored lore without opening cube");
        foreach (string passage in new[] { "二曰辨物。", "三曰借火。", "四曰归真。", "旧鼎随身，星河入袖。" })
        {
            dialog.Options[1].Action(); Check(dialog.Message == passage, "about action rotates one server-authored passage at a time");
        }
        Check(dialog.CubeWorkbenchEntityForIndependentRuntime(c.Endgame) == 0, "NPC can never lend scene authority to cube quote or execute");
        npc.DisplayName += "分身"; dialog.OpenForTest(npc); Check(dialog.Options.Count == 1, "recovery NPC uses exact configured name");
        npc.DisplayName = "游方巧匠"; dialog.OpenForTest(npc);
        int sent = c.Sent.Count; var stale = dialog.Options[2];
        GameBootstrap.CurMapId = 8; stale.Action();
        Check(c.Sent.Count == sent, "map departure invalidates recovery action"); GameBootstrap.CurMapId = 7;
        GameBootstrap.I.Window.Current = new WorldNpcState { EntityId = -1, DisplayName = npc.DisplayName, Script = npc.Script }; stale.Action();
        Check(c.Sent.Count == sent, "replacement actor with reused ID invalidates recovery action");
        GameBootstrap.I.Window.Current = npc; dialog.OpenForTest(npc); dialog.Options[2].Action();
        var request = c.Endgame.PendingRequest;
        Check(dialog.Closed && !(bool)Get(p,"open") && request != null && request.Op == "cube.recover" && request.Mutates && request.Json.Contains("\"npcEntityId\":4294967295") && request.Json.Contains("\"expectedRevision\":\"42\""),
            "recover sends exact live NPC and authoritative revision without opening workbench");
        sent = c.Sent.Count; int retries = c.Retries; string frozen = request.Json;
        stale.Action(); Check(c.Sent.Count == sent && c.Retries == retries + 1 && ReferenceEquals(c.Endgame.PendingRequest,request) && ReferenceEquals(c.Retried,request), "repeated recovery queries the exact pending request without a new nonce");
        dialog.OpenForTest(npc); Check(dialog.Options[2].Label == "查询寻回结果" && !EndgamePanel.CubeItemAvailableForIndependentRuntime(c.Endgame), "missing cube still has an explicit recovery-result query menu");
        dialog.Options[2].Action();
        Check(dialog.Closed && c.Retries == retries + 2 && c.Sent.Count == sent && ReferenceEquals(c.Retried,request) && request.Json == frozen, "recovery query retains exact original NPC revision and request ID");
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"cube.recover\",\"ok\":true,\"requestId\":\"" + request.RequestID + "\",\"revision\":\"43\"}"));
        Check(!c.Endgame.Pending, "correlated recovery result releases pending transaction");
        var unrelated = new EndgameRequest("rift.start", "43", tier: 1); c.Endgame.Begin(unrelated); retries = c.Retries;
        dialog.OpenForTest(npc); dialog.Options[2].Action();
        Check(!dialog.Closed && c.Retries == retries && c.Sent.Count == sent && ReferenceEquals(c.Endgame.PendingRequest,unrelated), "recovery menu cannot retry or replace another pending mutation");
        dialog.OpenForTest(npc); stale = dialog.Options[2]; c.Endgame.Reset(); Negotiate(c); stale.Action();
        Check(c.Sent.Count == sent, "reconnect never revives a captured recovery menu");
        p.Unbind(c);
    }
    static void PowerBookRecoveryContracts()
    {
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); GameBootstrap.CurMapId = 7;
        var npc = new WorldNpcState { EntityId = 73, DisplayName = "巧匠左慈", Script = "N611FY" };
        GameBootstrap.I = new GameBootstrap(); GameBootstrap.I.Window.Current = npc;
        c.Endgame.Begin(new EndgameRequest("hello"));
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"],\"result\":{\"powerBook\":\"1\",\"powerBookItem\":\"1\",\"powerCodexItemId\":\"1900028302\",\"powerScrollItemId\":\"1900028303\",\"powerBookNPC\":\"巧匠左慈\",\"powerBookName\":\"万象秘典\"}}"));
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"recover-codex\",\"chunks\":1,\"revision\":\"42\"}"));
        var dialog = new DialogUI(); dialog.OpenForTest(npc);
        Check(dialog.Options.Count == 2 && dialog.Options[1].Label == "寻回万象秘典", "book recovery is configured independently of cube, without replacing native quests");
        var stale = dialog.Options[1]; int sent = c.Sent.Count;
        GameBootstrap.CurMapId = 8; stale.Action(); Check(c.Sent.Count == sent, "book recovery rejects departed map");
        GameBootstrap.CurMapId = 7; GameBootstrap.I.Window.Current = new WorldNpcState { EntityId = 73, DisplayName = npc.DisplayName, Script = npc.Script };
        stale.Action(); Check(c.Sent.Count == sent, "book recovery rejects replaced NPC with reused identity");
        GameBootstrap.I.Window.Current = npc; dialog.OpenForTest(npc); dialog.Options[1].Action();
        var request = c.Endgame.PendingRequest;
        Check(request != null && request.Op == "powerbook.recover" && request.Json.Contains("\"npcEntityId\":73"), "book recovery carries exact NPC and immutable revision");
        dialog.OpenForTest(npc); dialog.Options[1].Action();
        Check(c.Retries == 1 && ReferenceEquals(c.Retried, request), "recovery retry reuses same request, not duplicate grant");
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"powerbook.result\",\"ok\":true,\"requestId\":\"" + request.RequestID + "\",\"revision\":\"43\"}"));
        Check(!c.Endgame.Pending, "book recovery result clears matching pending request");
        p.Unbind(c);
    }
    static void Main()
    {
        Check(!EndgamePanel.WorldEntryAvailable && !EndgamePanel.TryOpenRiftFromWorld() && !EndgamePanel.TryOpenCubeFromItem(1,7), "unbound entry is unavailable");
        var p = new EndgamePanel(); var c = new Client(); p.Bind(c);
        InventoryDlg.rows[7] = new InventoryDlg.Item { id=1900028301, bagIdx=7, count=1, name="周天鼎" }; InventoryDlg.rowTabs[7] = 1;
        Check(!EndgamePanel.TryOpenRiftFromWorld() && !EndgamePanel.TryOpenCubeFromWorld() && !EndgamePanel.TryOpenCubeFromItem(1,7) && c.Sent.Count == 0, "legacy server entry sends nothing");
        Negotiate(c); c.EndgameInWorld = false;
        Check(!EndgamePanel.WorldEntryAvailable && !EndgamePanel.TryOpenRiftFromWorld() && !EndgamePanel.TryOpenCubeFromItem(1,7) && c.Sent.Count == 0, "out-of-world entry sends nothing");
        c.EndgameInWorld = true;
        Check(EndgamePanel.WorldEntryAvailable && EndgamePanel.TryOpenRiftFromWorld(), "HUD opens negotiated rift review");
        Check((bool)Get(p, "open") && (EndgameModule)Get(p, "module") == EndgameModule.Rift, "HUD opens only rift controls");
        Check((long)Get(p, "generation") == c.Endgame.Generation, "world open synchronizes generation before first update");
        Check(c.Sent.Exists(r => r.Op == "snapshot") && c.Sent.Exists(r => r.Op == "rift.state"), "world open requests real snapshot and run");
        Check(c.Sent.TrueForAll(r => r.Op == "snapshot" || r.Op == "rift.state" || r.Op == "catalog"), "entry does not spend keys or authorize mutations");
        Update(p); Check((bool)Get(p, "open"), "first update keeps explicitly opened panel visible");
        Check(!EndgamePanel.TryOpenCubeFromWorld(), "generic world opener cannot bypass owned cube item");
        GameBootstrap.CurMapId = 506;
        Check(!EndgamePanel.TryOpenCubeFromNpc(7,77,"Nzhoutianding",()=>true) && !EndgamePanel.TryOpenCubeFromNpc(506,0,"Nzhoutianding",()=>true) &&
            !EndgamePanel.TryOpenCubeFromNpc(506,77,"Nartisan",()=>true) && !EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>false), "all retired scene cube entry routes remain rejected");
        Check(!EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>true), "exact former scene NPC no longer opens cube");
        Check(EndgamePanel.TryOpenCubeFromItem(1,7) && (EndgameModule)Get(p, "module") == EndgameModule.Cube, "native bag item opens only cube controls");
        Check(!c.Sent.Exists(r => r.Catalog == "recipes"), "ingredient-driven cube does not load recipe menu catalog");
        var pending = new EndgameRequest("rift.start", tier: 1); c.SendEndgame(pending);
        Check(EndgamePanel.TryOpenRiftFromWorld() && ReferenceEquals(c.Endgame.PendingRequest, pending), "opening a view preserves uncertain mutation identity");
        c.Endgame.Reset(); int before = c.Sent.Count;
        Check(!EndgamePanel.TryOpenCubeFromWorld() && !EndgamePanel.TryOpenCubeFromItem(1,7) && c.Sent.Count == before, "owned bag item cannot bypass reset negotiation");
        Update(p); Check(!(bool)Get(p, "open"), "reset closes old view");
        Negotiate(c);
        Check(EndgamePanel.TryOpenCubeFromItem(1,7), "renegotiated native cube item opens before update");
        Update(p); Check((bool)Get(p, "open"), "renegotiated open survives update");
        UnityEngine.Input.pressed = UnityEngine.KeyCode.F8; Update(p);
        Check((bool)Get(p, "open"), "F8 cannot toggle an item-opened workbench");
        UnityEngine.Input.pressed = UnityEngine.KeyCode.Escape; Update(p);
        Check(!(bool)Get(p, "open"), "Escape closes standalone workbench");
        UnityEngine.Input.pressed = UnityEngine.KeyCode.F8; Update(p);
        Check(!(bool)Get(p, "open"), "F8 cannot open a universal panel");
        Check(!Enum.IsDefined(typeof(EndgameModule), "Runes") && typeof(EndgamePanel).GetMethod("TryOpenRunesFromWorld", BindingFlags.NonPublic | BindingFlags.Static) == null, "rune controls exist only in the original furnace");
        Check(!c.Sent.Exists(r => r.Catalog == "uniques" || r.Catalog == "formations"), "module entries never open encyclopedias");
        UnityEngine.Input.pressed = null;
        p.Unbind(c); before = c.Sent.Count;
        Check(!EndgamePanel.TryOpenRiftFromWorld() && c.Sent.Count == before, "detached row sends nothing");
        EntranceContracts(); CubeRecoveryContracts(); PowerBookRecoveryContracts();
        Console.WriteLine("PASS " + assertions + " bag/NPC world-entry contracts; actual panel logic, deterministic Unity substitutes.");
    }
}
