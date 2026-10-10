using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class Win05
{
    public static bool GmTyping => Time.frameCount - gmTypeFrame < 3;
}

}
