namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class Win05
{
    // Original DrawPet local-function body, shared by its three preference callbacks.
    private static void PerfApply(bool hideOthersJustTurnedOn)
    {
        GameSettings.Save();
        if (hideOthersJustTurnedOn)
        {
            CombatWorld.DespawnOtherPets();
            CombatWorld.ReleaseOtherMounts();
        }
        MonsterEntity.OnPetGlowSettingChanged();
        NetClient client = NetClient.I;
        if (!ReferenceEquals(client, null)) client.SetPetViewForIndependentRuntime(GameSettings.PetViewMask);
    }
}

}
