namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    public static RemotePlayer FindPlayer(int id)
    {
        RemotePlayer player;
        return I && I.players.TryGetValue(id, out player) ? player : null;
    }

    // Original RenamePlayer only updates an existing Unity-live remote identity.
    public static void RenamePlayer(int id, string name)
    {
        RemotePlayer player = FindPlayer(id);
        if (player) player.pname = name ?? string.Empty;
    }

    public static void UpdatePlayerHair(int id, int head, int hair)
    {
        RemotePlayer player;
        if (!I || !I.players.TryGetValue(id, out player) || !player) return;
        player.SetHair(head, hair);
    }

    public static void UpdatePlayerAppearance(int id, int body, int cap, int backpack,
        int wr, int wl, int face, int variant, string aura, string soulFx,
        string titleFx, int glowMode = -1)
    {
        RemotePlayer player;
        if (!I || !I.players.TryGetValue(id, out player) || !player) return;
        player.UpdateAppearance(body, cap, backpack, wr, wl, face, variant);
        player.SetAvatarAura(aura);
        player.SetSoulFx(soulFx);
        player.SetTitleFx(titleFx);
        if (glowMode >= 0) player.SetGlowMode(glowMode);
    }
}

}
