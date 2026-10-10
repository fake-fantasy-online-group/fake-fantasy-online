using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity ISelectable owner (06000551..06000558).
/// </summary>
public partial class MonsterEntity : ISelectable
{
    public int id;
    public int level;
    public int hp;
    public int maxHp;
    public int lootOwnerId;
    public bool ally;

    public Transform SelTransform
    {
        get { return this ? transform : null; }
    }

    public string SelName
    {
        get { return mname; }
    }

    public int SelLevel
    {
        get { return level; }
    }

    public int SelHp
    {
        get { return hp; }
    }

    public int SelMaxHp
    {
        get { return maxHp; }
    }

    public bool SelFriendly
    {
        get { return friendly || ally; }
    }

    public int SelOwnerId
    {
        get { return friendly || ally ? -1 : lootOwnerId; }
    }

    public Vector3 SelFeetPos
    {
        get { return transform.position; }
    }
}

}
