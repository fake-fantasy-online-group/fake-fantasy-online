using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Original selectable type/members; NpcInteract.Show and its producer are separate recovery.
public partial class NpcInteract : MonoBehaviour, ISelectable
{
    public string npcName;
    public string portrait;
    public string label;
    public int sell;
    public int trans;
    public Vector3 feetPos;
    public bool hasFeetPos;
    public Transform SelTransform => this != null ? transform : null;
    public string SelName => npcName;
    public int SelLevel => 0;
    public int SelHp => -1;
    public int SelMaxHp => 0;
    public bool SelFriendly => true;
    public int SelOwnerId => -1;
    public Vector3 SelFeetPos => hasFeetPos ? feetPos : transform.position;
}

}
