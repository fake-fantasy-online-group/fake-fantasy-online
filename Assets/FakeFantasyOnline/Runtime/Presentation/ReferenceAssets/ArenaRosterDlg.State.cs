using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class ArenaRosterDlg
{
    // Official 06000079: an omitted/empty description set preserves a nonempty roster's cache.
    public static void Set(List<Row> rows, Dictionary<int, string> descs = null)
    {
        Ensure();
        _rows = rows ?? new List<Row>();
        if (descs != null && descs.Count > 0)
            _descs = descs;
        else if (_rows.Count == 0)
            _descs = new Dictionary<int, string>();
    }

    // Official 0600007a. Keep this operation separate from world/map lifecycle decisions.
    public static void Clear()
    {
        _rows.Clear();
        _descs.Clear();
    }

    // Official 0600007b: exact level first, then the fixed descending fallback sequence.
    public static string DescOf(int skillId, int lv)
    {
        int key = unchecked(skillId * 100);
        if (_descs.TryGetValue(unchecked(key + lv), out string text)) return text;
        if (_descs.TryGetValue(unchecked(key + 4), out text)) return text;
        if (_descs.TryGetValue(unchecked(key + 3), out text)) return text;
        if (_descs.TryGetValue(unchecked(key + 2), out text)) return text;
        if (_descs.TryGetValue(unchecked(key + 1), out text)) return text;
        return "";
    }
}
}
