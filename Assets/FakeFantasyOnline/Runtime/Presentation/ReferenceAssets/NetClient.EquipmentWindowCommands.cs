using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writeUnequipItem;
    private Action<int[]> writeAllocPoints;
    private Action<string> writeSetTitle;
    internal void BindEquipmentWindowCommandsForIndependentRuntime(
        Action<int> unequip, Action<int[]> points, Action<string> title)
    { writeUnequipItem = unequip; writeAllocPoints = points; writeSetTitle = title; }

    // Native 060006e6 / 0600071a / 060006c7 call points. Existing Application/Protocol
    // writers remain the sole serialization/transport owners; starting is not send success.
    public void UnequipItemForIndependentRuntime(int slot) => writeUnequipItem(slot);
    public void AllocPointsForIndependentRuntime(int[] points) => writeAllocPoints(points);
    public void SetTitleForIndependentRuntime(string title) => writeSetTitle(title);
}
}
