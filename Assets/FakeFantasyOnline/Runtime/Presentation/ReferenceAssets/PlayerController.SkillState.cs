using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    public static PlayerController I;
    public float worldW = 18432f;
    public float worldH = 10752f;
    public float ppu = 100f;
    private bool sitting;
    private bool sentSitting;
    private float chantUntil;
    private float lastHurtT = -99f;
    private int workSkill;

    public bool InCombat => Time.time - lastHurtT < 5f || CombatWorld.Engaged;

    public void BreakSit()
    {
        sitting = false;
    }

    internal bool SittingForIndependentRuntime => sitting;
    internal bool ChantingForIndependentRuntime => Chanting;
    internal bool ChantFinishedForIndependentRuntime => Time.time >= chantUntil;

    private void HandleSitKey()
    {
        if (Input.GetKeyDown(KeyCode.R) && !ChatUI.TypingNow && !Win05.GmTyping &&
            Time.time >= chantUntil && GUIUtility.keyboardControl == 0)
        {
            if (sitting)
            {
                sitting = false;
            }
            else if (InCombat)
            {
                ChatUI.Add("系统", "战斗中无法打坐",
                    new Color(1f, 180f / 255f, 120f / 255f, 1f));
            }
            else
            {
                sitting = true;
            }
        }
    }

    // Called at the eligible non-dead, unmounted Update boundary. The remaining input/ride
    // branches of PlayerController are separate recovery; BreakSit itself never sends a packet.
    internal void UpdateSittingForIndependentRuntime()
    {
        if (sitting != sentSitting)
        {
            sentSitting = sitting;
            if (NetClient.I != null)
            {
                NetClient.I.SitForIndependentRuntime(sitting);
            }
        }
    }
}

}
