using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class TaskClient
{
    public class ActiveTask
    {
        public int id;
        public int remainSec = -1;
        public int step;
        public int total;
        public string text = string.Empty;
        public string progress = string.Empty;
        public string to = string.Empty;
        public byte ready;
        public float recvTime;
    }

    public static readonly List<ActiveTask> Active = new List<ActiveTask>();
    public static readonly HashSet<int> Done = new HashSet<int>();
    public static Action OnStateChanged;

    public static void SetState(List<ActiveTask> active, List<int> done)
    {
        Active.Clear();
        Active.AddRange(active);
        Done.Clear();
        foreach (int id in done) Done.Add(id);
        OnStateChanged?.Invoke();
    }
}

}
