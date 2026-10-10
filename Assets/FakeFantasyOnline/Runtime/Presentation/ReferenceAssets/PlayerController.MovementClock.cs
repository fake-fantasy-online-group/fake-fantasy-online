using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private float moveDtAvg;
    public float moveSpeed = 400f;
    public float pxPerSpeed = 2f;

    private float MoveDt()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return 0f;
        if (dt > 0.1f)
        {
            moveDtAvg = 0f;
            return dt;
        }
        moveDtAvg = moveDtAvg <= 0f ? dt : moveDtAvg + (dt - moveDtAvg) * 0.05f;
        return Mathf.Clamp(moveDtAvg, dt * 0.8f, dt * 1.2f);
    }

    private float MoveStepPx()
    {
        if (CharData.attrs != null && CharData.attrs.Length > 9 && CharData.attrs[9] > 0)
            return (float)CharData.attrs[9] * pxPerSpeed;
        return moveSpeed;
    }

    // Original movement-branch call order; the caller has already selected target/waypoint distance.
    internal float MovementStepForIndependentRuntime()
    {
        float speed = MoveStepPx();
        float dt = MoveDt();
        return speed * dt;
    }
    internal float MoveDeltaAverageForIndependentRuntime => moveDtAvg;
}

}
