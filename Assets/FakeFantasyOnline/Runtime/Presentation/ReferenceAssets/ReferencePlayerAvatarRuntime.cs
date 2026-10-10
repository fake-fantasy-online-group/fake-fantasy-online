using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Type/ownership boundary for the existing renderer. All resource and pose decisions live in PlayerAvatar.
internal sealed class ReferencePlayerAvatarRuntime : IDisposable
{
    private readonly Dictionary<PlayerAvatar.Pose, ReferenceWorldPlayerStandPose> poses =
        new Dictionary<PlayerAvatar.Pose, ReferenceWorldPlayerStandPose>();
    private readonly Dictionary<PlayerAvatar.Pose[], ReferenceWorldPlayerStandPose[]> arrays =
        new Dictionary<PlayerAvatar.Pose[], ReferenceWorldPlayerStandPose[]>();
    private readonly Dictionary<PlayerAvatar.Pose[,], ReferenceWorldPlayerStandPose[,]> matrices =
        new Dictionary<PlayerAvatar.Pose[,], ReferenceWorldPlayerStandPose[,]>();
    public PlayerAvatar Source { get; }
    public CharacterAppearanceRecord Appearance { get; }

    public ReferencePlayerAvatarRuntime(WorldEntryRecord character, float ppu)
    {
        CharacterAppearanceRecord appearance = character.Appearance;
        PlayerAvatar.Appear combined = CharData.CombinedAppear();
        Appearance = new CharacterAppearanceRecord(combined.Body, combined.Cap, combined.Backpack,
            combined.WeaponR, combined.WeaponL, combined.Face, appearance.AttackVariant,
            appearance.WeaponCategory, appearance.AttackDistance);
        Source = PlayerAvatar.Build(character.Gender > 0 ? 1 : 0, character.Head, character.Hair, combined, ppu);
    }

    public ReferencePlayerAvatarRuntime(WorldRemotePlayerState character, float ppu)
    {
        CharacterAppearanceRecord appearance = character.Appearance;
        Appearance = appearance;
        Source = PlayerAvatar.Build(character.Gender, character.Head, character.Hair,
            new PlayerAvatar.Appear
            {
                Body = appearance.Body, Cap = appearance.Cap, Backpack = appearance.Backpack,
                WeaponR = appearance.RightWeapon, WeaponL = appearance.LeftWeapon, Face = appearance.Face
            }, ppu);
    }

    internal ReferencePlayerAvatarRuntime(PlayerAvatar source, CharacterAppearanceRecord appearance)
    {
        Source = source;
        Appearance = appearance;
    }

    public ReferenceWorldPlayerStandPose Wrap(PlayerAvatar.Pose source)
    {
        if (source == null) return null;
        ReferenceWorldPlayerStandPose result;
        if (!poses.TryGetValue(source, out result))
        {
            result = new ReferenceWorldPlayerStandPose(source, Source.bodyBoundW, Source.nameTopPx);
            poses.Add(source, result);
        }
        return result;
    }

    public ReferenceWorldPlayerStandPose[] Wrap(PlayerAvatar.Pose[] source)
    {
        if (source == null) return null;
        ReferenceWorldPlayerStandPose[] result;
        if (arrays.TryGetValue(source, out result)) return result;
        result = new ReferenceWorldPlayerStandPose[source.Length];
        for (int i = 0; i < source.Length; i++) result[i] = Wrap(source[i]);
        arrays.Add(source, result);
        return result;
    }

    public ReferenceWorldPlayerStandPose[,] Wrap(PlayerAvatar.Pose[,] source)
    {
        if (source == null) return null;
        ReferenceWorldPlayerStandPose[,] result;
        if (matrices.TryGetValue(source, out result)) return result;
        result = new ReferenceWorldPlayerStandPose[source.GetLength(0), source.GetLength(1)];
        for (int d = 0; d < source.GetLength(0); d++)
            for (int f = 0; f < source.GetLength(1); f++) result[d, f] = Wrap(source[d, f]);
        matrices.Add(source, result);
        return result;
    }

    // The returned view wraps this runtime's frames and never owns Source; the avatar's own lifetime
    // owner (RemotePlayer) is its only disposer.
    public ReferenceWorldPlayerNormalPoses Normal() =>
        new ReferenceWorldPlayerNormalPoses(this);
    public ReferenceWorldPlayerSittingPoses Sitting() => new ReferenceWorldPlayerSittingPoses(Wrap(Source.sit), Source.sitTopPx);

    public ReferenceWorldPlayerDeathPoses Death()
    {
        int fpd;
        PlayerAvatar.Pose[,] frames = Source.PoseFrames(3, out fpd);
        return frames == null ? null : new ReferenceWorldPlayerDeathPoses(Wrap(frames), fpd);
    }

    public ReferenceWorldPlayerHurtPoses Hurt()
    {
        int fpd;
        PlayerAvatar.Pose[,] frames = Source.PoseFrames(11, out fpd);
        return frames == null ? null : new ReferenceWorldPlayerHurtPoses(Wrap(frames), fpd);
    }

    public ReferenceWorldPlayerAttackPoses Attack(int variant)
    {
        int fpd;
        PlayerAvatar.Pose[,] frames = Source.AttackPose(variant, out fpd);
        return frames == null ? null : new ReferenceWorldPlayerAttackPoses(Wrap(frames), fpd, variant);
    }

    public ReferenceWorldPlayerPreparePoses Prepare(int actionIndex)
    {
        PlayerAvatar.Pose[] frames = Source.PoseAt(actionIndex);
        return frames == null ? null :
            new ReferenceWorldPlayerPreparePoses(Wrap(frames), actionIndex);
    }

    public ReferenceWorldPlayerMagicPoses Magic()
    {
        int fpd;
        PlayerAvatar.Pose[,] frames = Source.MagicPose(out fpd);
        return frames == null ? null : new ReferenceWorldPlayerMagicPoses(Wrap(frames), fpd);
    }

    public void Dispose()
    {
        Source.Dispose();
        ForgetObservationsForIndependentRuntime();
    }

    internal void ForgetObservationsForIndependentRuntime()
    {
        poses.Clear();
        arrays.Clear();
        matrices.Clear();
    }
}

}
