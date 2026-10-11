using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{
public sealed partial class EndgameClientState
{
    public IReadOnlyList<EndgameItemView> Uniques { get; private set; } = Array.Empty<EndgameItemView>();
    public IReadOnlyList<EndgameRuneView> Runes { get; private set; } = Array.Empty<EndgameRuneView>();
    public IReadOnlyList<EndgameFormationView> Formations { get; private set; } = Array.Empty<EndgameFormationView>();
    public IReadOnlyList<EndgameRecipeView> Recipes { get; private set; } = Array.Empty<EndgameRecipeView>();
    private sealed class CatalogRequest
    {
        internal readonly string ID, Cursor;
        internal readonly DateTime At = DateTime.UtcNow;
        internal CatalogRequest(EndgameRequest r) { ID = r.RequestID; Cursor = r.Cursor; }
    }
    private readonly Dictionary<string, CatalogRequest> catalogRequests = new Dictionary<string, CatalogRequest>();
    private readonly Dictionary<string, string> catalogCursors = new Dictionary<string, string>();
    public bool CatalogRequested(string name) => catalogRequests.ContainsKey(name);
    public bool CatalogLoaded(string name) => catalogCursors.ContainsKey(name);
    public bool CatalogHasMore(string name) => !CatalogLoaded(name) || NextCatalogCursor(name).Length > 0;
    public string NextCatalogCursor(string name) => catalogCursors.TryGetValue(name, out string cursor) ? cursor : "";
    private string CatalogForRequest(string id)
    {
        foreach (var row in catalogRequests) if (row.Value.ID == id) return row.Key;
        return null;
    }
    private void ResetCatalogValues()
    {
        Uniques = Array.Empty<EndgameItemView>(); Runes = Array.Empty<EndgameRuneView>(); Formations = Array.Empty<EndgameFormationView>(); Recipes = Array.Empty<EndgameRecipeView>(); catalogCursors.Clear();
    }
    private void ResetCatalogs() { ResetCatalogValues(); catalogRequests.Clear(); }
    private void ApplyCatalog(string name, EndgameResponse r)
    {
        if (!catalogRequests.TryGetValue(name, out CatalogRequest request) || request.Cursor != r.Cursor) return;
        if (r.RequestID.Length > 0 && request.ID != r.RequestID) return;
        if (r.NextCursor.Length > 0 && r.NextCursor == r.Cursor) throw new FormatException("Non-advancing catalog cursor.");
        if (r.Cursor.Length > 0 && !CatalogLoaded(name)) { catalogRequests.Remove(name); return; }
        if (name == "uniques") Uniques = Append(r.Cursor.Length == 0 ? Array.Empty<EndgameItemView>() : Uniques, r.Items);
        else if (name == "runes") Runes = Append(r.Cursor.Length == 0 ? Array.Empty<EndgameRuneView>() : Runes, r.Runes);
        else if (name == "formations") Formations = Append(r.Cursor.Length == 0 ? Array.Empty<EndgameFormationView>() : Formations, r.Formations);
        else if (name == "recipes") Recipes = Append(r.Cursor.Length == 0 ? Array.Empty<EndgameRecipeView>() : Recipes, r.Recipes);
        else return;
        catalogCursors[name] = r.NextCursor; catalogRequests.Remove(name);
    }
    private static IReadOnlyList<T> Append<T>(IReadOnlyList<T> a, IReadOnlyList<T> b)
    {
        if (a.Count + b.Count > 4096) throw new FormatException("Catalog exceeds limit.");
        var rows = new List<T>(a.Count + b.Count); foreach (var row in a) rows.Add(row); foreach (var row in b) rows.Add(row);
        return new ReadOnlyCollection<T>(rows);
    }
}
}
