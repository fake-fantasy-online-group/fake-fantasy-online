using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official 1.5.8 selectable surface used by TargetSelect/TargetRing.
/// </summary>
public interface ISelectable
{
    Transform SelTransform { get; }

    string SelName { get; }

    int SelLevel { get; }

    int SelHp { get; }

    int SelMaxHp { get; }

    bool SelFriendly { get; }

    int SelOwnerId { get; }

    Vector3 SelFeetPos { get; }
}

}
