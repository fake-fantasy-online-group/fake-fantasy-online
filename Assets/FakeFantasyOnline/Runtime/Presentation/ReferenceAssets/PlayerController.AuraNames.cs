using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{

    internal static string AuraEffName(string statusName) =>
        AuraEffOverride.TryGetValue(statusName, out string value) ? value : statusName;
}

}
