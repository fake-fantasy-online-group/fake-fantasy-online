using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IArenaDraftReceiveContext
{
    // Official 1.5.8 NetClient.Dispatch case 0x8060 (arm64 0x24d2cc). The leading flag decides the
    // branch: 0 tail-calls ArenaDraftDlg.Close, every other value tail-calls ArenaDraftDlg.Show with
    // the values just read off the wire. Nothing here invents a third path or a default, and no
    // packet is sent - the window's own commands (Pick / Reroll) are not restored yet.
    void IArenaDraftReceiveContext.ReceiveArenaDraft(ArenaDraftStateData state)
    {
        if (state.Flag == 0)
        {
            ArenaDraftDlg.Close();
            return;
        }

        // Reify the native data type without changing order or values.
        var cards = new List<ArenaDraftDlg.Card>(state.Cards.Count);
        foreach (ArenaDraftCardData data in state.Cards)
        {
            cards.Add(new ArenaDraftDlg.Card
                { skillId = data.SkillId, lv = data.Level, name = data.Name, desc = data.Desc });
        }
        ArenaDraftDlg.Show(state.Round, state.Total, state.Sec, cards, state.RerollFree,
            state.RerollCost);
    }
}
}
