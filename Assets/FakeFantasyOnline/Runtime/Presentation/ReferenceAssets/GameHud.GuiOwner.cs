using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    private ReferenceResourceLoader guiLoader;
    private ReferenceFirstWorldHudResources guiResources;
    private ReferenceFirstWorldPlayerStatusHud guiPlayerStatus;
    private ReferenceFirstWorldTargetHud guiTarget;
    private ReferenceFirstWorldHotbar guiHotbar;
    private ReferenceFirstWorldMinimapHud guiMinimap;
    private ReferenceFirstWorldBuffHud guiBuffs;
    private ReferenceFirstWorldControlBar guiControls;
    private ReferenceFirstWorldWindowOwner guiHost;

    internal ReferenceFirstWorldHudResources GuiResourcesForIndependentRuntime => guiResources;
    internal ReferenceFirstWorldPlayerStatusHud PlayerStatusForIndependentRuntime => guiPlayerStatus;
    internal ReferenceFirstWorldTargetHud TargetForIndependentRuntime => guiTarget;
    internal ReferenceFirstWorldHotbar HotbarForIndependentRuntime => guiHotbar;
    internal ReferenceFirstWorldMinimapHud MinimapForIndependentRuntime => guiMinimap;
    internal ReferenceFirstWorldBuffHud BuffsForIndependentRuntime => guiBuffs;
    internal ReferenceFirstWorldControlBar ControlsForIndependentRuntime => guiControls;
    internal bool LastGuiDrawSucceededForIndependentRuntime { get; private set; }

    // These source views carry already-restored drawing bodies. They belong to the original
    // independent GameHud root, not to a map borrower. The resource loader is already persistent.
    internal bool InitializeGuiViewsForIndependentRuntime(ReferenceResourceLoader loader,
        WorldEntryRecord entry, out ReferenceFirstWorldWindowFailure failure)
    {
        failure = ReferenceFirstWorldWindowFailure.None;
        guiLoader = loader;
        if (guiResources == null) guiResources = new ReferenceFirstWorldHudResources(this);
        if (guiPlayerStatus == null &&
            (!ReferenceFirstWorldPlayerStatusHud.TryCreate(loader, guiResources, entry, out guiPlayerStatus) ||
             guiPlayerStatus == null || !guiPlayerStatus.Ready))
        {
            failure = ReferenceFirstWorldWindowFailure.InvalidPlayerStatusHud;
            return false;
        }
        if (guiControls == null &&
            (!ReferenceFirstWorldControlBar.TryCreate(loader, guiResources, out guiControls) ||
             guiControls == null || !guiControls.Ready))
        {
            failure = ReferenceFirstWorldWindowFailure.InvalidControlBar;
            return false;
        }
        if (guiTarget == null &&
            (!ReferenceFirstWorldTargetHud.TryCreate(loader, guiResources, out guiTarget) ||
             guiTarget == null || !guiTarget.Ready))
        {
            failure = ReferenceFirstWorldWindowFailure.InvalidTargetHud;
            return false;
        }
        if (guiBuffs == null &&
            (!ReferenceFirstWorldBuffHud.TryCreate(loader, out guiBuffs) ||
             guiBuffs == null || !guiBuffs.Ready))
        {
            failure = ReferenceFirstWorldWindowFailure.InvalidBuffHud;
            return false;
        }
        if (guiMinimap == null &&
            (!ReferenceFirstWorldMinimapHud.TryCreateNativeView(loader, guiResources, out guiMinimap) ||
             guiMinimap == null || !guiMinimap.Ready))
        {
            failure = ReferenceFirstWorldWindowFailure.InvalidMinimapHud;
            return false;
        }
        if (guiHotbar == null)
            guiHotbar = ReferenceFirstWorldHotbar.CreateNativeView(guiResources);

        guiPlayerStatus.BindHudOwnerForIndependentRuntime(this);
        guiTarget.BindHudOwnerForIndependentRuntime(this);
        guiHotbar.BindHudOwnerForIndependentRuntime(this);
        guiMinimap.BindNativeOwnerForIndependentRuntime(this);
        guiControls.BindHudOwnerForIndependentRuntime(this);
        return true;
    }

    internal void BindGuiHostForIndependentRuntime(ReferenceFirstWorldWindowOwner host,
        Action activationObserver)
    {
        guiHost = host;
        guiHotbar.BindActivationObserverForIndependentRuntime(activationObserver);
    }

    internal void UnbindGuiHostForIndependentRuntime(ReferenceFirstWorldWindowOwner host)
    {
        if (!ReferenceEquals(guiHost, host)) return;
        guiHost = null;
        guiHotbar.BindActivationObserverForIndependentRuntime(null);
    }

    // 06000457: sx/sy are already pixel coordinates; only w/h use this receiver's scale.
    internal Rect Px(float sx, float sy, float w, float h)
    {
        float scale = s;
        return new Rect(sx, sy, w * scale, h * scale);
    }

    // Actual original Unity message. No map-Built/session/borrower gate or manual replay.
    // Each native leaf retains its own event, state, normal-return and exception behavior.
    private void OnGUI()
    {
        LastGuiDrawSucceededForIndependentRuntime = false;
        DrawResourcePrefixForIndependentRuntime(out int screenWidth, out int screenHeight);
        if (MobileClientPresentation.Enabled)
        {
            bool enabled=GUI.enabled;
            try { GUI.enabled=enabled && !MobileClientPresentation.ModalOpen; DrawMobileHud(); }
            finally { GUI.enabled=enabled; }
            return;
        }
        float elapsedSeconds = Time.time;
        float hudScale = s;
        bool ready = guiPlayerStatus.DrawAvatarForIndependentRuntime(elapsedSeconds, screenHeight);

        ready &= guiTarget.Draw(out ReferenceFirstWorldTargetHudAction targetAction, nativeInvocation: true);
        guiHost?.ObserveNativeTargetHudForIndependentRuntime(targetAction);

        Win05 skills = Win05.I;
        int heldSkillId = DragSkillIdForIndependentRuntime;
        guiHotbar.BindExternalHeldTextureForIndependentRuntime(DragSkillIconForIndependentRuntime);
        if (!guiHotbar.Draw(elapsedSeconds, heldSkillId,
            skills != null ? skills.HeldSkillIconId : 0,
            (WorldHotbarSlotKind)DragKindForIndependentRuntime,
            out _, out _, out bool releasedHeldSkill, screenWidth, screenHeight,
            observeVisibleState: false, nativeInvocation: true))
        {
            guiHost?.ObserveNativeHudFailureForIndependentRuntime(ReferenceFirstWorldWindowFailure.InvalidHotbar);
            ready = false;
        }
        // Native hotbar release already ran before DrawDragIcon. This is the retained
        // Win05 icon-id observation, never an independent release or an Event.Use.
        if (releasedHeldSkill && skills != null) skills.ClearHeldSkill();

        if (!guiMinimap.DrawForNativeOwner(hudScale, screenWidth, screenHeight))
        {
            guiHost?.ObserveNativeHudFailureForIndependentRuntime(ReferenceFirstWorldWindowFailure.InvalidMinimapHud);
            ready = false;
        }
        if (guiMinimap.ConsumeWorldMapRequest()) guiHost?.ObserveNativeWorldMapRequestForIndependentRuntime();
        if (!guiBuffs.DrawForIndependentRuntime(elapsedSeconds, hudScale, screenWidth, this, nativeInvocation: true))
        {
            guiHost?.ObserveNativeHudFailureForIndependentRuntime(ReferenceFirstWorldWindowFailure.InvalidBuffHud);
            ready = false;
        }
        DrawRaidTimer(s);
        DrawZoneBar(s);
        ready &= guiPlayerStatus.DrawExperienceForIndependentRuntime(elapsedSeconds, screenWidth, screenHeight);
        ready &= guiControls.Draw(out ReferenceFirstWorldControlBarAction action, screenWidth, screenHeight);
        guiHost?.ObserveNativeControlHudForIndependentRuntime(action);
        LastGuiDrawSucceededForIndependentRuntime = ready;
    }
}
}
