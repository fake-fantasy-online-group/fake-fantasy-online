using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 JobUI — the shared job-window control toolkit, recovered from the native 5-method
/// surface (tokens 0x06000559..0x0600055d) together with JobRightDlg, whose OnGUI draws its buttons
/// and hit tests through <see cref="Btn"/> / <see cref="Click"/>.
/// Evidence: docs/contracts/VERSION_UPGRADE_1_5_8_JOB_RIGHT_DLG_TRANSLATION_ARM64_V1.json and
/// ..._VERIFICATION_X86_V1.json. The four job windows that use it are JobCustomDlg, JobOfferDlg,
/// JobRightDlg and FamilyMailDlg.
/// The two static colours keep their official metadata names: GOLD is byte-identical to the value
/// exposed as FamUI.Gold and GRAY to FamUI.Gray (both .cctors read the same literal pool).
/// </summary>
internal static class JobUI
{
    /// <summary>Official static JobUI.GOLD (+0x0), value (1,0.92,0.66,1).</summary>
    internal static readonly Color GOLD = new Color(1f, 0.92f, 0.66f, 1f);

    /// <summary>Official static JobUI.GRAY (+0x10), value (0.66,0.64,0.55,1).</summary>
    internal static readonly Color GRAY = new Color(0.66f, 0.64f, 0.55f, 1f);

    /// <summary>Official static JobUI._edit (+0x20): the lazily built shared edit style.</summary>
    private static GUIStyle _edit;

    /// <summary>Official JobUI.Btn (0x06000559): draw a texture or a tinted fallback rect and report
    /// a left-button MouseDown inside it. Event.Use + world-click suppression happen on the hit
    /// path.</summary>
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
            if (BitmapFont.Ready) BitmapFont.CenterOutline(r, fallback, GOLD, s);
        }
        if (!hover || SysMsgBox.Visible) return false;
        Event e = Event.current;
        if (e == null || e.type != EventType.MouseDown || e.button != 0) return false;
        e.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }

    /// <summary>Official JobUI.Click (0x0600055a): left-button MouseDown inside the rect.</summary>
    public static bool Click(Rect r, Vector2 mp)
    {
        if (!r.Contains(mp) || SysMsgBox.Visible) return false;
        Event e = Event.current;
        if (e == null || e.type != EventType.MouseDown || e.button != 0) return false;
        e.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }

    /// <summary>Official JobUI.Box (0x0600055b): fill the rect with (0,0,0,0.16). 1.5.8 has no caller
    /// for this member; it is restored on the official surface and left uncalled.</summary>
    public static void Box(Rect r)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    /// <summary>Official JobUI.Edit (0x0600055c): lazily built shared edit style (CnFont, MiddleLeft,
    /// white text) whose font size tracks the UI scale at 12 px per factor step.</summary>
    public static GUIStyle Edit(float s)
    {
        if (_edit == null)
        {
            _edit = new GUIStyle(GUIStyle.none);
            _edit.font = NpcChatter.CnFont();
            _edit.alignment = TextAnchor.MiddleLeft;
            _edit.normal.textColor = Color.white;
        }
        _edit.fontSize = (int)System.Math.Round((double)(s > 0f ? s * 12f : 12f));
        return _edit;
    }
}
}
