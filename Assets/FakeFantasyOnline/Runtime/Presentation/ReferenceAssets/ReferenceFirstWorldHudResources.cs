using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>Live resource view of one native GameHud; never a second loader or texture cache.</summary>
public sealed class ReferenceFirstWorldHudResources : IDisposable
{
    private const string HeadPortraitFormat = "res/head/DThed{0:D2}{1}_{2}.gsa";
    private readonly GameHud owner;
    private bool disposed;

    internal ReferenceFirstWorldHudResources(GameHud owner)
    {
        this.owner = owner;
    }

    // Construction readiness only. Native loaded is a one-shot attempt latch, not an
    // all-present resource condition, and borrowing never initiates Load.
    public bool Ready => !disposed && owner != null;
    internal GameHud OwnerForIndependentRuntime => owner;
    public string HeadPortraitPath => owner.HeadPortraitPathForIndependentRuntime;
    internal int HeadPortraitVersion => owner.headPortVer;

    public Texture2D StatusBarBackground => owner.statusbarbk;
    public Texture2D HpBackground => owner.hpBg;
    public Texture2D HpFill => owner.hpFill;
    public Texture2D MpBackground => owner.mpBg;
    public Texture2D MpFill => owner.mpFill;
    public Texture2D HeadPortrait => owner.headPortrait;
    public Texture2D PetBar => owner.petbar;
    public Texture2D PetHpFill => owner.petHpFill;
    public Texture2D PetMpFill => owner.petMpFill;
    public Texture2D NameEffectGray => owner.nameFxGray;
    public Texture2D NameEffectWhite => owner.nameFxWhite;
    public Texture2D SocketFill => owner.socketFill;
    public Texture2D TargetMonster => owner.tbarMonster;
    public Texture2D TargetPlayer => owner.tbarPlayer;
    public Texture2D TargetHpFill => owner.tbarHpFill;
    public Texture2D AccelBarBackground => owner.accelbarbk;
    public Texture2D AccelText => owner.acceltext;
    public Texture2D AccelToggle => owner.togglebtn;
    public Texture2D AccelPlus => owner.accelPlus;
    public Texture2D AccelMinus => owner.accelMinus;
    public Texture2D AccelHelp => owner.accelHelp;
    public Texture2D AccelEffect => owner.accelEffect;
    public Texture2D AccelShowButton => owner.accelShowBtn;
    public Texture2D BottomHotbarBackground => owner.BottomBackgroundForIndependentRuntime;
    public Texture2D BottomHotbarText => owner.hb24Text;
    public Texture2D BottomHotbarText2 => owner.hb24Text2;
    public Texture2D BottomHotbarBlock => owner.hb24Block;
    public Texture2D BottomHotbarEffect => owner.hb24Effect;
    public Texture2D[] BottomControlButtonTextures => owner.hb24Ctrl;
    public Texture2D SketchBar => owner.skBar;
    public Texture2D SketchToggle => owner.skToggle;
    public Texture2D SketchWorldButton => owner.skWorldBtn;
    public Texture2D SketchPlus => owner.skPlus;
    public Texture2D SketchMinus => owner.skMinus;
    public Texture2D SketchFrame => owner.skFrame;
    public Texture2D SketchMarkSelf => owner.skMarkSelf;
    public Texture2D SketchMarkMate => owner.skMarkMate;
    public Texture2D HpAimer => owner.hpAimer;
    public Texture2D LianliRing => owner.lianliRing;
    public Texture2D NailiRing => owner.nailiRing;
    public Texture2D ExperienceBackground => owner.exp05Bk;
    public Texture2D ControlBarBackground => owner.ctrlBarBk;
    public IReadOnlyList<Texture2D> HpFillFrames => owner.hpFill3;
    public IReadOnlyList<Texture2D> PetHpFillFrames => owner.petHpFill3;
    public IReadOnlyList<Texture2D> ExperienceFillFrames => owner.exp05Fill;
    public IReadOnlyList<Texture2D> TargetHpFillFrames => owner.tbarHpFill3;
    public IReadOnlyList<Texture2D> CooldownFrames => owner.CooldownFramesForIndependentRuntime;
    public IReadOnlyList<Texture2D> ControlButtonTextures => owner.ctrlTex;
    public IReadOnlyList<Texture2D> Rack2021Frames => owner.rack21;

    // Kept for existing editor callers. They must mount the actual native owner and drive
    // its GUI prefix; a supplied entry record cannot become a second portrait authority.
    public static bool TryCreate(
        ReferenceResourceLoader resources,
        WorldEntryRecord character,
        out ReferenceFirstWorldHudResources view)
    {
        view = null;
        if (resources == null || character == null || GameHud.I == null) return false;
        view = new ReferenceFirstWorldHudResources(GameHud.I);
        return true;
    }

    internal void SyncHeadPortraitVersion(ReferenceResourceLoader resources) =>
        owner.SyncHeadPortraitForIndependentRuntime();

    internal static string BuildHeadPortraitPath(int gender, int head, int hair)
    {
        int reviewedHead = Mathf.Clamp(head, 1, 8);
        int reviewedHair = Mathf.Clamp(hair, 1, 8);
        bool male = gender == 1;
        if (male && reviewedHead == 7)
        {
            reviewedHead = 8;
        }
        else if (male && reviewedHead == 8)
        {
            reviewedHead = 7;
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            HeadPortraitFormat,
            reviewedHead,
            reviewedHair,
            male ? "01" : "51");
    }

    public void Dispose()
    {
        if (disposed) return;
        // Preserve the existing host release boundary for the two generated textures.
        // This is not a Load failure rollback, retry or native lifecycle method.
        if (owner != null) owner.ReleaseGeneratedHudTexturesForIndependentRuntime();
        disposed = true;
    }
}
}
