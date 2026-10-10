using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class UIManager
{
    // Official UIManager.Update (06000fa8) arm64 0x3a7ee8 loads KeyCode 293 (F12) and calls
    // Input.GetKeyDown at 0x10a4594; a false result branches away and a true result falls into
    // HelpWinDlg.Toggle at 0x3a7f14. The same enumeration shape drives the neighbouring binding at
    // 0x3a7f18, where KeyCode 277 is Insert and the target is HeadHpBar.Toggle — the source-owned
    // UIManager.HeadHpBar helper already spells that one as KeyCode.Insert.
    internal static void UpdateHelpShortcutForIndependentRuntime()
    {
        if (Input.GetKeyDown(KeyCode.F12)) HelpWinDlg.Toggle();
    }
}

}
