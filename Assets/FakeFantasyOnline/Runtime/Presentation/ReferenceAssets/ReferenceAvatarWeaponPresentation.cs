namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>Completes paired weapon artwork without changing equipped items or combat data.</summary>
internal static class ReferenceAvatarWeaponPresentation
{
    internal static int LeftModel(int rightModel, int leftModel)
    {
        // The source weapon_dual table identifies models with separate weaponl artwork.
        // An explicitly supplied left-hand appearance (including a shield) always wins.
        // Resolve on each build so changing or removing the right weapon cannot retain a
        // synthesized left model in CharData, network records, or the equipment state.
        return leftModel == 0 && SuitFxDB.IsDualWeaponModel(rightModel)
            ? rightModel : leftModel;
    }
}

}
