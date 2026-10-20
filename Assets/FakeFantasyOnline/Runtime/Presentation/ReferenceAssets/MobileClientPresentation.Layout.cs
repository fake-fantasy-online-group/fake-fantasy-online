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
    // Captured comfortable layout: 2616 x 1212 landscape, GUI safe area
    // (104, 0, 2512, 1212), UI scale 1212 / 540. Convert the safe-normalized
    // centers to logical offsets around the unchanged attack/joystick anchor.
    // Only the lower skill pair, upper skill pair and potion row are aligned
    // to their respective midpoint (at most 2.83 physical pixels on that phone).
    private static readonly Vector2[] DefaultSkillOffsets =
    {
        new Vector2(-177.18376f, 35.76483f), new Vector2(-106.85673f, 35.76483f),
        new Vector2(-102.74753f, -33.18696f), new Vector2(-35.82306f, -98.36820f),
        new Vector2(31.81393f, -166.51180f), new Vector2(37.04461f, -98.36820f)
    };
    private const float DefaultAttackScale = 1.3344183f;
    // The left edge of Skill0, including its 32-unit half-width.
    private const float DefaultClusterLeftExtent = 209.18376f;

    private static float DefaultClusterScale
    {
        get
        {
            // Keep the captured arrangement and sizes on wide phones. On narrower
            // safe areas, fit the whole default cluster uniformly beside chat instead
            // of compressing only the centers into overlapping touch targets.
            // ResolveScale's width / 940 cap keeps this factor >= .8767; Fit still
            // enforces the 44-unit touch minimum for pickup and the group wheel.
            float scale = Scale;
            float attackX = SafeAreaGui.xMax - 100f * scale;
            float available = attackX - ChatRect.xMax - 8f * scale;
            return Mathf.Max(MobileControlLayout.MinScale,
                Mathf.Min(1f, available / (DefaultClusterLeftExtent * scale)));
        }
    }

    internal static float DefaultControlScale(MobileControlId control) =>
        control == MobileControlId.Joystick || control == MobileControlId.Mount || control == MobileControlId.Sit
            ? 1f : DefaultClusterScale * (control == MobileControlId.Attack ? DefaultAttackScale : 1f);

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
        // Keep the original size baselines: saved v1 multipliers must not be
        // multiplied by the new defaults, especially the larger attack button.
        if (control == MobileControlId.Attack) return Centered(attack, 96f * scale);
        float clusterScale = scale * DefaultClusterScale;
        if (control == MobileControlId.Pickup)
            return Centered(attack + new Vector2(-170.38122f, -30.00996f) * clusterScale, 48f * scale);
        if (control >= MobileControlId.Skill0 && control <= MobileControlId.Skill5)
        {
            int index = (int)control - (int)MobileControlId.Skill0;
            return Centered(attack + DefaultSkillOffsets[index] * clusterScale, 64f * scale);
        }
        if (control == MobileControlId.GroupWheel)
            return Centered(attack + new Vector2(-99.79882f, -92.52787f) * clusterScale, 48f * scale);
        if (control == MobileControlId.HealthPotion || control == MobileControlId.ManaPotion)
        {
            float x = control == MobileControlId.HealthPotion ? -107.37580f : -45.55954f;
            return Centered(attack + new Vector2(x, -173.83856f) * clusterScale, 52f * scale);
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
        CurrentControlLayout.ResolveRect(control, DefaultControlRect(control), SafeAreaGui, Scale, DefaultControlScale(control));
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
        float size = editingControlLayout.TryGet(control, out MobileControlPlacement value) ? value.Scale : DefaultControlScale(control);
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
