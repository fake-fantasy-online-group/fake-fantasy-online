namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class GroundItemEntity
{
    private void OnMouseDown()
    {
        if (GameHud.OverUI) return;
        if (PlayerController.I != null)
        {
            PlayerController.I.WalkToPickup(id, gx, gy);
            return;
        }
        if (NetClient.I != null) NetClient.I.PickItemForIndependentRuntime(id);
    }
}
}
