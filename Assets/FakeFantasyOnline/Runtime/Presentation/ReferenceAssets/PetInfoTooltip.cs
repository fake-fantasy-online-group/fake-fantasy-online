using PetBrief = FakeFantasyOnline.Application.PetBrief;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class PetInfoTooltip
{
    private static readonly string[] HabitName = { "", "草食", "肉食", "杂食", "灵食" };
    private static bool has;
    private static PetBrief reqBrief;
    private static Vector2 reqMouse;
    private static float reqS;
    private static string reqHint;
    private static PetTipRenderer renderer;

    public static void Show(PetBrief b, Vector2 mouse, float s, string actionHint = null)
    {
        reqBrief = b;
        reqMouse = mouse;
        reqS = s;
        reqHint = actionHint;
        has = true;
        if (renderer == null)
            renderer = new GameObject("PetTipRenderer").AddComponent<PetTipRenderer>();
    }

    private class PetTipRenderer : MonoBehaviour
    {
        private void OnGUI()
        {
            GUI.depth = 1;
            if (has)
            {
                Draw(reqBrief, reqMouse, reqS, reqHint);
                if (Event.current.type == EventType.Repaint) has = false;
            }
        }
    }
}
}
