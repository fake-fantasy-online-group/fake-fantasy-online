using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Direct source recovery of the complete official 1.5.8 UIScale owner.
/// </summary>
public static class UIScale
{
    private const float DESIGN_H = 600f;
    private const float MIN = 1f;
    private const float MAX = 4f;
    private const float USER_MIN = 0.6f;
    private const float USER_MAX = 2f;
    private const string USER_KEY = "ui_scale";

    private static float _user = -1f;

    public static float User
    {
        get
        {
            if (_user < 0f)
            {
                _user = Mathf.Clamp(
                    PlayerPrefs.GetFloat(USER_KEY, 1f),
                    USER_MIN,
                    USER_MAX);
            }

            return _user;
        }
        set
        {
            _user = Mathf.Clamp(value, USER_MIN, USER_MAX);
            PlayerPrefs.SetFloat(USER_KEY, _user);
            PlayerPrefs.Save();
        }
    }

    public static float Factor
    {
        get
        {
            float automatic;
            if (Screen.dpi > 1f)
            {
                float dpi = Screen.dpi;
                automatic = ResolveDpiFactor(dpi, Screen.height);
            }
            else
            {
                automatic = ResolveHeightFactor(Screen.height);
            }

            return ResolveFactor(automatic, User);
        }
    }

    // The official Factor arithmetic is shared with the focused audit. The public
    // getter remains the owner of Screen reads and the cached User preference.
    internal static float ResolveHeightFactor(int screenHeight)
    {
        return screenHeight / DESIGN_H;
    }

    internal static float ResolveDpiFactor(float dpi, int screenHeight)
    {
        return Mathf.Min(dpi / 96f, ResolveHeightFactor(screenHeight));
    }

    internal static float ResolveFactor(float automatic, float user)
    {
        return Mathf.Clamp(Mathf.Round(automatic * user), MIN, MAX);
    }

    public static Rect Snap(Rect r)
    {
        return new Rect(
            Mathf.Round(r.x),
            Mathf.Round(r.y),
            Mathf.Round(r.width),
            Mathf.Round(r.height));
    }

    public static float SnapX(float v)
    {
        return Mathf.Round(v);
    }

    public static float SnapY(float v)
    {
        return Mathf.Round(v);
    }

    public static void SnapDraw(Rect r, Texture2D t)
    {
        if (t != null)
        {
            GUI.DrawTexture(Snap(r), t);
        }
    }
}

}
