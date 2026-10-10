using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceWorldReceiveOwner : ICounterWindowReceiveOwner
{
    public void StoreCounterCharges(int value) => CharData.counterCharges = value;

    public void StoreCounterTtlMilliseconds(int value) => CharData.counterTtlMs = value;

    public void StoreCounterSkillId(int value) => CharData.counterSkillId = value;

    public void StampCounterWindowReceipt() =>
        CharData.counterRecvTime = Time.realtimeSinceStartup;
}

}
