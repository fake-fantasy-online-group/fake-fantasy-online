namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class FamilyDlg
{
    // Adapter-only drawing ownership for the family windows still hosted by a map.
    // Main FamilyDlg receives its own Unity callbacks and never borrows this driver.
    // A map owns no family data, selection, editor state, textures or command callbacks.
    private static ReferenceFirstWorldWindowOwner independentGuiDriver;
    internal static bool HasGuiDriverForIndependentRuntime => independentGuiDriver != null;
    internal static bool IsGuiDriverForIndependentRuntime(ReferenceFirstWorldWindowOwner driver) =>
        ReferenceEquals(independentGuiDriver, driver);

    internal static void BindGuiDriverForIndependentRuntime(ReferenceFirstWorldWindowOwner driver)
    {
        independentGuiDriver = driver;
        bool automatic = driver == null;
    }

    internal static void UnbindGuiDriverForIndependentRuntime(ReferenceFirstWorldWindowOwner driver)
    {
        // An older map cannot detach the replacement map's driver.
        if (ReferenceEquals(independentGuiDriver, driver))
            BindGuiDriverForIndependentRuntime(null);
    }
}
}
