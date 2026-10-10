using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    public static Color TargetNameColor(ISelectable target)
    {
        if (target.SelFriendly)
            return new Color(0.3019607961177826f, 0.772549033164978f, 0.04313725605607033f, 1f);
        if (target.SelLevel <= 0)
            return new Color(0.9254902005195618f, 0.843137264251709f, 0.12941177189350128f, 1f);
        int difference = unchecked(target.SelLevel - CharData.level);
        if (difference > 9)
            return new Color(0.9647058844566345f, 0.07058823853731155f, 0.7843137383460999f, 1f);
        if (difference > 2)
            return new Color(0.9921568632125854f, 0.35686275362968445f, 0.0784313753247261f, 1f);
        if (difference >= 0)
            return new Color(0.9254902005195618f, 0.843137264251709f, 0.12941177189350128f, 1f);
        if (difference > -10)
            return new Color(0.3019607961177826f, 0.772549033164978f, 0.04313725605607033f, 1f);
        return new Color(0.7411764860153198f, 0.7019608020782471f, 0.7019608020782471f, 1f);
    }
}

}
