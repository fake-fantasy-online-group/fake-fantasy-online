using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Original GameHud.TgSt (0200008f): sequential value storage, not snapshot objects.
    public struct TgSt
    {
        public int icon;
        public bool good;
        public int remainMs;
        public string name;
        public string desc;
    }

    // Official five static slots shared by 0600041c/41d/41e. The List<TgSt> allocation
    // is ordered with all other statics in Initialization; the scalar roots use managed defaults.
    internal static int tgStatusOf;
    internal static float tgStatusRecv;
    private static int tgPollId;
    private static float tgPollNextT;

    // 0600041c: ARM 43d128/268, x86 454e10/256, frozen WBB111. Identity and time are
    // written even for a null list; preserve the list root and its native null dereferences.
    public static void SetTargetStatus(int id, List<TgSt> list)
    {
        tgStatusOf = id;
        tgStatusRecv = Time.realtimeSinceStartup;
        tgStatus.Clear();
        if (list != null) tgStatus.AddRange(list);
    }

    // 0600041d: ARM 43c660/684, x86 454320/608, frozen WBB104 plus corrected WBB112.
    public static void PollTargetStatus(int id)
    {
        // Reading type must precede even id admission: null Event.current throws natively.
        EventType eventType = Event.current.type;
        if (id < 1) return;
        if (eventType != EventType.Repaint) return;
        if (NetClient.I == null) return;

        float now = Time.realtimeSinceStartup;
        if (id != tgPollId)
        {
            tgPollId = id;
            tgPollNextT = 0f;
            tgStatus.Clear();
            tgStatusOf = 0;
        }
        // Deliberately use the native less-than return, including its floating-point polarity.
        if (now < tgPollNextT) return;
        tgPollNextT = now + 1f;
        // Re-read the real Unity singleton for the instance call, after advancing the deadline.
        // No task-completion admission, extra target filter or receive-time reset belongs here.
        NetClient.I.QueryTargetStatusForIndependentRuntime(id);
    }
}
}
