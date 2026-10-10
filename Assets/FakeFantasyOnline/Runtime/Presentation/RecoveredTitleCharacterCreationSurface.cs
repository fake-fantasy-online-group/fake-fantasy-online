using System;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{

// Viewport and existing page capabilities only. Resources and choices are read from
// the same TitleFlow that selection owns; no constructor preloads a private image table.
internal sealed class RecoveredTitleCharacterCreationSurface : IDisposable
{

    private TitleFlow native;
    private bool _disposed;

    internal void BindNativeForIndependentRuntime(TitleFlow owner,
        Action<CharacterCreationRequest> submit, Action returnToSelection, Func<string> getMessage)
    {
        if (ReferenceEquals(native, owner)) return;
        native = owner ?? throw new ArgumentNullException(nameof(owner));
        if (submit == null) throw new ArgumentNullException(nameof(submit));
        native.BindCreationForIndependentRuntime(
            (name, race, gender, head, hair) =>
                submit(new CharacterCreationRequest(name, race, gender, head, hair)),
            returnToSelection, getMessage);
    }

    public void Reset(ServerCharacterRecord? unused)
    {
        // Before the first lazy bind, the newly constructed owner's charName is already empty.
        // On subsequent entries ef7 clears only that field; it does not reset CharData choices.
        native?.ClearCreationNameForIndependentRuntime();
    }

    public void Draw()
    {
        if (_disposed) return;
        native.EnsureStylesForIndependentRuntime();
        native.EnsureCreationStylesForIndependentRuntime();
        Matrix4x4 previousMatrix = native.BeginGuiFrameForIndependentRuntime(true);
        native.DrawCharCreate();
        GUI.matrix = previousMatrix;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }


}
}
