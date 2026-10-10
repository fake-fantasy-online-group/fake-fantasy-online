namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class LoadTrace
{
    private static int _spPlayers;
    private static int _spPets;
    private static int _spMobs;

    public static void Spawn(string kind, int id, int model, string name)
    {
        if (!On) return;
        if (kind == "玩家") _spPlayers = unchecked(_spPlayers + 1);
        else if (kind == "宠") _spPets = unchecked(_spPets + 1);
        else _spMobs = unchecked(_spMobs + 1);
        Line(string.Format("SPAWN {0} id={1} model={2} {3}", kind, id, model, name));
    }
}

}
