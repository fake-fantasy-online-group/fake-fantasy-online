using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IDuelStateSource
    {
        private WorldDuelState _duelState = WorldDuelState.Empty;
        private long _duelStateVersion;
        public WorldDuelState DuelState => Volatile.Read(ref _duelState);
        public long DuelStateVersion => Interlocked.Read(ref _duelStateVersion);

        // Official Dispatch 0x8076 -> DuelDlg.SetList. BetsOpen precedes Spectators on wire.
        private static WorldDuelState ReadDuelState(byte[] payload)
        {
            var reader = new NetClient.Buf(payload);
            int entry = reader.I32(), fee = reader.I32(), minimum = reader.I32(), maximum = reader.I32();
            int count = reader.U16();
            var rows = new List<WorldDuelRowState>(count);
            for (int i = 0; i < count; i++)
            {
                int id = reader.I32();
                string a = reader.Str(), b = reader.Str();
                int mode = reader.U8(), phase = reader.U8(), entryGold = reader.I32();
                bool betsOpen = reader.U8() != 0;
                int spectators = reader.I32();
                rows.Add(new WorldDuelRowState(id, a, b, mode, phase, entryGold, spectators, betsOpen));
            }
            return new WorldDuelState(false, 0, string.Empty, 0, entry, fee, minimum, maximum, rows);
        }

        // Observation only, after receipt processing. This never drives the native owner.
        private void PublishDuelState(WorldDuelState state)
        {
            lock (_worldStateSync)
            {
                Volatile.Write(ref _duelState, state);
                Interlocked.Increment(ref _duelStateVersion);
            }
        }
    }
}
