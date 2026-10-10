using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{
internal enum TitleCharacterAction
{
    None,
    Create,
    Enter,
    Delete,
    ReturnToServerSelection
}

// Viewport/resource binding only. Character decisions and per-slot avatars belong to TitleFlow.
internal sealed partial class RecoveredTitleCharacterSurface
{
    internal const string PropertyPanelPath = "res/uires/titleui/proppanel1.gsa";
    internal const int PropertyPanelWidth = 394;
    internal const int PropertyPanelHeight = 104;
    private readonly TitleFlow native;
    internal void BindRosterReceiptReleaseForIndependentRuntime(Action clear) =>
        NetClient.I.clearRosterReceipt = clear;

    public RecoveredTitleCharacterSurface(TitleFlow owner)
    {
        native = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    internal void PrepareTitleDrawForIndependentRuntime()
    {
        if (native != null && native.isActiveAndEnabled) native.TryLoad05Login();
    }

    public string SelectedCharacterId => native.SelectedCharacterForIndependentRuntime?.CharacterId ?? string.Empty;
    public ServerCharacterRecord? SelectedCharacterItem => native.SelectedCharacterForIndependentRuntime;
    internal bool Entering => native.entering;
    // The connected creation-page adapter borrows this same owner; it must not allocate another.
    internal TitleFlow NativeOwnerForIndependentRuntime => native;
    // Drawing follows the actual component lifetime and activation state.
    internal bool CanDrawForIndependentRuntime => native != null && native.isActiveAndEnabled;

    internal void BeginEnterForIndependentRuntime()
    {
        native.entering = true;
        native.enterAtMs = Time.realtimeSinceStartup;
    }

    internal void CompleteEnterForIndependentRuntime() => native.entering = false;

    internal void BindRosterForIndependentRuntime(IReadOnlyList<ServerCharacterRecord> characters)
    {
        if (native != null) native.BindRosterForIndependentRuntime(characters);
    }

    public void Draw(IReadOnlyList<ServerCharacterRecord> characters)
    {
        if (native == null || !native.isActiveAndEnabled) return;
        native.BindRosterForIndependentRuntime(characters);
        native.EnsureStylesForIndependentRuntime();
        Matrix4x4 previousMatrix = native.BeginGuiFrameForIndependentRuntime(true);
        native.DrawCharSelect();
        GUI.matrix = previousMatrix;
    }

    // Product-only background entry: that path supplies a row without an original title/slot
    // receiver. Keep its established null-row behavior and projection; the actual TitleFlow
    // roster receipt and click call the instance MirrorSlotToCharData(int) body instead.
    internal static void MirrorSlotToCharData(ServerCharacterRecord? selected)
    {
        if (selected == null)
        {
            CharData.name = string.Empty;
            return;
        }

        ServerCharacterRecord row = selected.Value;
        CharData.name = row.DisplayName;
        CharData.race = row.Race;
        CharData.gender = row.Gender;
        CharData.head = row.Head;
        CharData.hair = row.Hair;
        CharData.level = row.Level;
        CharData.lastLogin = row.LastLogin;
        CharData.SetAppearance(
            row.apBody,
            row.apCap,
            row.apBackpack,
            row.apWeaponR,
            row.apWeaponL,
            row.apFace);
    }

    // Existing DrawBorder (06000ef8) carrier, shared by the native page without a second policy.
    internal static void DrawBorder(Rect rect, float thickness, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
}
