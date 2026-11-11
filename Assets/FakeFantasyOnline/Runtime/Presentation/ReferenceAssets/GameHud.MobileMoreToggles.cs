using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Mobile 更多 panel entries for actions that desktop reaches only by hotkey. Each entry
// calls exactly what UIManager.Update / PlayerController.Update call for that key.
public partial class GameHud
{
    internal static readonly string[] MobileMoreToggleLabels = { "决斗", "换套装", "称号", "血条", "帮助" };

    internal static void ActivateMobileMoreToggle(int entry)
    {
        switch (entry)
        {
            case 0: DuelDlg.Toggle(); break;                          // D
            case 1: Win05.ChangeSetHotkey(); break;                   // X
            case 2: PlayerController.ToggleTitleVisibility(); break;  // Z
            case 3: HeadHpBar.Toggle(); break;                        // Insert
            case 4: HelpWinDlg.Toggle(); break;                       // F12
        }
    }

    private void DrawMobileMoreToggles(float scale)
    {
        int first = ControlButtonCountForIndependentRuntime;
        for (int entry = 0; entry < MobileMoreToggleLabels.Length; entry++)
        {
            Rect rect = MobileClientPresentation.MenuItemRect(first + entry);
            if (ArtworkSkin.Button(rect, MobileMoreToggleLabels[entry], scale))
                ActivateMobileMoreToggle(entry);
        }
    }
}
}
