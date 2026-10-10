using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CursorManager
{
    private bool repairTried;
    public void SetRepairMode(bool on)
    {
        if (repairMode == on) return;
        if (on && !repairTried)
        {
            repairTried = true;
            ResLoader loader = ResLoader.I;
            Texture2D source = loader != null ? loader.LoadGsaFrame2021First("res/effect/repaircursor.gsa", 0) : null;
            repairCur = ToCursorTex(source);
        }
        repairMode = on;
        Refresh();
    }
}
}
