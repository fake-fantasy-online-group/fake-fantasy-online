using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    private static int dragSkillId;
    private static Texture2D dragSkillIcon;
    private static int dragKind;

    public static void PickupSlot(int id, Texture2D icon, int kind)
    {
        dragSkillId = id;
        dragSkillIcon = icon;
        dragKind = kind;
        dragFromSlot = -1;
    }

    public static void PickupSkill(int skillId, Texture2D icon, bool isPet = true)
        => PickupSlot(skillId, icon, isPet ? 1 : 0);

    internal static void DrawDragIcon(float scale)
    {
        if (dragSkillId == 0 || dragSkillIcon == null) return;
        Vector2 mouse = Event.current.mousePosition;
        GUI.DrawTexture(new Rect(mouse.x - 16f * scale, mouse.y - 16f * scale,
            32f * scale, 32f * scale), dragSkillIcon);
    }

    internal static int PressSlotForIndependentRuntime { get => pressSlot; set => pressSlot = value; }
    internal static int DragFromSlotForIndependentRuntime => dragFromSlot;

    // Original OnGUI MouseDrag state writes; admission remains in the current input adapter.
    internal static void BeginSlotDragForIndependentRuntime(int source)
    {
        dragSkillId = hotSkill[source];
        dragSkillIcon = HotIcon(source);
        dragKind = hotKind[source];
        dragFromSlot = source;
        ClearHotbarSlotForIndependentRuntime(source);
    }

    internal static void ClearDragForIndependentRuntime()
    {
        dragSkillId = 0;
        dragSkillIcon = null;
        dragFromSlot = -1;
        pressSlot = -1;
    }

    internal static int DragSkillIdForIndependentRuntime => dragSkillId;
    internal static Texture2D DragSkillIconForIndependentRuntime => dragSkillIcon;
    internal static int DragKindForIndependentRuntime => dragKind;
    internal static void DrawDragIconForIndependentRuntime(float scale)
    {
        int previous = GUI.depth;
        try { GUI.depth = 50; DrawDragIcon(scale); }
        finally { GUI.depth = previous; }
    }
}

}
