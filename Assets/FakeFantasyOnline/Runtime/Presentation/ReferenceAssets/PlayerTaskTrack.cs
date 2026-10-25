using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public class PlayerTaskTrack : MonoBehaviour
{
    private readonly List<string> lines = new List<string>();
    private float showUntil = -1f;
    private readonly Dictionary<int, string> prevSig = new Dictionary<int, string>();
    private readonly HashSet<int> seen = new HashSet<int>();
    private readonly HashSet<int> changedIds = new HashSet<int>();
    private readonly List<int> tmpRemove = new List<int>();
    private static PlayerTaskTrack inst;
    private readonly TaskPromptLayout layout = new TaskPromptLayout();
    private static readonly Color RED = new Color(1f, 0.3529411852359772f, 0.3529411852359772f, 1f);

    internal IReadOnlyList<string> Lines => lines;
    internal int PreviousSignatureCount => prevSig.Count;
    internal float ShowUntil => showUntil;

    private void OnEnable() { inst = this; }

    public static void Show(string text)
    {
        if (inst == null || string.IsNullOrEmpty(text)) return;
        inst.lines.Clear();
        foreach (string line in text.Replace("\r", string.Empty).Split('\n'))
            if (line.Length > 0) inst.lines.Add(line);
        if (inst.lines.Count > 0) inst.showUntil = Time.time + 5f;
    }

    private void Update()
    {
        var active = TaskClient.Active;
        changedIds.Clear();
        seen.Clear();
        if (active != null)
            foreach (TaskClient.ActiveTask task in active)
            {
                seen.Add(task.id);
                string sig = task.step.ToString() + "|" + task.progress;
                if (prevSig.TryGetValue(task.id, out string old) && sig != old) changedIds.Add(task.id);
                prevSig[task.id] = sig;
            }
        tmpRemove.Clear();
        foreach (int id in prevSig.Keys) if (!seen.Contains(id)) tmpRemove.Add(id);
        foreach (int id in tmpRemove) prevSig.Remove(id);
        if (changedIds.Count > 0)
        {
            BuildProgress();
            if (lines.Count > 0) showUntil = Time.time + 5f;
        }
    }

    private void BuildProgress()
    {
        lines.Clear();
        var active = TaskClient.Active;
        if (active == null) return;
        var changed = new List<TaskClient.ActiveTask>();
        foreach (TaskClient.ActiveTask task in active)
            if (changedIds.Contains(task.id) && !string.IsNullOrEmpty(task.progress)) changed.Add(task);
        int count = changed.Count;
        foreach (TaskClient.ActiveTask task in changed)
        {
            string name = count >= 2 ? (TaskDB.Get(task.id) != null ? TaskDB.Get(task.id).name : CubeQuestTitles.MissingTitle(task.id)) : null;
            foreach (string segment in task.progress.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries))
                lines.Add(string.IsNullOrEmpty(name) ? segment.Trim() : "【" + name + "】" + segment.Trim());
        }
    }

    private void OnGUI()
    {
        if (Time.time > showUntil || lines.Count == 0 || !BitmapFont.Ready) return;
        Camera camera = Camera.main;
        if (camera == null) return;
        PlayerController pc = GetComponent<PlayerController>();
        float ppu = pc != null ? pc.ppu : 100f;
        Vector3 screen = camera.WorldToScreenPoint(transform.position + new Vector3(0f, 140f / ppu, 0f));
        if (screen.z < 0f) return;
        float s = UIScale.Factor;
        float h = s * 16f;
        float margin = 12f * s;
        float maxWidth = Mathf.Max(1f, Mathf.Min(320f * s, Screen.width - 2f * margin));
        layout.Update(lines, maxWidth, s, text => BitmapFont.Measure(text, s));
        float y = Mathf.Max(8f * s, Screen.height - screen.y - h * layout.Rows.Count - 8f * s);
        for (int i = 0; i < layout.Rows.Count; i++)
        {
            float width = layout.Widths[i];
            float x = Mathf.Max(margin, Mathf.Min(screen.x - width * 0.5f, Screen.width - margin - width));
            BitmapFont.DrawOutline(x, y, layout.Rows[i], RED, s);
            y = h + y;
        }
    }
}

}
