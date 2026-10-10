using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{
    // Legacy unbound-gateway observation. This is not authorization for delayed native delivery;
    // product delivery requires ISocialNoticeReceiveOwner at the main-thread receipt boundary.
    public sealed class WorldSocialUiUpdate
    {
        public WorldSocialUiUpdate(WorldPrivateChatState message) { Message = message; }
        public WorldSocialUiUpdate(IReadOnlyList<WorldSystemRequestState> requests)
        { Requests = new ReadOnlyCollection<WorldSystemRequestState>(new List<WorldSystemRequestState>(requests ?? Array.Empty<WorldSystemRequestState>())); }
        public WorldPrivateChatState Message { get; }
        public IReadOnlyList<WorldSystemRequestState> Requests { get; }
    }
    public interface ISocialUiUpdateSource
    {
        bool TryDequeueSocialUiUpdate(out WorldSocialUiUpdate update);
    }
}
