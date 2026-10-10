using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private float pickSaddleApplied;

    private void UpdatePickBox(float raiseU, float saddleXu)
    {
        if (pickBox == null) return;
        if (Mathf.Abs(raiseU - pickRaiseApplied) < 0.02f &&
            Mathf.Abs(saddleXu - pickSaddleApplied) < 0.02f) return;
        pickRaiseApplied = raiseU;
        pickSaddleApplied = saddleXu;
        if (raiseU <= 0.01f)
        {
            pickBox.size = PickSizeFoot;
            pickBox.offset = PickOffFoot;
        }
        else
        {
            pickBox.size = new Vector2(Mathf.Max(PickSizeFoot.x, 0.9f), PickSizeFoot.y + raiseU);
            pickBox.offset = new Vector2(saddleXu * 0.5f, PickOffFoot.y + raiseU * 0.5f);
        }
    }

    internal void UpdatePickBoxForIndependentRuntime(float raiseU, float saddleXu) =>
        UpdatePickBox(raiseU, saddleXu);
    internal float PickRaiseForIndependentRuntime => pickRaiseApplied;
    internal float PickSaddleForIndependentRuntime => pickSaddleApplied;
}

}
