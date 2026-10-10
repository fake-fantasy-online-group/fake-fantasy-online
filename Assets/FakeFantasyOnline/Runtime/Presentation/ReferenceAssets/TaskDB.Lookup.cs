using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class TaskDB
{
    private static Dictionary<int, Def> _byId;
    private static Dictionary<string, List<int>> _byPub;

    private static void Ensure()
    {
        if (_byId != null) return;
        _byId = new Dictionary<int, Def>();
        _byPub = new Dictionary<string, List<int>>();
        string text = GameTables.LoadText("tasks_full");
        if (text == null)
        {
            Debug.LogWarning("[task] 缺 tasks_full.json(StreamingAssets/gameassets/tables/ 与 Resources 都没有)");
            return;
        }
        Wrap wrap = JsonUtility.FromJson<Wrap>("{\"tasks\":" + text + "}");
        if (wrap == null || wrap.tasks == null) return;
        foreach (Def def in wrap.tasks)
        {
            def.name = RichCode.Strip(def.name);
            def.desc = RichCode.Strip(def.desc);
            def.publisher = RichCode.Strip(def.publisher);
            def.place = RichCode.Strip(def.place);
            def.award = RichCode.Strip(def.award);
            _byId[def.id] = def;
            if (string.IsNullOrEmpty(def.publisher)) continue;
            if (!_byPub.TryGetValue(def.publisher, out List<int> ids))
            { ids = new List<int>(); _byPub[def.publisher] = ids; }
            ids.Add(def.id);
        }
    }

    public static int Count { get { Ensure(); return _byId.Count; } }

    // Official 1.5.8 token 06000e79.
    public static IReadOnlyList<int> ByPublisher(string npc)
    {
        Ensure();
        if (_byPub != null && _byPub.TryGetValue(npc, out List<int> ids)) return ids;
        return Array.Empty<int>();
    }

    public static Def Get(int id)
    {
        Ensure();
        return _byId != null && _byId.TryGetValue(id, out Def def) ? def : null;
    }
}

}
