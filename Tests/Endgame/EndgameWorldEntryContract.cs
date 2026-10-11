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
        c.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}"));
    }
    static void Main()
    {
        Check(!EndgamePanel.WorldEntryAvailable && !EndgamePanel.TryOpenRiftFromWorld(), "unbound entry is unavailable");
        var p = new EndgamePanel(); var c = new Client(); p.Bind(c);
        Check(!EndgamePanel.TryOpenRiftFromWorld() && !EndgamePanel.TryOpenCubeFromWorld() && c.Sent.Count == 0, "legacy server entry sends nothing");
        Negotiate(c); c.EndgameInWorld = false;
        Check(!EndgamePanel.WorldEntryAvailable && !EndgamePanel.TryOpenRiftFromWorld() && c.Sent.Count == 0, "out-of-world entry sends nothing");
        c.EndgameInWorld = true;
        Check(EndgamePanel.WorldEntryAvailable && EndgamePanel.TryOpenRiftFromWorld(), "guardian opens negotiated rift view");
        Check((bool)Get(p, "open") && (int)Get(p, "tab") == 3, "guardian selects rift tab");
        Check((long)Get(p, "generation") == c.Endgame.Generation, "world open synchronizes generation before first update");
        Check(c.Sent.Exists(r => r.Op == "snapshot") && c.Sent.Exists(r => r.Op == "rift.state"), "world open requests real snapshot and run");
        Check(c.Sent.TrueForAll(r => r.Op == "snapshot" || r.Op == "rift.state" || r.Op == "catalog"), "entry does not spend keys or authorize mutations");
        Update(p); Check((bool)Get(p, "open"), "first update keeps NPC-opened panel visible");
        Check(EndgamePanel.TryOpenCubeFromWorld() && (int)Get(p, "tab") == 2, "crafting NPC selects cube tab");
        Check(c.Sent.Exists(r => r.Catalog == "recipes"), "crafting entry requests recipe catalog");
        var pending = new EndgameRequest("rift.start", tier: 1); c.SendEndgame(pending);
        Check(EndgamePanel.TryOpenRiftFromWorld() && ReferenceEquals(c.Endgame.PendingRequest, pending), "opening a view preserves uncertain mutation identity");
        c.Endgame.Reset(); int before = c.Sent.Count;
        Check(!EndgamePanel.TryOpenCubeFromWorld() && c.Sent.Count == before, "stale NPC option cannot bypass reset negotiation");
        Update(p); Check(!(bool)Get(p, "open"), "reset closes old view");
        Negotiate(c);
        Check(EndgamePanel.TryOpenCubeFromWorld(), "renegotiated entry opens before update");
        Update(p); Check((bool)Get(p, "open"), "renegotiated open survives update");
        UnityEngine.Input.pressed = UnityEngine.KeyCode.F8; Update(p);
        Check(!(bool)Get(p, "open"), "F8 still closes the NPC-opened view");
        Update(p); Check((bool)Get(p, "open"), "F8 still opens the panel");
        UnityEngine.Input.pressed = null;
        p.Unbind(c); before = c.Sent.Count;
        Check(!EndgamePanel.TryOpenRiftFromWorld() && c.Sent.Count == before, "detached row sends nothing");
        Console.WriteLine("PASS " + assertions + " NPC world-entry contracts; actual panel logic, deterministic Unity substitutes.");
    }
}
