namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    // Official 0x06001037, static System.Boolean Win05.IsShowing(System.String).
    //
    // arm64 rva 0x3b2d84 (284 bytes) and x86_64 rva 0x3c5d80 (224 bytes) agree: the body loads the
    // static instance, returns false when it is absent, and otherwise compares the instance string
    // field at +0x28 with the argument using System.String.op_Equality (so a null argument only
    // matches when the window's own current-name field is null).
    // The frozen fields table gives Win05.cur as the System.String instance field at +0x28.
    //
    // OnGUI and ItemActions.Handle both call it with the literal "magicstoredlg"
    // (metadata usage 15345), so the question it answers is "is the magic wardrobe the window that
    // is currently showing".
    public static bool IsShowing(string name) => I != null && I.cur == name;
}
}
