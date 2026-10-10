using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// State projection and host lifetime adapter for one server-authored static NPC.
/// </summary>
/// <remarks>
/// GameBootstrap.SpawnNpcEntry owns art, geometry and native component order. This adapter
/// observes an original-owner-created root and owns only optional host views.
/// Missing optional views do not reject a constructed NPC.
/// </remarks>
internal sealed class ReferenceWorldNpcActor : IDisposable
{
    private readonly ReferenceGsmMap map;
    private readonly Transform parent;
    private readonly WorldNpcState state;
    private readonly ReferenceWorldRemoteFeedbackAssets feedbackAssets;
    private readonly Camera worldCamera;
    private readonly Transform mainPlayer;
    private readonly ReferenceWorldNpcTaskFeedback taskFeedback;
    private readonly int playerLevel;
    private readonly ReferenceNpcGreetingCatalog greetingCatalog;
    private readonly GameBootstrap spawnOwner;

    private GameObject rootObject;
    private SpriteRenderer renderer;
    private BoxCollider2D pickBox;
    private NpcInteract selectableOwner;
    private BlobShadow shadow;
    private ReferenceWorldNpcNameView nameView;
    private FrameAnimator animator;
    private bool built;
    private bool disposed;

    public ReferenceWorldNpcActor(
        ReferenceGsmMap map,
        ReferenceResourceLoader resources,
        Transform parent,
        WorldNpcState state,
        ReferenceWorldRemoteFeedbackAssets feedbackAssets,
        Camera worldCamera,
        Transform mainPlayer,
        ReferenceWorldNpcTaskFeedback npcTaskFeedback,
        int sourcePlayerLevel,
        ReferenceNpcGreetingCatalog npcGreetingCatalog,
        GameBootstrap nativeSpawnOwner,
        GameObject nativeRoot = null)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        if (resources == null) throw new ArgumentNullException(nameof(resources));
        this.parent = parent == null
            ? throw new ArgumentNullException(nameof(parent))
            : parent;
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        this.feedbackAssets = feedbackAssets;
        this.worldCamera = worldCamera;
        this.mainPlayer = mainPlayer;
        this.taskFeedback = npcTaskFeedback;
        playerLevel = sourcePlayerLevel;
        greetingCatalog = npcGreetingCatalog;
        spawnOwner = nativeSpawnOwner;
        rootObject = nativeRoot;
    }

    public bool Built => built;

    public WorldNpcState State => state;

    public GameObject RootObject => rootObject;

    public SpriteRenderer Renderer => renderer;

    public BoxCollider2D PickBox => pickBox;
    internal NpcInteract SelectableOwner => selectableOwner;

    public BlobShadow Shadow => shadow;

    internal ReferenceWorldNpcNameView NameView => nameView;

    internal FrameAnimator Animator => animator;

    public bool Build()
    {
        ThrowIfDisposed();
        if (built) return false;
        // This view never creates the native NPC. Historical direct-audit callers must now
        // supply or first publish a root through GameBootstrap's actual scene owner.
        if (rootObject == null)
            GameBootstrap.NetNpcObjectsForIndependentRuntime.TryGetValue(state.EntityId, out rootObject);
        if (rootObject == null) return false;

        renderer = rootObject.GetComponent<SpriteRenderer>();
        pickBox = rootObject.GetComponent<BoxCollider2D>();
        selectableOwner = rootObject.GetComponent<NpcInteract>();
        shadow = rootObject.GetComponentInChildren<BlobShadow>();
        animator = rootObject.GetComponent<FrameAnimator>();

        if (feedbackAssets != null && feedbackAssets.Font != null &&
            feedbackAssets.Font.Ready && worldCamera != null && mainPlayer != null &&
            taskFeedback != null && playerLevel >= 0 &&
            !string.IsNullOrEmpty(state.DisplayName))
        {
            nameView = rootObject.AddComponent<ReferenceWorldNpcNameView>();
            NpcChatter chatter = rootObject.GetComponent<NpcChatter>();
            if (nameView == null || !nameView.Configure(
                    feedbackAssets, worldCamera, mainPlayer,
                    chatter != null ? chatter.headAnchor : Vector3.zero,
                    state.DisplayName, taskFeedback, playerLevel,
                    greetingCatalog, greetingCatalog == null ? null : state))
            {
                if (nameView != null) DestroyOwned(nameView);
                nameView = null;
            }
            else
            {
                nameView.ApplyTaskLog(WorldTaskLogState.Empty);
            }
        }

        built = true;
        return true;
    }

    public bool MatchesSpawnRoot(WorldNpcState value)
    {
        return value != null &&
               state.EntityId == value.EntityId &&
               state.X == value.X && state.Y == value.Y &&
               string.Equals(state.Sprite, value.Sprite, StringComparison.Ordinal) &&
               string.Equals(state.DisplayName, value.DisplayName, StringComparison.Ordinal) &&
               string.Equals(state.Label, value.Label, StringComparison.Ordinal) &&
               string.Equals(state.Portrait, value.Portrait, StringComparison.Ordinal) &&
               string.Equals(state.Script, value.Script, StringComparison.Ordinal) &&
               state.SellList == value.SellList &&
               state.TransportList == value.TransportList &&
               state.Direction == value.Direction;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ReleaseOwnedState();
    }

    private void ReleaseOwnedState()
    {
        built = false;
        shadow = null;
        if (nameView != null) DestroyOwned(nameView);
        nameView = null;
        animator = null;
        pickBox = null;
        renderer = null;
        selectableOwner = null;
        rootObject = null; // Borrowed: the native registry/scene owner alone destroys this root.
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (value == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(value);
            return;
        }
#endif
        UnityEngine.Object.Destroy(value);
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldNpcActor));
        }
    }
}

}
