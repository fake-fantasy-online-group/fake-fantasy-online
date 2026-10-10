using System.Collections.Generic;
using FakeFantasyOnline.Application;
using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // The observation reads the existing value rows and aliases their native skill lists.
    internal static IReadOnlyList<WorldChatSharedEntryState> ProjectIncomingShares(List<Share> source) => source;
}
}
