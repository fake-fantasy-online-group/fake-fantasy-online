using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud : MonoBehaviour
{
    // Official GameHud static slots of 0x06000448 (arm64 0x43d234 = 4444724, x86_64 0x454f10 =
    // 4542224): +0x90 good box, +0x98 bad box, +0xa0 the one-shot attempt flag.
    private static Texture2D goodExceptBox;
    private static Texture2D badExceptBox;
    private static bool exceptBoxAttempted;

    // metadata_usage 15673 and 15666 of the same body.
    private const string GoodExceptBoxPath = "res/effect/goodexceptbox.gsa";
    private const string BadExceptBoxPath = "res/effect/badexceptbox.gsa";

    /// <summary>
    /// Official GameHud.ExceptBox (0x06000448). One <see cref="ResLoader.ReadRes"/> per box,
    /// decoded by the official DimgDecoder.DecodeAnim (0x06000b91) and reduced to frame 0; the
    /// attempt flag is raised before the first read, so a missing resource, a failed decode or a
    /// null ResLoader.I is latched for the whole process and never retried. The two boxes are read
    /// one after the other with no early exit between them, a null byte array skips the decode of
    /// that box only, and no TexScale registration happens here: the frame scale is registered
    /// inside DimgDecoder itself, which is why 0x06000448 depends on 0x06000b91 and 0x06000c07
    /// alone.
    /// </summary>
    public static Texture2D ExceptBox(bool good)
    {
        if (!exceptBoxAttempted)
        {
            exceptBoxAttempted = true;
            if (ResLoader.I)
            {
                byte[] goodData = ResLoader.I.ReadRes(GoodExceptBoxPath);
                if (goodData != null)
                {
                    DimgDecoder.Anim goodAnimation = DimgDecoder.DecodeAnim(goodData);
                    if (goodAnimation.ok && goodAnimation.f != null && goodAnimation.f.Length > 0)
                    {
                        goodExceptBox = goodAnimation.f[0].tex;
                    }
                }

                byte[] badData = ResLoader.I.ReadRes(BadExceptBoxPath);
                if (badData != null)
                {
                    DimgDecoder.Anim badAnimation = DimgDecoder.DecodeAnim(badData);
                    if (badAnimation.ok && badAnimation.f != null && badAnimation.f.Length > 0)
                    {
                        badExceptBox = badAnimation.f[0].tex;
                    }
                }
            }
        }

        return good ? goodExceptBox : badExceptBox;
    }
}

}
