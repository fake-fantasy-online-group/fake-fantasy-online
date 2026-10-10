using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{

    private static string AuraEffName(string statusName) =>
        AuraEffOverride.TryGetValue(statusName, out string value) ? value : statusName;
}

}
