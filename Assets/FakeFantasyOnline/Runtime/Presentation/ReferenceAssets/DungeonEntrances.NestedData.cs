using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public static partial class DungeonEntrances
{
    [Serializable] public class Dungeon { public string name; public int map; public float x; public float y; }
    [Serializable] public class Entry { public string npc; public Dungeon[] dungeons; }
    [Serializable] private class Root { public Entry[] entries; }
}
}
