using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    // Server-spawned only after the guardian completion is durable. Reuses the
    // native NPC dialogue and leave command; the server verifies membership,
    // completion, current dialogue target and distance before moving the player.
    private void AddRiftExitOptions()
    {
        if (!string.Equals(NpcName, "归元道使", StringComparison.Ordinal)) return;
        Greeting = "劫影已散，尘缘未了。且收好洞天所得，贫道送诸位归还人间。";
        Add(new ReferenceNpcDialogOption("送我离开洞天", () =>
        {
            Visible = false;
            SysMsgBox.Show("可已拾妥洞天所得？洞天收束后，可补领未拾之物。",
                () => NetClient.I?.GmCmdForIndependentRuntime("trialleave"), null);
        }));
    }
}
}
