using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 FamUI — the shared family-window control toolkit, recovered from the native
/// 4-method surface (tokens 0x0600035f..0x06000362) together with FamilyCreateDlg, whose OnGUI
/// draws its buttons and hit tests through <see cref="Btn"/> / <see cref="Click"/>.
/// Evidence: docs/contracts/VERSION_UPGRADE_1_5_8_FAMILY_CREATE_DLG_TRANSLATION_ARM64_V1.json and
/// ..._VERIFICATION_X86_V1.json.
/// The three static colors mirror the official .cctor statics (1,0.92,0.66,1) / (0.66,0.64,0.55,1)
/// / (0.95,0.95,0.92,1); <see cref="Btn"/>'s texture-less fallback text uses the first one.
/// </summary>
internal static class FamUI
{
    internal static readonly Color Gold = new Color(1f, 0.92f, 0.66f, 1f);
    internal static readonly Color Gray = new Color(0.66f, 0.64f, 0.55f, 1f);
    internal static readonly Color Light = new Color(0.95f, 0.95f, 0.92f, 1f);

    /// <summary>Official FamUI.Btn (0x0600035f): draw a texture or tinted fallback rect and report a
    /// left-button MouseDown inside it. Event.Use + world-click suppression happen on the hit path.</summary>
    public static bool Btn(Rect r, Texture2D tex, string fallback, Vector2 mp, float s)
    {
        bool hover = r.Contains(mp);
        if (tex != null)
        {
            float brightness = hover ? 1.25f : 1f;
            GUI.color = new Color(brightness, brightness, brightness, 1f);
            GUI.DrawTexture(r, tex);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = hover
                ? new Color(0.55f, 0.42f, 0.18f, 0.95f)
                : new Color(0.3f, 0.26f, 0.16f, 0.88f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready) BitmapFont.CenterOutline(r, fallback, Gold, s);
        }
        if (!hover || SysMsgBox.Visible) return false;
        Event e = Event.current;
        if (e == null || e.type != EventType.MouseDown || e.button != 0) return false;
        e.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }

    /// <summary>Official FamUI.Click (0x06000360): left-button MouseDown inside the rect.</summary>
    public static bool Click(Rect r, Vector2 mp)
    {
        if (!r.Contains(mp) || SysMsgBox.Visible) return false;
        Event e = Event.current;
        if (e == null || e.type != EventType.MouseDown || e.button != 0) return false;
        e.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }

    /// <summary>Official FamUI.RClick (0x06000361): the same contract with the right button.</summary>
    public static bool RClick(Rect r, Vector2 mp)
    {
        if (!r.Contains(mp) || SysMsgBox.Visible) return false;
        Event e = Event.current;
        if (e == null || e.type != EventType.MouseDown || e.button != 1) return false;
        e.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }
}
}
