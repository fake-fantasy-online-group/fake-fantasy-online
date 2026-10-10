using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private string soulFxCsv = string.Empty;
    private string soulFxRaw = string.Empty;

    public void SetSoulFx(string csv)
    {
        soulFxRaw = csv ?? string.Empty;
        ApplySoulFx();
    }

    public void RefreshSoulFx() => ApplySoulFx();

    private void ApplySoulFx()
    {
        string text = soulFxRaw;
        if (!string.IsNullOrEmpty(soulFxRaw) && CharData.soulFxPart.Count > 0)
        {
            var keep = new List<string>();
            foreach (string part in soulFxRaw.Split(','))
            {
                string name = part.Trim();
                if (name.Length == 0) continue;
                if (CharData.soulFxPart.TryGetValue(name, out int slot) && !CharData.EquipShown(slot, 1)) continue;
                keep.Add(name);
            }
            text = string.Join(",", keep);
        }
        if (text == soulFxCsv && !(actorFx != null && actorFx.SoulFxPending)) return;
        soulFxCsv = text;
        EnsureActorFx();
        actorFx.SyncSoulFx(text.Length == 0 ? null : text.Split(','));
    }
}

}
