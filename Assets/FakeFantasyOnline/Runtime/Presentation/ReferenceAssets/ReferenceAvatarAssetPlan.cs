using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Resolves the source-owned stand portrait cone recovered from PlayerAvatar.Build, PartsFor, and
/// LoadPart (06000847, 06000848, and 06000853).
/// </summary>
internal static class ReferenceAvatarAssetPlan
{
    private const int MaximumAppearanceModel = ushort.MaxValue;

    // PlayerAvatar.HairRGB, recovered from the eight constants initialized by 06000836. The
    // packed reference values are ABGR in Color32 memory order.
    private static readonly Color32[] HairColors =
    {
        new Color32(0xd7, 0x90, 0x3b, 0xff),
        new Color32(0x84, 0x60, 0x4b, 0xff),
        new Color32(0xee, 0x4e, 0x37, 0xff),
        new Color32(0xb5, 0xae, 0xa3, 0xff),
        new Color32(0x3b, 0x67, 0xcb, 0xff),
        new Color32(0x50, 0xa6, 0x1a, 0xff),
        new Color32(0xb2, 0x4a, 0xc1, 0xff),
        new Color32(0x55, 0x54, 0x54, 0xff)
    };

    public static bool TryCreate(
        CharacterSelectionItem character,
        out ReferenceAvatarAssets assets)
    {
        assets = default(ReferenceAvatarAssets);
        if (character == null || character.Appearance == null)
        {
            return false;
        }

        return TryCreate(
            character.Gender,
            character.Head,
            character.Hair,
            character.Appearance,
            out assets);
    }

    public static bool TryCreate(
        WorldEntryRecord character,
        out ReferenceAvatarAssets assets)
    {
        assets = default(ReferenceAvatarAssets);
        if (character == null || character.Appearance == null)
        {
            return false;
        }

        return TryCreate(
            character.Gender,
            character.Head,
            character.Hair,
            character.Appearance,
            out assets);
    }

    public static bool TryCreate(
        WorldRemotePlayerState character,
        out ReferenceAvatarAssets assets)
    {
        assets = default(ReferenceAvatarAssets);
        if (character == null || character.Appearance == null)
        {
            return false;
        }

        return TryCreate(
            character.Gender,
            character.Head,
            character.Hair,
            character.Appearance,
            out assets);
    }

    private static bool TryCreate(
        int gender,
        int requestedHead,
        int requestedHair,
        CharacterAppearanceRecord appearance,
        out ReferenceAvatarAssets assets)
    {
        assets = default(ReferenceAvatarAssets);
        if (!IsModel(appearance.Body) ||
            !IsModel(appearance.Cap) ||
            !IsModel(appearance.Backpack) ||
            !IsModel(appearance.RightWeapon) ||
            !IsModel(appearance.LeftWeapon) ||
            !IsModel(appearance.Face))
        {
            return false;
        }

        int defaultBody = gender == 1 ? 141 : 142;
        int body = appearance.Body > 0 ? appearance.Body : defaultBody;
        int head = Math.Max(1, Math.Min(8, requestedHead));
        int hair = Math.Max(1, Math.Min(HairColors.Length, requestedHair));
        string genderSuffix = gender == 1 ? "01" : "51";

        // PlayerAvatar.Build (0x0600099c) reads its first argument, the gender flag, twice:
        // `cmp r15d, 1; setne` at x86_64 0x2a7716-0x2a771a stores the is-not-1 result as the
        // avatar's female flag, and `cmp r15d, 1; setne cl; add ecx, 0x8d` at 0x2a7796-0x2a77bb
        // turns the same comparison into the default body 141 or 142, which then replaces any
        // appearance body that is <= 0 (0x2a77c1-0x2a77c9). The head path suffix comes from the
        // two metadata slots selected right after the first comparison: usage ordinal 11022
        // decodes exactly to the string literal "01" for gender == 1 and ordinal 11172 to "51"
        // otherwise. LoadPart tries female action/fallback art before the neutral/male candidates,
        // preserving the original failover order.
        bool female = gender != 1;
        List<ReferenceAvatarPart> parts = new List<ReferenceAvatarPart>(5);
        AddPart(parts, 6, "weaponr", appearance.RightWeapon, female);
        AddPart(parts, 5, "weaponl", ReferenceAvatarWeaponPresentation.LeftModel(
            appearance.RightWeapon, appearance.LeftWeapon), female);
        AddPart(parts, 4, "backpack", appearance.Backpack, female);
        AddPart(parts, 1, "cap", appearance.Cap, female);
        AddPart(parts, 2, "face", appearance.Face, female);

        assets = new ReferenceAvatarAssets(
            body,
            defaultBody,
            string.Format("avatar/head/head{0:D2}{1}.gsn", head, genderSuffix),
            HairColors[hair - 1],
            parts.ToArray());
        return true;
    }

