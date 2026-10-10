using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 1.6.0 Dispatch 0x8027. Read and strip each row before constructing its native
    // ActiveTask, then sample its time before Add and before any read for the next row.
    // Both lists use the original parameterless constructor. SetState owns all public
    // list/set mutations and the callback; no immutable observation precedes that tail.
    internal static bool ReceiveTaskLogForIndependentRuntime(IWorldTaskReceiptReader reader)
    {
        int count = reader.U16();
        List<TaskClient.ActiveTask> active = new List<TaskClient.ActiveTask>();
        for (int index = 0; index < count; index++)
        {
            int id = reader.I32();
            int remainSec = reader.I32();
            int step = reader.I32();
            int total = reader.I32();
            string text = RichCode.Strip(reader.Str());
            string progress = RichCode.Strip(reader.Str());
            byte ready = (byte)reader.U8();
            string to = RichCode.Strip(reader.Str());
            TaskClient.ActiveTask row = new TaskClient.ActiveTask();
            row.text = text;
            row.id = id;
            row.remainSec = remainSec;
            row.step = step;
            row.total = total;
            row.progress = progress;
            row.to = to;
            row.ready = ready;
            row.recvTime = UnityEngine.Time.realtimeSinceStartup;
            active.Add(row);
        }

        int doneCount = reader.U16();
        List<int> done = new List<int>();
        for (int index = 0; index < doneCount; index++) done.Add(reader.I32());
        TaskClient.SetState(active, done);
        return count != 0 || doneCount != 0;
    }

    // Official 1.6.0 Dispatch 0x8028 allocates each Offer before the first row read, then
    // stores each read in order and appends that exact object. SetOffers retains those
    // references in the existing static list before calling the current delegate.
    internal static void ReceiveTaskOffersForIndependentRuntime(IWorldTaskReceiptReader reader)
    {
        string npc = reader.Str();
        int count = reader.U16();
        List<TaskClient.Offer> offers = new List<TaskClient.Offer>();
        for (int index = 0; index < count; index++)
        {
            TaskClient.Offer row = new TaskClient.Offer();
            row.id = reader.I32();
            row.state = (byte)reader.U8();
            row.title = reader.Str();
            row.desc = reader.Str();
            row.award = reader.Str();
            offers.Add(row);
        }
        TaskClient.SetOffers(npc, offers);
    }
}
}
