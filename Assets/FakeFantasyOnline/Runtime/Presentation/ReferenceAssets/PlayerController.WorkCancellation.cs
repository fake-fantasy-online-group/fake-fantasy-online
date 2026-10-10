namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    public void CancelWork()
    {
        if (workSkill == 0) return;
        workSkill = 0;
        actLoop = false;
        if (actAction == 1)
        {
            actAction = -1;
            actFrames = null;
        }
        if (NetClient.I != null) NetClient.I.EndWorkForIndependentRuntime();
    }
}

}
