using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 1.6.0 Dispatch 06000789, case 8022: build the two native collections
    // with one Buf, then publish those same roots only after every row completes.
    internal static void ReceiveLifeSkillsForIndependentRuntime(IWorldLifeSkillReceiptReader reader)
    {
        int count = reader.U16();
        var learned = new Dictionary<int, (int, string)>(count);
        var canLearn = new HashSet<int>();
        for (int index = 0; index < count; index++)
        {
            int id = reader.I32();
            int level = reader.I32();
            string name = reader.Str();
            int eligible = reader.U8();
            if (level >= 0) learned[id] = (level, name);
            else if (eligible != 0) canLearn.Add(id);
        }
        Win05.SetLifeSkills(learned, canLearn);
    }
}
}
