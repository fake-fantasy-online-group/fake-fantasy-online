using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Fit the complete original PC surface, including its portrait and every option.
// No alternate artwork, pagination, text rewriting or action ownership lives here.
internal sealed class MobileNpcDialogLayout
{
    internal readonly Rect Panel;
    internal readonly float Scale;

    internal MobileNpcDialogLayout(Rect safe, float preferredScale, int count,
        float portraitWidth, float portraitHeight)
    {
        float optionHeight = count > 0 ? Math.Max(80f, 8f + count * 16f) + 2f : 0f;
        float width = Math.Max(451f, portraitWidth);
        float height = portraitHeight + 160f + optionHeight;
        float margin = Math.Min(6f * preferredScale, Math.Min(safe.width, safe.height) * .05f);
        Scale = Math.Min(preferredScale,
            Math.Min((safe.width - 2f * margin) / width, (safe.height - 2f * margin) / height));
        float x = safe.x + (safe.width - 451f * Scale) * .5f;
        float top = safe.y + margin + portraitHeight * Scale;
        float bottom = safe.y + safe.height - margin - (160f + optionHeight) * Scale;
        float y = Math.Max(top, Math.Min(safe.y + 190f * Scale, bottom));
        Panel = new Rect(x, y, 451f * Scale, 160f * Scale);
    }
}
}
