using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Official 0600077f, bounded 0x8062 branch. Publish only after all entered reads succeed.
    private static void ReceiveArenaRoster(byte[] payload, IArenaRosterReceiveContext context)
    {
        var reader = new NetClient.Buf(payload);
        int count = reader.U8();
        var rows = new List<ArenaRosterRowData>(count);
        for (int i = 0; i < count; i++)
        {
            int entityId = reader.I32();
            string name = reader.Str();
            bool alive = reader.U8() != 0;
            int deaths = reader.U8();
            int cardCount = reader.U8();
            var cards = new List<ArenaRosterCardData>();
            for (int j = 0; j < cardCount; j++)
                cards.Add(new ArenaRosterCardData(reader.I32(), reader.U8(), reader.Str()));
            rows.Add(new ArenaRosterRowData(entityId, name, alive, deaths, cards));
        }

        var descriptions = new Dictionary<int, string>();
        if (reader.Remaining >= 2)
        {
            int descriptionCount = reader.U16();
            for (int i = 0; i < descriptionCount && reader.Remaining > 0; i++)
            {
                int skillId = reader.I32();
                int level = reader.U8();
                string description = reader.Str();
                descriptions[unchecked(skillId * 100 + level)] = description;
            }
        }
        context.ReceiveArenaRoster(rows, descriptions);
    }
}
}
