using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class ChatUI
{
    // Original instance slots +0x20..+0x68. loaded is distinct from static TypingNow +0x68.
    private Texture2D inputBar;
    private Texture2D winUp;
    private Texture2D winMid;
    private Texture2D winDown;
    private Texture2D[] dropBtn;
    private Texture2D[] sendBtn;
    private Texture2D[] faceBtn;
    private Texture2D[] sizeBtn;
    private Texture2D[][] sideBtn;
    private bool loaded;

    // 060000f3, complete inherited WBC078/079 body: fresh array, ordered LoadUI calls,
    // exact five-part path, no readiness gate or fallback for a null texture/loader.
    private Texture2D[] Frames(ResLoader R, string name, int n)
    {
        Texture2D[] frames = new Texture2D[n];
        for (int i = 0; i < n; i++)
        {
            frames[i] = R.LoadUI("ui/chatbar/" + name + "_f" + i + ".png");
        }

        return frames;
    }

    // 060000f4. The latch is set before capturing the loader, even on the normal absent-
    // loader return. Each field and then the jagged array are published incrementally.
    private void Load()
    {
        loaded = true;
        ResLoader R = ResLoader.I;
        if (R == null) return;

        inputBar = R.LoadUI("ui/chatbar/inputbar.png");
        winUp = R.LoadUI("ui/chatbar/winup.png");
        winMid = R.LoadUI("ui/chatbar/winmid.png");
        winDown = R.LoadUI("ui/chatbar/windown.png");
        dropBtn = Frames(R, "dropbtn", 3);
        sendBtn = Frames(R, "sendbtn", 2);
        faceBtn = Frames(R, "facebtn", 2);
        sizeBtn = Frames(R, "sizebtn", 3);
        sideBtn = new Texture2D[SideBtns.Length][];
        for (int i = 0; i < SideBtns.Length; i++)
        {
            // Capture the destination before either RHS. Its null/bounds check belongs to
            // the later element store. The non-size arm re-reads the static tuple's name.
            Texture2D[][] destination = sideBtn;
            int destinationIndex = i;
            Texture2D[] frames = SideBtns[i].Name == "sizebtn"
                ? sizeBtn
                : Frames(R, SideBtns[i].Name, 3);
            destination[destinationIndex] = frames;
        }

        // 1.6.0 Load (060000f9), ARM64 0x235804..0x2358d0: this resource uses
        // Pkg then Pkg24 directly. Publish the array before copying decoded textures.
        byte[] checkboxData = R.Pkg?.Read("res/uires/common/checkbox.gsa")
            ?? R.Pkg24?.Read("res/uires/common/checkbox.gsa");
        if (checkboxData != null)
        {
            DimgDecoder.Anim animation = DimgDecoder.DecodeAnim(checkboxData);
            if (animation.ok && animation.f != null)
            {
                checkbox = new Texture2D[animation.f.Length];
                for (int i = 0; i < animation.f.Length; i++)
                    checkbox[i] = animation.f[i].tex;
            }
        }

        if (chatLines.Count == 0)
        {
            Add("系统", "欢迎来到 fake-fantasy-online", new Color(1f, 220f / 255f, 120f / 255f, 1f),
                string.Empty);
        }
    }
}

}
