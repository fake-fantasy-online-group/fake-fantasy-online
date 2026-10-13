using System;
using System.Collections.Generic;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

// Executes the real EndgamePanel partials; NPC menu wiring is checked separately below.
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
        public bool RetryEndgame() => false;
        public bool EquipEndgameItem(EndgameItemView item) => false;
        public bool UnequipEndgameItem(EndgameItemView item) => false;
    }
    static object Get(EndgamePanel p, string name) => typeof(EndgamePanel).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(p);
    static void Update(EndgamePanel p) => typeof(EndgamePanel).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(p, null);
    static void Negotiate(Client c)
    {
        c.Endgame.Begin(new EndgameRequest("hello"));
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.cube-inputs.v1\"]}"));
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
        Check(invalidCommand && !new EndgameRequest("rift.state").Json.Contains("npcEntityId"), "NPC identity is restricted to rift.start");
        p.Unbind(c); ArtworkSkin.click = null;
    }
    static void Main()
    {
        Check(!EndgamePanel.WorldEntryAvailable && !EndgamePanel.TryOpenRiftFromWorld(), "unbound entry is unavailable");
        var p = new EndgamePanel(); var c = new Client(); p.Bind(c);
        Check(!EndgamePanel.TryOpenRiftFromWorld() && !EndgamePanel.TryOpenCubeFromWorld() && c.Sent.Count == 0, "legacy server entry sends nothing");
        Negotiate(c); c.EndgameInWorld = false;
        Check(!EndgamePanel.WorldEntryAvailable && !EndgamePanel.TryOpenRiftFromWorld() && c.Sent.Count == 0, "out-of-world entry sends nothing");
        c.EndgameInWorld = true;
        Check(EndgamePanel.WorldEntryAvailable && EndgamePanel.TryOpenRiftFromWorld(), "HUD opens negotiated rift review");
        Check((bool)Get(p, "open") && (EndgameModule)Get(p, "module") == EndgameModule.Rift, "HUD opens only rift controls");
        Check((long)Get(p, "generation") == c.Endgame.Generation, "world open synchronizes generation before first update");
        Check(c.Sent.Exists(r => r.Op == "snapshot") && c.Sent.Exists(r => r.Op == "rift.state"), "world open requests real snapshot and run");
        Check(c.Sent.TrueForAll(r => r.Op == "snapshot" || r.Op == "rift.state" || r.Op == "catalog"), "entry does not spend keys or authorize mutations");
        Update(p); Check((bool)Get(p, "open"), "first update keeps NPC-opened panel visible");
        Check(!EndgamePanel.TryOpenCubeFromWorld(), "generic world opener cannot bypass cube NPC identity");
        GameBootstrap.CurMapId = 506;
        Check(!EndgamePanel.TryOpenCubeFromNpc(7,77,"Nzhoutianding",()=>true) && !EndgamePanel.TryOpenCubeFromNpc(506,0,"Nzhoutianding",()=>true) &&
            !EndgamePanel.TryOpenCubeFromNpc(506,77,"Nartisan",()=>true) && !EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>false), "cube entry requires current map, entity, exact script and live source");
        Check(EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>true) && (EndgameModule)Get(p, "module") == EndgameModule.Cube, "native cube NPC opens only cube controls");
        Check(!c.Sent.Exists(r => r.Catalog == "recipes"), "ingredient-driven cube does not load recipe menu catalog");
        var pending = new EndgameRequest("rift.start", tier: 1); c.SendEndgame(pending);
        Check(EndgamePanel.TryOpenRiftFromWorld() && ReferenceEquals(c.Endgame.PendingRequest, pending), "opening a view preserves uncertain mutation identity");
        c.Endgame.Reset(); int before = c.Sent.Count;
        Check(!EndgamePanel.TryOpenCubeFromWorld() && c.Sent.Count == before, "stale NPC option cannot bypass reset negotiation");
        Update(p); Check(!(bool)Get(p, "open"), "reset closes old view");
        Negotiate(c);
        Check(EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>true), "renegotiated native cube entry opens before update");
        Update(p); Check((bool)Get(p, "open"), "renegotiated open survives update");
        UnityEngine.Input.pressed = UnityEngine.KeyCode.F8; Update(p);
        Check((bool)Get(p, "open"), "F8 cannot toggle an NPC-opened workbench");
        UnityEngine.Input.pressed = UnityEngine.KeyCode.Escape; Update(p);
        Check(!(bool)Get(p, "open"), "Escape closes standalone workbench");
        UnityEngine.Input.pressed = UnityEngine.KeyCode.F8; Update(p);
        Check(!(bool)Get(p, "open"), "F8 cannot open a universal panel");
        Check(!Enum.IsDefined(typeof(EndgameModule), "Runes") && typeof(EndgamePanel).GetMethod("TryOpenRunesFromWorld", BindingFlags.NonPublic | BindingFlags.Static) == null, "rune controls exist only in the original furnace");
        Check(!c.Sent.Exists(r => r.Catalog == "uniques" || r.Catalog == "formations"), "module entries never open encyclopedias");
        UnityEngine.Input.pressed = null;
        p.Unbind(c); before = c.Sent.Count;
        Check(!EndgamePanel.TryOpenRiftFromWorld() && c.Sent.Count == before, "detached row sends nothing");
        EntranceContracts();
        Console.WriteLine("PASS " + assertions + " NPC world-entry contracts; actual panel logic, deterministic Unity substitutes.");
    }
}
