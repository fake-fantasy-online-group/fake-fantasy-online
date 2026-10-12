using System;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway : IWorldCombatNumberStreamSource
{
    private long _combatNumberStreamGeneration;
    public long CombatNumberStreamGeneration => Interlocked.Read(ref _combatNumberStreamGeneration);

    private void ClearCombatNumberPresentationJournal()
    {
        lock (_worldStateSync)
        {
            if (_combatNumbers.Count > 0)
            {
                Volatile.Write(ref _combatNumbers, Array.Empty<WorldCombatNumberState>());
                Interlocked.Increment(ref _combatNumbersVersion);
            }
            _combatNumberRevision = 0;
            Interlocked.Increment(ref _combatNumberStreamGeneration);
        }
    }
}

}
