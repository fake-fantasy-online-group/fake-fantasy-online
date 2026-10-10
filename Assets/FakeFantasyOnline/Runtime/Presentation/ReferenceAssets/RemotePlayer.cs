using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Original remote identity, path, pose and action/animation owners feed the live CombatWorld
/// dictionary and skill/follow boundaries. Full selection consumers, spawn/update and mount lifecycle
/// recovery remains separate from the restored partial members.
/// </summary>
public partial class RemotePlayer : MonoBehaviour
{
    public int id;
    public float worldX;
    public float worldY;
    private bool dead;
    public bool IsDead => dead;
    internal void BindSkillIdentityForIndependentRuntime(int entityId, bool isDead)
    {
        id = entityId;
        dead = isDead;
    }
}

}
