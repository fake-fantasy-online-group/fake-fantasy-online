using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public static partial class MobileClientPresentation
{
    private static MobileControlLayout savedControlLayout;
    private static MobileControlLayout editingControlLayout;
    private static int layoutClosedFrame = -1;
    internal static bool LayoutEditing => editingControlLayout != null;
    // The release that saves/cancels must never become a world click or chat click.
    internal static bool GameplayInputBlocked => Enabled && (LayoutEditing || layoutClosedFrame == Time.frameCount);
    internal static Rect MountRect => ResolveControlRect(MobileControlId.Mount);
    internal static Rect HealthPotionRect => ResolveControlRect(MobileControlId.HealthPotion);
    internal static Rect ManaPotionRect => ResolveControlRect(MobileControlId.ManaPotion);
    internal static Rect SitRect => ResolveControlRect(MobileControlId.Sit);

    private static MobileControlLayout CurrentControlLayout
    {
        get
        {
            if (savedControlLayout == null)
                savedControlLayout = MobileControlLayoutStore.Load(UnityEngine.Application.persistentDataPath);
            return editingControlLayout ?? savedControlLayout;
        }
    }

    internal static Rect DefaultControlRect(MobileControlId control)
    {
        Rect safe = SafeAreaGui;
        float scale = Scale;
        Vector2 attack = new Vector2(safe.xMax - 100f * scale, safe.yMax - 110f * scale);
        if (control == MobileControlId.Joystick)
            return Centered(new Vector2(safe.x + 100f * scale, attack.y), 160f * scale);
        if (control == MobileControlId.Attack) return Centered(attack, 96f * scale);
        if (control == MobileControlId.Pickup) return Centered(attack + new Vector2(74f, 64f) * scale, 48f * scale);
        if (control >= MobileControlId.Skill0 && control <= MobileControlId.Skill5)
        {
            int index = (int)control - (int)MobileControlId.Skill0;
            return Centered(attack + new Vector2(-156f + index % 3 * 72f,
                -156f + index / 3 * 72f) * scale, 64f * scale);
        }
        if (control == MobileControlId.GroupWheel)
            return Centered(attack + new Vector2(72f, -156f) * scale, 48f * scale);
        if (control == MobileControlId.HealthPotion || control == MobileControlId.ManaPotion)
        {
            int column = control == MobileControlId.HealthPotion ? 0 : 1;
            return Centered(new Vector2(MinimapRect.x + (34f + column * 64f) * scale,
                MinimapRect.yMax + 34f * scale), 52f * scale);
        }
        if (control == MobileControlId.Mount)
        {
            Rect chat = ChatRect;
            return Centered(new Vector2(chat.x - 36f * scale, chat.y + 26f * scale), 52f * scale);
        }
        Rect chatAnchor = ChatRect;
        return Centered(new Vector2(chatAnchor.x - 36f * scale, chatAnchor.y - 38f * scale), 52f * scale);
    }

    internal static Rect ResolveControlRect(MobileControlId control) =>
        CurrentControlLayout.ResolveRect(control, DefaultControlRect(control), SafeAreaGui, Scale);
    internal static float ControlScale(MobileControlId control) =>
        ResolveControlRect(control).width / DefaultControlRect(control).width;
    internal static Rect FixedActionRect(MobileFixedAction action) =>
        action == MobileFixedAction.Mount ? MountRect :
        action == MobileFixedAction.HealthPotion ? HealthPotionRect :
        action == MobileFixedAction.ManaPotion ? ManaPotionRect : SitRect;

    internal static void BeginLayoutEdit()
    {
        if (LayoutEditing) return;
        editingControlLayout = CurrentControlLayout.Clone();
        MenuExpanded = ChatExpanded = GroupWheelOpen = false;
        ActiveSkillButton = -1;
        ClientInput.CancelAll();
    }
    internal static bool SaveLayoutEdit(out string error)
    {
        error = string.Empty;
        if (!LayoutEditing) return true;
        if (!MobileControlLayoutStore.TrySave(UnityEngine.Application.persistentDataPath, editingControlLayout))
        {
            error = "保存失败，请检查存储空间后重试。当前调整仍然保留。";
            return false;
        }
        savedControlLayout = editingControlLayout;
        editingControlLayout = null;
        layoutClosedFrame = Time.frameCount;
        ClientInput.CancelAll();
        return true;
    }
    internal static void CancelLayoutEdit()
    {
        if (!LayoutEditing) return;
        editingControlLayout = null;
        layoutClosedFrame = Time.frameCount;
        ClientInput.CancelAll();
    }
    internal static void ResetLayoutEdit() { if (LayoutEditing) editingControlLayout.Clear(); }
    internal static void SetControlCenter(MobileControlId control, Vector2 center)
    {
        if (!LayoutEditing) return;
        float size = editingControlLayout.TryGet(control, out MobileControlPlacement value) ? value.Scale : 1f;
        editingControlLayout.Set(control, center, size, DefaultControlRect(control), SafeAreaGui, Scale);
    }
    internal static void SetControlScale(MobileControlId control, float size)
    {
        if (!LayoutEditing) return;
        editingControlLayout.Set(control, ResolveControlRect(control).center, size,
            DefaultControlRect(control), SafeAreaGui, Scale);
    }
}
}
