using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class NpcChatter
{
    private Camera cam;
    private string role = "common";
    private string bubble;
    private float bubbleUntil = -999f;
    public Vector3 headAnchor;
    public string npcName;
    private Texture2D _bakedName;
    private string _bakedNameFor;
    private int _bakedNameScale;
    private float acc;
    private int lineIdx;
    public string bubbleTheme = "huang";

    // Official 1.5.8 token 06000915.
    public static string RoleOf(NpcInteract n)
    {
        if (n == null) return "common";
        if (n.sell > 0) return "vendor";
        if (n.trans > 0) return "transport";
        string name = n.npcName ?? string.Empty;
        if (name.Contains("仓库")) return "storage";
        if (name.Contains("主持") || name.Contains("仙子") || name.Contains("医") ||
            name.Contains("药") || name.Contains("治疗")) return "heal";
        if (name.Contains("传令") || name.Contains("任务") || name.Contains("委托")) return "quest";
        if (name.Contains("新手") || name.Contains("教头") || name.Contains("向导") ||
            name.Contains("引导") || name.Contains("导师")) return "guide";
        return "common";
    }

    // Official 1.5.8 token 06000916.
    private void Start()
    {
        EnsureData();
        cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        NpcInteract n = GetComponent<NpcInteract>();
        role = RoleOf(n);
        if (string.IsNullOrEmpty(npcName) && n != null) npcName = n.npcName;
        if (!string.IsNullOrEmpty(npcName)) allNpcNames.Add(npcName);
        acc = Random.Range(0f, CycleMs());
    }

    // Official 1.5.8 token 06000917.
    private void OnDestroy()
    {
        if (_bakedName != null) Destroy(_bakedName);
    }

    // Official 1.5.8 token 06000918.
    private static float CycleMs()
    {
        return sMaxGap > 0f ? sMaxGap * 1000f : 20000f;
    }

    // Official 1.5.8 token 06000919.
    private void Update()
    {
        if (!sEnabled) return;
        if (Time.time <= bubbleUntil) return;
        acc += Time.deltaTime * 1000f;
        if (acc < CycleMs()) return;
        acc = 0f;
        string next = PickLine();
        if (next != null)
        {
            bubble = next;
            bubbleUntil = Time.time + sShowSec;
        }
    }

    // Official 1.5.8 token 0600091a.
    private string PickLine()
    {
        if (byNpc != null && !string.IsNullOrEmpty(npcName) &&
            byNpc.TryGetValue(npcName, out List<string> named) && named.Count > 0)
            return named[unchecked(lineIdx++) % named.Count];
        string own = OwnGreeting(npcName);
        if (own != null) return own;
        List<string> lines;
        if (byRole == null || !byRole.TryGetValue(role, out lines) || lines.Count <= 0)
            lines = all;
        if (lines == null || lines.Count <= 0) return null;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            string line = lines[unchecked(lineIdx++) % lines.Count];
            if (!IsOthersGreeting(line, npcName)) return line;
        }
        return null;
    }

    // Official 1.5.8 token 06000914.
    private static bool IsOthersGreeting(string line, string selfName)
    {
        if (string.IsNullOrEmpty(line)) return false;
        foreach (string name in allNpcNames)
            if (name.Length > 1 && name != selfName && line.Contains(name)) return true;
        return false;
    }
}

}
