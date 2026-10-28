using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Application
{
    // Native actor projection substitute; schema/state/panel/dialog behavior uses production code.
    public sealed class WorldNpcState
    {
        public int EntityId; public string DisplayName, Script;
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class GameBootstrap
    {
        public EntranceWorld Window = new EntranceWorld();
    }
    public sealed class EntranceWorld
    {
        public WorldNpcState Current;
        public bool IsNpcStateCurrentForIndependentRuntime(WorldNpcState source) => ReferenceEquals(Current, source);
    }
    public sealed class ReferenceNpcDialogOption
    {
        public readonly string Label; public readonly Action Action;
        public ReferenceNpcDialogOption(string label, Action action) { Label = label; Action = action; }
    }
    public sealed partial class DialogUI
    {
        public readonly List<ReferenceNpcDialogOption> Options = new List<ReferenceNpcDialogOption>();
        public bool Closed; public string Message;
        private void Add(ReferenceNpcDialogOption row) => Options.Add(row);
        private void Close() => Closed = true;
        private void ShowSay(string message) => Message = message;
        public void OpenForTest(WorldNpcState source)
        {
            CaptureRiftEntrance(source); CaptureCubeWorkbench(source); CaptureSpiritWorkbench(source); Options.Clear(); Closed = false; Message = null;
            Add(new ReferenceNpcDialogOption("原生任务和服务", () => {}));
            AddRiftEntranceOption(); AddCubeWorkbenchOption(); AddSpiritWorkbenchOptions();
        }
    }
}
