using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

[Serializable]
public class NpcMap
{
    public string map;
    public int worldW;
    public int worldH;
    public int mapImgW;
    public int mapImgH;
    public NpcEntry[] npcs;
}

}