    private static bool IsModel(int value)
    {
        return value >= 0 && value <= MaximumAppearanceModel;
    }

    private static void AddPart(
        ICollection<ReferenceAvatarPart> parts,
        int tag,
        string folder,
        int model,
        bool female)
    {
        if (model < 1)
        {
            return;
        }

        parts.Add(new ReferenceAvatarPart(tag, folder, model, female));
    }

}

internal struct ReferenceAvatarAssets
{
    public ReferenceAvatarAssets(
        int bodyModel,
        int defaultBodyModel,
        string headPath,
        Color32 hairColor,
        ReferenceAvatarPart[] parts)
    {
        BodyModel = bodyModel;
        DefaultBodyModel = defaultBodyModel;
        BodyPath = string.Format("avatar/npc/npc{0}01.gsn", bodyModel);
        HeadPath = headPath ?? throw new ArgumentNullException(nameof(headPath));
        HairColor = hairColor;
        Parts = parts ?? throw new ArgumentNullException(nameof(parts));
    }

    public string BodyPath { get; }
    public int BodyModel { get; }
    public int DefaultBodyModel { get; }
    public string HeadPath { get; }
    public Color32 HairColor { get; }
    public ReferenceAvatarPart[] Parts { get; }

    public string[] BodyCandidatePaths(int fileAction)
    {
        if (fileAction < 1 || fileAction > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(fileAction));
        }

        List<string> paths = new List<string>(2);
        AddBodyCandidate(paths, BodyModel, fileAction);
        AddBodyCandidate(paths, DefaultBodyModel, fileAction);
        return paths.ToArray();
    }

    public string BodyPathForAction(int fileAction)
    {
        if (fileAction < 1 || fileAction > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(fileAction));
        }

        return string.Format(
            "avatar/npc/npc{0}{1:D2}.gsn",
            BodyModel,
            fileAction);
    }

    private static void AddBodyCandidate(
        ICollection<string> paths,
        int model,
        int fileAction)
    {
        string path = string.Format(
            "avatar/npc/npc{0}{1:D2}.gsn",
            model,
            fileAction);
        if (!paths.Contains(path))
        {
            paths.Add(path);
        }
    }
}

internal struct ReferenceAvatarPart
{
    public ReferenceAvatarPart(
        int tag,
        string folder,
        int model,
        bool female)
    {
        Tag = tag;
        Folder = folder ?? throw new ArgumentNullException(nameof(folder));
        Model = model;
        Female = female;
        CandidatePaths = BuildCandidatePaths(folder, model, female, 1);
    }

    public int Tag { get; }
    public string Folder { get; }
    public int Model { get; }
    public bool Female { get; }
    public string[] CandidatePaths { get; }

    public string[] CandidatePathsForAction(int action)
    {
        if (action < 1 || action > 49)
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }

        return BuildCandidatePaths(Folder, Model, Female, action);
    }

    public string[] GlowCandidatePathsForAction(int action)
    {
        if (action < 1 || action > 49)
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }

        return BuildCandidatePaths(Folder, Model, Female, action, "e");
    }

    private static string[] BuildCandidatePaths(
        string folder,
        int model,
        bool female,
        int action,
        string suffix = "")
    {
        int modelBase = checked(model * 100);
        List<string> candidates = new List<string>(4);
        if (female)
        {
            AddCandidate(candidates, folder, modelBase + action + 50, suffix);
            AddCandidate(candidates, folder, modelBase + 51, suffix);
        }

        AddCandidate(candidates, folder, modelBase + action, suffix);
        AddCandidate(candidates, folder, modelBase + 1, suffix);
        return candidates.ToArray();
    }

    private static void AddCandidate(
        ICollection<string> candidates,
        string folder,
        int variant,
        string suffix)
    {
        string path = string.Format(
            "avatar/{0}/{0}{1:D4}{2}.gsn",
            folder,
            variant,
            suffix);
        if (!candidates.Contains(path))
        {
            candidates.Add(path);
        }
    }
}
}
