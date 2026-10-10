using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class SystemInfoBar
    {
        // Official 0x06000e33. Match against the previous list, including duplicate ids.
        public static void SetList(List<(int, byte, int, string, string)> items)
        {
            Ensure();
            List<Req> previous = I.reqs;
            var next = new List<Req>();
            bool hasNew = false;
            foreach (var item in items)
            {
                int index = previous.FindIndex(q => q.reqId == item.Item1);
                float animation;
                if (index >= 0) animation = previous[index].anim;
                else { animation = 0f; hasNew = true; }
                next.Add(new Req { reqId = item.Item1, kind = item.Item2, fromId = item.Item3,
                    fromName = item.Item4 ?? "", text = item.Item5 ?? "", anim = animation, local = false });
            }
            previous.Clear();
            previous.AddRange(next);
            if (hasNew)
            {
                SoundManager.Play(1009);
                I.ListSoundCountForIndependentRuntime++;
            }
        }

        // Official 0x06000e36. The six-item limit applies to the copy, never the stored lists.
        internal List<Req> Merged()
        {
            var result = new List<Req>(reqs);
            result.AddRange(gossip);
            if (result.Count > 6) result.RemoveRange(6, result.Count - 6);
            return result;
        }
    }
}
