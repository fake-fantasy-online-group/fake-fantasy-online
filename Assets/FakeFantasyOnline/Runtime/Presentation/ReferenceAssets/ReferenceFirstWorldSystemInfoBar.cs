using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// One user decision emitted by the source-recovered system-request confirmation surface.
/// </summary>
public sealed class ReferenceSystemRequestResponseIntent
{
    public ReferenceSystemRequestResponseIntent(int requestId, bool accept)
    {
        RequestId = requestId;
        Accept = accept;
    }

    public int RequestId { get; }

    public bool Accept { get; }
}

internal sealed class ReferenceSystemRequestDisplay
{
    public ReferenceSystemRequestDisplay(WorldSystemRequestState state, float animation)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        Animation = Mathf.Clamp01(animation);
    }

    public WorldSystemRequestState State { get; }

    public float Animation { get; set; }
}

/// <summary>
/// Existing-window API bridge to the original SysMsgBox component. Native OnGUI owns drawing
/// and button events; creation and texture loading remain lazy in the original owner.
/// </summary>
public sealed class ReferenceSystemMessageBox : IDisposable
{
    public const string BackgroundPath = "res/uires/common/msgboxbk.gsa";
    public const string AffirmativeButtonPath = "res/uires/common/affirmbtn.gsa";
    public const string CancelButtonPath = "res/uires/common/cancelbtn.gsa";
    public const float FallbackWidth = 346f;
    public const float FallbackHeight = 128f;
    public const float ButtonWidth = 70f;
    public const float ButtonHeight = 22f;
    public const float HorizontalButtonInset = 36f;
    public const float BottomButtonInset = 16f;
    public const float MessageHorizontalInset = 34f;
    public const float MessageTopInset = 20f;
    public const float MessageLineAdvance = 16f;

    private bool disposed;
    private Action<string> openGossip;
    private Action<SysMsgBox> registerCreatedOwner;
    private ReferenceSystemMessageBox() { }
    public bool Ready => !disposed;
    public bool Visible => SysMsgBox.Visible;
    public string Message => SysMsgBox.I != null ? SysMsgBox.I.MessageForIndependentRuntime : null;

    internal static bool TryCreate(ReferenceResourceLoader resources, ReferenceWorldBitmapFont font,
        out ReferenceSystemMessageBox owner)
    {
        owner = new ReferenceSystemMessageBox();
        return true;
    }

    public void Show(string message, Action affirmative, Action cancel)
    {
        ThrowIfDisposed();
        bool createsOwner = SysMsgBox.I == null;
        SysMsgBox.Show(message, affirmative, cancel);
        if (createsOwner) registerCreatedOwner?.Invoke(SysMsgBox.I);
    }

    internal void BindSceneOwnerForIndependentRuntime(Action<SysMsgBox> register) =>
        registerCreatedOwner = register;

    public void Close() => SysMsgBox.Close();
    public bool MouseOver(Vector2 topOriginPointer, int viewportWidth, int viewportHeight, float scale) =>
        SysMsgBox.MouseOver();

    public bool Draw(float scale)
    {
        ThrowIfDisposed();
        return true;
    }

    public void FireAffirmativeForAudit()
    {
        if (SysMsgBox.Visible) SysMsgBox.I.FireAffirmativeForIndependentRuntime();
    }

    public void FireCancelForAudit()
    {
        if (SysMsgBox.Visible) SysMsgBox.I.FireCancelForIndependentRuntime();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        registerCreatedOwner = null;
    }

    private void ThrowIfDisposed()
    {
        if (disposed) throw new ObjectDisposedException(nameof(ReferenceSystemMessageBox));
    }
}

