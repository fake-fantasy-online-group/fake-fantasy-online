using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>
/// Official 1.5.8 CursorManager cursor cone: 060001c4 Update, 060001ca SetAttackHover and
/// 060001cb SetNpcHover. 060001cd (.ctor) and 060001ce (.cctor) are field-initializer
/// constructors and are recorded as evidence-backed exclusions instead.
/// </summary>
/// <remarks>
/// The frozen field table fixes the official instance layout, and every offset quoted in the
/// evidence is that official offset. The product already declares the same instance fields in a
/// different order, which this batch does not disturb; chatTried was missing altogether and is
/// added here, so its product offset is not the official 152.
/// </remarks>
public partial class CursorManager
{
    // Official instance field chatTried, frozen offset 152, the once-only latch for the chat
    // cursor frames. CursorManager.cs never declared it, so it is added on the partial.
    private bool chatTried;

    // Official 0x060001c4, instance System.Void CursorManager.Update(). A right button press or
    // Escape cancels an active avatar fuse or tool target, then the animated cursor advances one
    // frame whenever the ten-per-second index moves.
    private void Update()
    {
        if (AvatarFuse.Active && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            AvatarFuse.Cancel();
        }

        if (ToolTarget.Active && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            ToolTarget.Cancel();
        }

        if (animFrames != null && animFrames.Length != 0)
        {
            int elapsed = (int)(Time.time * 1000f / 100f);
            int count = animFrames.Length;
            int index = elapsed % count;
            if (index != animLastFrame)
            {
                animLastFrame = index;
                Texture2D frame = animFrames[index];
                if (Valid(frame))
                {
                    int scale = cursorScale < 2 ? 1 : cursorScale;
                    Cursor.SetCursor(frame, new Vector2(animHot.x * scale, animHot.y * scale),
                        CursorMode.Auto);
                }
            }
        }
    }

    // Official 0x060001ca, instance System.Void CursorManager.SetAttackHover(System.Int32): the
    // attack comparison latch, refreshed only when the value actually changes.
    public void SetAttackHover(int kind)
    {
        if (kind == attackCmp) return;
        attackCmp = kind;
        Refresh();
    }

    // Official 0x060001cb, instance System.Void CursorManager.SetNpcHover(System.Boolean): load the
    // chat cursor frame strip at most once, keep it only when every frame converted, and refresh.
    public void SetNpcHover(bool on)
    {
        if (npcHover == on) return;

        if (on && !chatTried)
        {
            chatTried = true;
            ResLoader loader = ResLoader.I;
            if (loader != null)
            {
                int count = loader.GsaFrameCount("res/uires/cursor/Chat.gsa");
                if (count > 1)
                {
                    Texture2D[] frames = new Texture2D[count];
                    int ready = 0;
                    for (int i = 0; i < count; i++)
                    {
                        Texture2D frame = ToCursorTex(loader.LoadGsaFrame("res/uires/cursor/Chat.gsa", i));
                        frames[i] = frame;
                        ready += frame != null ? 1 : 0;
                    }
                    if (ready == count) chatFrames = frames;
                }
            }
        }

        npcHover = on;
        Refresh();
    }
}
}
