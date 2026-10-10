using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : ISocialUiUpdateSource
    {
        // Explicit unbound compatibility observations only. Product receipt callbacks bypass
        // this FIFO, and no world window drains it or reconstructs gameplay from snapshots.
        private readonly Queue<WorldSocialUiUpdate> _socialUiUpdates = new Queue<WorldSocialUiUpdate>();
        public bool TryDequeueSocialUiUpdate(out WorldSocialUiUpdate update)
        {
            lock (_worldStateSync)
            {
                if (_socialUiUpdates.Count == 0) { update = null; return false; }
                update = _socialUiUpdates.Dequeue(); return true;
            }
        }
    }
}
