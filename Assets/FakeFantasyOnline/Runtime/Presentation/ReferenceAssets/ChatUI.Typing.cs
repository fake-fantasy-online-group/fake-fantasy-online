namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // Official 060000e6/060000e7: the property stores state; it does not query input.
    public static bool TypingNow { get; private set; }

    internal static void SetTypingForIndependentRuntime(bool typing) => TypingNow = typing;
}
}
