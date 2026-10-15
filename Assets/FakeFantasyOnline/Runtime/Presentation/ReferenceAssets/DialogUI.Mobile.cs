using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    // Mobile shares the original PC painter, text, portrait and option actions.
    // Only its uniform scale and position change to respect the device safe area.
    private MobileNpcDialogLayout MobileLayout()
    {
        return new MobileNpcDialogLayout(MobileClientPresentation.SafeAreaGui,
            MobileClientPresentation.Scale, optLabels.Count,
            pic != null ? TexScale.W(pic) : 0f,
            pic != null ? TexScale.H(pic) : 0f);
    }
}
}