/// <summary>Host projection over the native 1.5.8 SystemInfoBar owner.</summary>
public sealed class ReferenceFirstWorldSystemInfoBar : IDisposable
{
    public const string IconPath = "res/uires/gameui/systeminfobtn.gsa";
    public const string HandleSuffix = "\n点击处理";
    public const string WhisperSuffix = "\n点击打开私聊";
    public const string ConfirmationSuffix = "  是否接受？";
    public const int MaximumVisibleRequests = 6;
    public const int NewRequestSoundId = 1009;
    public const int ClickSoundId = 1002;
    public const float AnimationSeconds = 0.3f;
    public const float InitialLeft = 447f, FinalLeft = 184f, IconTop = 70f, IconPitch = 26f, IconSize = 24f;
    private static readonly int[] FrameScheme = { 0, 0, 1, 1, 2, 3, 3, 3 };
    private readonly ReferenceSystemMessageBox messageBox;
    private bool disposed;
    private ReferenceFirstWorldSystemInfoBar(ReferenceSystemMessageBox box)
    {
        messageBox = box;
    }
    public bool Ready => !disposed;
    public int Count => SystemInfoBar.I == null ? 0 : SystemInfoBar.I.RequestsForIndependentRuntime.Count;
    public bool MessageBoxVisible => SysMsgBox.Visible;
    public string ConfirmationMessage => messageBox.Message;
    internal ReferenceSystemMessageBox SharedMessageBox => messageBox;
    public int LastSoundRequestId => NewRequestSoundCount == 0 ? 0 : NewRequestSoundId;
    public int NewRequestSoundCount => SystemInfoBar.I == null ? 0 :
        SystemInfoBar.I.ListSoundCountForIndependentRuntime;
    public static IReadOnlyList<int> OfficialFrameScheme => FrameScheme;
    public static void AddGossipNotice(string from) => SystemInfoBar.AddGossipNotice(from);
    public static void ClearGossipNotice(string from) => SystemInfoBar.ClearGossipNotice(from);
    public static bool TryCreate(ReferenceResourceLoader resources, out ReferenceFirstWorldSystemInfoBar owner)
    {
        owner = null;
        if (resources == null) return false;
        ReferenceSystemMessageBox.TryCreate(resources, null, out ReferenceSystemMessageBox box);
        owner = new ReferenceFirstWorldSystemInfoBar(box);
        return true;
    }
    public bool SetList(IReadOnlyList<WorldSystemRequestState> next)
    {
        ThrowIfDisposed();
        if (next == null) return false;
        ReferenceWorldReceiveOwner.ReceiveSystemRequestsForIndependentRuntime(next);
        return true;
    }
    public bool Update(float deltaTime)
    {
        ThrowIfDisposed();
        return deltaTime >= 0f && !float.IsInfinity(deltaTime);
    }
    public Rect IconRect(int index, float scale) => index < 0 || index >= Count ? new Rect() :
        SystemInfoBar.IconRect(SystemInfoBar.I.RequestsForIndependentRuntime[index], index, scale);
    public float AnimationAt(int index) => index < 0 || index >= Count ? -1f : SystemInfoBar.I.RequestsForIndependentRuntime[index].anim;
    public int RequestIdAt(int index) => index < 0 || index >= Count ? 0 : SystemInfoBar.I.RequestsForIndependentRuntime[index].reqId;
    // Retained diagnostic API forwards to the actual native confirmation owner.
    public bool OpenConfirmation(int index)
    {
        ThrowIfDisposed();
        if (index < 0 || index >= Count) return false;
        SystemInfoBar.I.OpenConfirmForIndependentRuntime(SystemInfoBar.I.RequestsForIndependentRuntime[index]);
        return true;
    }
    public ReferenceSystemRequestResponseIntent ConsumeResponseIntent() => null;
    public WorldSystemRequestState ConsumeLocalGossipIntent() => null;
    public void FireAffirmativeForAudit() => messageBox.FireAffirmativeForAudit();
    public void FireCancelForAudit() => messageBox.FireCancelForAudit();
    public bool Draw(float elapsedSeconds, float iconScale, float messageScale,
        ReferenceFirstWorldUiTip uiTip, Action hidePlayerActionMenu)
    {
        ThrowIfDisposed();
        // Observation-only compatibility API. Unity invokes the native OnGUI directly.
        return true;
    }
    public bool MouseOver(Vector2 pointer, int width, int height, float iconScale, float messageScale) => SystemInfoBar.MouseOver();
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        messageBox.Dispose();
    }
    private void ThrowIfDisposed()
    {
        if (disposed) throw new ObjectDisposedException(nameof(ReferenceFirstWorldSystemInfoBar));
    }
}
}
