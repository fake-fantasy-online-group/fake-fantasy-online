using System;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{
// Resource/viewport adapter only. All server tree, selection, cadence, controls and messages
// execute on the same TitleFlow instance already owned by character selection and creation.
internal sealed class RecoveredTitleServerSurface : IDisposable
{
    private bool disposed;

    internal void Draw(TitleFlow native, ClientSessionCoordinator session, Func<string> message,
        Action returnToLogin, Action<Result> observeConfirmation)
    {
        if (disposed) return;
        native.BindServerPageForIndependentRuntime(session, message, returnToLogin, observeConfirmation);
        native.EnsureStylesForIndependentRuntime();
        Matrix4x4 previousMatrix = native.BeginGuiFrameForIndependentRuntime(false);
        native.DrawServer();
        GUI.matrix = previousMatrix;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
    }
}
}
