using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class GroundItemEntity
{
    // Resource names are reviewed against original artwork, never synthesized geometry.
    internal const int UniqueDropSound = 1002;
    private static readonly WorldUniqueDropPresentation uniqueReceipts = new WorldUniqueDropPresentation();
    private int uniqueId;
    private long uniqueDropToken;
    private int uniqueGeneration;
    private float uniqueRetryAt;
    private EffectPlayer uniqueBeam;
    private bool uniqueSubscribed;

    internal void ConfigureUniqueDrop(int identity, bool fresh, long token)
    {
        if (uniqueId != identity || uniqueDropToken != token) ReleaseUniqueDrop();
        uniqueId = identity;
        uniqueDropToken = token;
        if (uniqueId <= 0) return;
        if (!uniqueSubscribed)
        {
            ArtDetail.OnBudgetScaleChanged += InvalidateUniqueBeam;
            uniqueSubscribed = true;
        }
        // Consume at the authoritative receive edge, before any asynchronous audio load.
        if (uniqueReceipts.TryConsume(identity, fresh, token))
        {
            int generation = uniqueGeneration;
            float received = Time.realtimeSinceStartup;
            SoundManager.PlayGuarded(UniqueDropSound, () => this != null && isActiveAndEnabled &&
                uniqueGeneration == generation && uniqueDropToken == token &&
                Time.realtimeSinceStartup - received <= 2f);
        }
        UpdateUniqueDrop();
    }

    private void UpdateUniqueDrop()
    {
        if (uniqueId <= 0 || !isActiveAndEnabled || uniqueBeam != null ||
            Time.realtimeSinceStartup < uniqueRetryAt) return;
        // Retry missing assets without recreating a sound edge, including a delayed loader.
        uniqueRetryAt = Time.realtimeSinceStartup + 0.5f;
        uniqueBeam = EffectPlayer.PlayAttachedGround(transform,
            GetComponent<SpriteRenderer>(), CombatNumber.Ppu > 0f ? CombatNumber.Ppu : 100f);
    }

    private void InvalidateUniqueBeam()
    {
        DropUniqueBeam();
        uniqueRetryAt = 0f;
    }

    private void DropUniqueBeam()
    {
        if (uniqueBeam == null) return;
        uniqueBeam.gameObject.SetActive(false);
        Destroy(uniqueBeam.gameObject);
        uniqueBeam = null;
    }

    private void ReleaseUniqueDrop()
    {
        uniqueGeneration++;
        if (uniqueSubscribed)
        {
            ArtDetail.OnBudgetScaleChanged -= InvalidateUniqueBeam;
            uniqueSubscribed = false;
        }
        DropUniqueBeam();
        uniqueRetryAt = 0f;
    }

    private void OnEnable()
    {
        if (uniqueId <= 0) return;
        if (!uniqueSubscribed)
        {
            ArtDetail.OnBudgetScaleChanged += InvalidateUniqueBeam;
            uniqueSubscribed = true;
        }
        uniqueRetryAt = 0f;
    }

    internal void CancelUniqueDrop()
    {
        uniqueId = 0;
        ReleaseUniqueDrop();
    }

    private void OnDestroy() => ReleaseUniqueDrop();
}
}
