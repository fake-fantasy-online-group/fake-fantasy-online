using System;
using System.Collections.Generic;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

// Production workbench code with deterministic Unity substitutes; no renderer or Player.
class EndgameModuleContract
{
    static int assertions;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); assertions++; }
    sealed class Client : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld => true;
        public List<EndgameRequest> Sent = new List<EndgameRequest>();
        public bool FailRecipeTransport;
        public bool SendEndgame(EndgameRequest r)
        {
            if (!Endgame.Begin(r)) return false;
            Sent.Add(r);
            // Match the production gateway: a transport exception releases read state and returns false.
            if (FailRecipeTransport && r.Catalog == "recipes")
            { Endgame.NoteTransportFailure("transport unavailable", r); return false; }
            return true;
        }
        public bool RetryEndgame() => false;
        public bool EquipEndgameItem(EndgameItemView item) => false;
        public bool UnequipEndgameItem(EndgameItemView item) => false;
    }
    static object Get(EndgamePanel p, string field) => typeof(EndgamePanel).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(p);
    static void Call(EndgamePanel p, string method) => typeof(EndgamePanel).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(p, null);
    static void Draw(EndgamePanel p, string click = null) { ArtworkSkin.enableds.Clear(); GUI.labels.Clear(); ArtworkSkin.click = click; Call(p, "OnGUI"); }
    static void Apply(Client c, string json) => c.Endgame.Apply(EndgameResponse.Parse(json));
    static string Recipe(int id, string operation, string name, int output = 0) => "{\"id\":" + id + ",\"name\":\"" + name + "\",\"operation\":\"" + operation + "\",\"targetRequired\":" + (output == 0 && operation != "salvage.rune" ? "true" : "false") + ",\"outputItemId\":" + output + "}";
    static void Main()
    {
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c);
        c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        Call(p, "Update");
        Check(EndgamePanel.TryOpenCubeFromWorld(), "NPC opens cube");
        var rows = new List<string>();
        string[] ops = { "salvage.normal", "salvage.unique", "salvage.rune", "socket.open", "socket.extract", "socket.reorder", "socket.array", "unique.awaken", "unique.reforge", "unique.temper" };
        int[] ids = { 1001, 1002, 1003, 1006, 1007, 1008, 1009, 1010, 1011, 1012 };
        for (int i = 0; i < ops.Length; i++) rows.Add(Recipe(ids[i], ops[i], "动作" + i));
        var runes = new List<string>();
        for (int i = 1; i <= 28; i++)
        {
            rows.Add(Recipe(11000 + i, "rune.inscribe", "凝髓成篆·星篆" + i, 1900028000 + i));
            rows.Add(Recipe(12000 + i, "rune.convert", "同象易宿·星篆" + i, 1900028000 + i));
            runes.Add("{\"itemId\":" + (1900028000 + i) + ",\"name\":\"星篆" + i + "\"}");
        }
        Apply(c, "{\"kind\":\"catalog.runes\",\"ok\":true,\"runes\":[" + string.Join(",", runes) + "]}");
        for (int offset = 0; offset < rows.Count; offset += 32)
        {
            int count = Math.Min(32, rows.Count - offset);
            string cursor = offset == 0 ? "" : offset.ToString();
            string next = offset + count == rows.Count ? "" : (offset + count).ToString();
            Check(c.Sent.Exists(r => r.Catalog == "recipes" && r.Cursor == cursor), "workbench requests page " + cursor);
            int before = c.Sent.Count; Call(p, "Update"); Check(c.Sent.Count == before, "pending page does not duplicate");
            Apply(c, "{\"kind\":\"catalog.recipes\",\"ok\":true,\"cursor\":\"" + cursor + "\",\"nextCursor\":\"" + next + "\",\"recipes\":[" + string.Join(",", rows.GetRange(offset, count)) + "]}");
            Call(p, "Update");
        }
        Check(c.Endgame.Recipes.Count == 66, "all 66 functional recipes retained across automatic pages");
        int terminalCount = c.Sent.Count; Call(p, "Update"); Check(c.Sent.Count == terminalCount, "terminal metadata page stops loading");
        Draw(p);
        for (int i = 0; i < ops.Length; i++) Check(ArtworkSkin.enableds.ContainsKey("动作" + i), "singular operation is reachable: " + ops[i]);
        Check(ArtworkSkin.enableds.ContainsKey("凝髓成篆") && ArtworkSkin.enableds.ContainsKey("同象易宿"), "output variants are grouped under two actions");
        Check(!ArtworkSkin.enableds.ContainsKey("凝髓成篆·星篆28") && !ArtworkSkin.enableds.ContainsKey("继续翻页") && !ArtworkSkin.enableds.ContainsKey("嵌入所选星篆"), "cube contains only its operation controls, no recipe browser or rune module");
        for (int kind = 0; kind < 2; kind++)
        {
            Draw(p, kind == 0 ? "凝髓成篆" : "同象易宿");
            for (int i = 1; i <= 28; i++)
            {
                Draw(p, "选择成品星篆"); Draw(p, "星篆" + i);
                Check((int)Get(p, "recipeID") == (kind == 0 ? 11000 : 12000) + i, "specific output remains actionable " + kind + ":" + i);
                Check((EndgameModule)Get(p, "module") == EndgameModule.Cube, "output selection never changes module");
            }
        }
        Draw(p, "动作3"); Draw(p, "选择器物");
        Check((bool)Get(p, "selectingCubeTarget") && (EndgameModule)Get(p, "module") == EndgameModule.Cube, "target selection remains inside cube");
        Draw(p, "返回炼化方式"); Check(!(bool)Get(p, "selectingCubeTarget"), "target picker has a local back path");
        Check(EndgamePanel.TryOpenRunesFromWorld(), "runes open separately"); Draw(p);
        Check(ArtworkSkin.enableds.ContainsKey("嵌入所选星篆") && !ArtworkSkin.enableds.ContainsKey("动作3") && !ArtworkSkin.enableds.ContainsKey("宿阵图鉴") && !ArtworkSkin.enableds.ContainsKey("二十八宿图鉴"), "rune workbench contains no cube action or encyclopedia navigation");
        Check(!c.Sent.Exists(r => r.Catalog == "uniques" || r.Catalog == "formations"), "module entries never load player encyclopedias");
        Check(EndgamePanel.TryOpenRiftFromWorld(), "rift opens separately"); Draw(p);
        Check(!ArtworkSkin.enableds.ContainsKey("嵌入所选星篆") && !ArtworkSkin.enableds.ContainsKey("选择器物"), "rift has no socket or cube navigation");
        Draw(p, "×"); Input.pressed = KeyCode.F8; Call(p, "Update"); Input.pressed = null;
        Check(!(bool)Get(p, "open"), "removed F8 cannot reopen a module");
        var failed = new Client(); var retryPanel = new EndgamePanel(); retryPanel.Bind(failed);
        failed.Endgame.Begin(new EndgameRequest("hello")); Apply(failed, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        EndgamePanel.TryOpenCubeFromWorld();
        var requested = failed.Sent.Find(r => r.Catalog == "recipes");
        Apply(failed, "{\"kind\":\"catalog.recipes\",\"ok\":false,\"requestId\":\"" + requested.RequestID + "\",\"error\":\"storage\"}");
        int failedCount = failed.Sent.Count; Call(retryPanel, "Update"); Call(retryPanel, "Update");
        Check(failed.Sent.Count == failedCount && !failed.Endgame.CatalogRequested("recipes"), "failed recipe page waits for explicit refresh rather than frame retries");
        Draw(retryPanel, "刷新");
        Check(failed.Endgame.CatalogRequested("recipes") && failed.Sent.FindAll(r => r.Catalog == "recipes").Count == 2, "explicit refresh retries failed recipe metadata");
        requested = failed.Sent.FindLast(r => r.Catalog == "recipes");
        Draw(retryPanel, "×");
        Apply(failed, "{\"kind\":\"catalog.recipes\",\"ok\":true,\"requestId\":\"" + requested.RequestID + "\",\"nextCursor\":\"32\",\"recipes\":[]}");
        failedCount = failed.Sent.Count; Call(retryPanel, "Update");
        Check(failed.Sent.Count == failedCount, "closed cube does not continue metadata paging");
        EndgamePanel.TryOpenCubeFromWorld();
        Check(failed.Sent.Exists(r => r.Catalog == "recipes" && r.Cursor == "32"), "reopening resumes the exact unfinished metadata cursor");
        var transport = new Client { FailRecipeTransport = true }; var transportPanel = new EndgamePanel(); transportPanel.Bind(transport);
        transport.Endgame.Begin(new EndgameRequest("hello")); Apply(transport, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        EndgamePanel.TryOpenCubeFromWorld();
        Check(transport.Sent.FindAll(r => r.Catalog == "recipes").Count == 1 && !transport.Endgame.CatalogRequested("recipes"), "one open attempts a failed transport cursor only once after gateway read-state release");
        for (int i = 0; i < 10; i++) Call(transportPanel, "Update");
        Check(transport.Sent.FindAll(r => r.Catalog == "recipes").Count == 1, "transport failure never retries on idle frames");
        Draw(transportPanel, "刷新");
        Check(transport.Sent.FindAll(r => r.Catalog == "recipes").Count == 2, "explicit refresh attempts a transport-failed cursor once");
        for (int i = 0; i < 10; i++) Call(transportPanel, "Update");
        Check(transport.Sent.FindAll(r => r.Catalog == "recipes").Count == 2, "failed explicit retry is still bounded across idle frames");
        Draw(transportPanel, "×"); EndgamePanel.TryOpenCubeFromWorld();
        Check(transport.Sent.FindAll(r => r.Catalog == "recipes").Count == 3, "close and reopen explicitly permits one fresh transport attempt");
        transport.FailRecipeTransport = false; Draw(transportPanel, "刷新");
        Check(transport.Sent.FindAll(r => r.Catalog == "recipes").Count == 4 && transport.Endgame.CatalogRequested("recipes"), "recovered transport can load after explicit refresh");
        Apply(transport, "{\"kind\":\"catalog.recipes\",\"ok\":true,\"nextCursor\":\"32\",\"recipes\":[]}"); Call(transportPanel, "Update");
        Check(transport.Sent.FindAll(r => r.Catalog == "recipes").Count == 5 && transport.Sent.FindLast(r => r.Catalog == "recipes").Cursor == "32", "successful recovery resumes authoritative next cursor normally");
        Console.WriteLine("PASS " + assertions + " independent-module and complete crafting-choice contracts; no Unity launch.");
    }
}
