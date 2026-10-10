using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // Original 060000ed: pass both text values unchanged to the native menu owner.
    internal static void LineMenuForIndependentRuntime(Rect lineRect, string who, string fullText)
    {
        Event ev = Event.current;
        if (ev.type != EventType.MouseDown || ev.button != 1 || !lineRect.Contains(ev.mousePosition)) return;
        int id;
        if (string.IsNullOrEmpty(who) || who == CharData.name)
        {
            id = 0;
            who = "";
        }
        else id = CombatWorld.FindPlayerIdByName(who);
        string copyBlock = RecentText(30);
        PlayerActionMenu.Open(id, who, fullText, copyBlock);
        ev.Use();
    }
}
}
