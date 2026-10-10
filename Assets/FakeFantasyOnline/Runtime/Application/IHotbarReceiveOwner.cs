namespace FakeFantasyOnline.Application
{

/// <summary>
/// Unity-free native-owner operations for the ordered 0x8016 receipt. Protocol owns all wire
/// reads and the per-row gate; the host performs each store synchronously before the next read.
/// </summary>
public interface IHotbarReceiveOwner
{
    /// <summary>Dictionary index assignment for one qualifying row; do not clear or ensure it.</summary>
    void ReceiveHotbarPetSkillIcon(int id, int icon);

    /// <summary>The one successful native LoadHotbar tail, after all reached row stores.</summary>
    void ReceiveHotbarSlots(int[] ids, int[] kinds, bool expanded, int[] icons);
}

}
