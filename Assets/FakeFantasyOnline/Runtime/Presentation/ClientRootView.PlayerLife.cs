using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{

public sealed partial class ClientRootView : IPlayerLifeReceiveContext, IWorldReceiveSceneContext
{
    void IWorldReceiveSceneContext.ApplyPublishedWorldEvents()
    {
        DispatchOfficialTeleportEdge();
        _gameBootstrap?.Window?.ApplyPublishedWorldEventsForIndependentRuntime();
    }

    void IPlayerLifeReceiveContext.BeginSelfData() => NetClient.BeginSelfDataForIndependentRuntime();
    void IPlayerLifeReceiveContext.SetSelfEntityId(int entityId) => NetClient.SelfEntityId = entityId;

    PlayerLifeReceiveResult IPlayerLifeReceiveContext.ReceivePlayerLife(int targetId, bool alive, byte flags)
    {
        PlayerController owner = PlayerController.I;
        bool local = NetClient.HandleLocalPlayerLifeForIndependentRuntime(targetId, alive, flags);
        if (local && owner != null)
            _gameBootstrap?.Window?.Frame?.ObserveNativeLifeForIndependentRuntime(owner);
        return new PlayerLifeReceiveResult(local, local && owner != null, NetClient.SelfEntityId);
    }
}
}
