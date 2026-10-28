using System;
using FakeFantasyOnline.Application;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private EndgameSpiritDefinitionView spiritDefinition;
    private EndgameSpiritSelection spiritSelection, spiritMaterial, spiritUseSource, spiritSubmittedSelection;
    private string spiritMode = "unseal", spiritAwaiting = "", spiritUseRevision = "", spiritPreviewRevision = "", spiritRebindRevision = "";
    private int spiritEntity, spiritMap; private long spiritUseGeneration; private byte spiritUseKind;
    private Func<bool> spiritSourceCurrent; private bool spiritBagOpened;
    private static bool SpiritDefinitionSupported(EndgameSpiritDefinitionView d)
    {
        switch (d.UIKey.ToLowerInvariant())
        { case "shoering": case "glovesoul": case "wingsoul": case "clothsoul": case "neckring": case "facesoul": case "capsoul": case "honorsoul": case "weaponsoul": return true; default: return false; }
    }
    internal static EndgameSpiritDefinitionView SpiritNpcDefinition(int map, string name, string script)
    {
        if (!WorldEntryAvailable || !Current.State.SpiritAvailable) return null;
        foreach (var d in Current.State.Spirits) if (SpiritDefinitionSupported(d) && d.MapID == map && d.NpcName == name && d.NpcScript == script) return d;
        return null;
    }
    internal static bool TryOpenSpiritFromNpc(int map, int entity, string name, string script, string mode, Func<bool> current)
    {
        var d = SpiritNpcDefinition(map, name, script);
        if (d == null || entity == 0 || current == null || !current() || (mode != "unseal" && mode != "process" && mode != "reset")) return false;
        if (!TryOpenWorldView(EndgameModule.Spirit)) return false;
        Current.spiritDefinition = d; Current.spiritEntity = entity; Current.spiritMap = map; Current.spiritMode = mode; Current.spiritSourceCurrent = current;
        return true;
    }
    private bool SpiritSourceCurrent() => State != null && State.SpiritAvailable && spiritDefinition != null && spiritMap == GameBootstrap.CurMapId &&
        generation == State.Generation && spiritSourceCurrent != null && spiritSourceCurrent() &&
        ReferenceEquals(SpiritNpcDefinition(spiritMap, spiritDefinition.NpcName, spiritDefinition.NpcScript), spiritDefinition);
    internal static bool SpiritWorkbenchOpen => WorldEntryAvailable && Current.open && Current.module == EndgameModule.Spirit && Current.SpiritSourceCurrent();
    private bool SpiritEditable => SpiritWorkbenchOpen && State.InventorySnapshotCurrent && !State.Pending;
    private void ClearSpiritItems() { spiritSelection = spiritMaterial = null; spiritPreviewRevision = spiritRebindRevision = ""; State?.InvalidateSpiritPreview(); }
    private void ClearSpirit() { ClearSpiritItems(); spiritDefinition = null; spiritSourceCurrent = null; spiritEntity = spiritMap = 0; spiritBagOpened = false; }
    private static EndgameSpiritSelection SpiritBagSelection(int tab, int slot)
    {
        var state = Current?.State;
        if (state == null || !state.InventorySnapshotCurrent || tab < 0 || tab >= InventoryDlg.Items.Length || slot < 0 || InventoryDlg.IsLocked((byte)tab, slot)) return null;
        var native = InventoryDlg.ItemAt(tab, slot); if (native.id <= 0 || native.count <= 0) return null;
        var item = state.Find("bag", tab, slot, native.id);
        if (item != null && (item.Locked || !MatchesNative(item))) return null;
        return new EndgameSpiritSelection(tab, slot, native.id, item?.UID ?? "");
    }
    private static bool SpiritSelectionCurrent(EndgameSpiritSelection s)
    {
        if (s == null) return false;
        var now = SpiritBagSelection(s.Tab, s.Slot);
        return now != null && now.ItemID == s.ItemID && now.UID == s.UID;
    }
    internal static bool TryPutSpiritBagItem(int tab, int slot)
    {
        if (!SpiritWorkbenchOpen || !Current.SpiritEditable || SysMsgBox.Visible) return false;
        var s = SpiritBagSelection(tab, slot); if (s == null) return false;
        if (s.ItemID == Current.spiritDefinition.EssenceID) { Current.spiritMaterial = s; return true; }
        if (s.ItemID != Current.spiritDefinition.ItemID) { NewbieTipBox.PushText("此物灵息不合，请置入对应的灵与元神。"); return false; }
        Current.spiritSelection = s; Current.spiritPreviewRevision = ""; Current.State.InvalidateSpiritPreview(); Current.PreviewSpirit(); return true;
    }
    private void PreviewSpirit()
    {
        if (!SpiritEditable || !SpiritSelectionCurrent(spiritSelection) || State.SpiritLoading) return;
        if (Send(new EndgameRequest("spirit.preview", State.Revision, npcEntityID: spiritEntity, spiritItem: spiritSelection))) spiritPreviewRevision = State.Revision;
    }
    private void SubmitSpirit(string operation)
    {
        if (State.Pending) { if (State.PendingRequest.Op == "spirit." + operation) client.RetryEndgame(); return; }
        if (!SpiritEditable || !State.SpiritCurrent || !SpiritSelectionCurrent(spiritSelection) || !SpiritSelectionCurrent(spiritMaterial)) return;
        var v = State.SpiritView;
        bool allowed = operation == "unseal" ? v.CanUnseal : operation == "process" ? v.CanProcess : operation == "rollback" ? v.CanRollback : v.CanReset;
        if (!allowed) return;
        var request = new EndgameRequest("spirit." + operation, State.Revision, npcEntityID: spiritEntity, spiritItem: spiritSelection, spiritMaterial: spiritMaterial);
        if (Send(request)) { spiritAwaiting = request.RequestID; spiritSubmittedSelection = spiritSelection; }
    }
    private void ObserveSpiritRequest()
    {
        if (spiritAwaiting.Length > 0)
        {
            var response = State.ResponseFor(spiritAwaiting);
            if (response != null && State.PendingRequest?.RequestID != spiritAwaiting)
            { spiritAwaiting = ""; if (response.OK && open && module == EndgameModule.Spirit && ReferenceEquals(spiritSubmittedSelection, spiritSelection)) spiritRebindRevision = response.Revision;
                else if (!response.OK)
                {
                    if (response.Message.Length > 0) NewbieTipBox.PushText(response.Message);
                    if (response.Error == "stale_revision") { State.InvalidateSpiritPreview(); spiritPreviewRevision = ""; Send(new EndgameRequest("snapshot")); }
                } }
        }
        if (!open || module != EndgameModule.Spirit) return;
        if (!SpiritSourceCurrent()) { Close(); return; }
        if (!State.InventorySnapshotCurrent || State.Pending) return;
        // An unsealed legacy item receives its first UID in this exact successful transaction.
        // Only the matching receipt plus committed snapshot may rebind that bag identity.
        if (spiritRebindRevision.Length > 0 && State.Revision == spiritRebindRevision)
        {
            if (spiritSelection != null) { var now = SpiritBagSelection(spiritSelection.Tab, spiritSelection.Slot); if (now != null && now.ItemID == spiritSelection.ItemID) spiritSelection = now; }
            spiritRebindRevision = "";
        }
        if (spiritSelection != null && !SpiritSelectionCurrent(spiritSelection)) ClearSpiritItems();
        if (spiritMaterial != null && !SpiritSelectionCurrent(spiritMaterial)) spiritMaterial = null;
        if (spiritSelection != null && !State.SpiritCurrent && !State.SpiritLoading && spiritPreviewRevision != State.Revision) PreviewSpirit();
    }
    private void DrawSpiritSlot(Rect rect, EndgameSpiritSelection selection, bool material)
    {
        if (selection != null)
        {
            var icon = NetClient.LoadItemIcon(selection.ItemID); if (icon != null) GUI.DrawTexture(rect, icon);
            var native = InventoryDlg.ItemAt(selection.Tab, selection.Slot);
            if (material) CubeText(new Rect(rect.x, rect.yMax - 12, rect.width, 12), native.count.ToString(), Gold);
            if (rect.Contains(Event.current.mousePosition)) ShowPanelTooltip(BagTooltip(selection.Tab, native, TooltipContent.Fmt(native.name, native.desc)));
        }
        if (!SpiritEditable || !rect.Contains(Event.current.mousePosition)) return;
        if (InventoryDlg.HasHeld && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            var held = SpiritBagSelection(InventoryDlg.HeldTab, InventoryDlg.HeldBagSlot);
            if (held != null && held.ItemID == (material ? spiritDefinition.EssenceID : spiritDefinition.ItemID) && TryPutSpiritBagItem(held.Tab, held.Slot)) InventoryDlg.ClearHeld();
            Event.current.Use(); PlayerController.SuppressWorldClick();
        }
        else if (!material && selection != null && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        { State.InvalidateSpiritPreview(); spiritPreviewRevision = ""; PreviewSpirit(); Event.current.Use(); }
        else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
        { if (material) spiritMaterial = null; else { spiritSelection = null; State.InvalidateSpiritPreview(); } Event.current.Use(); }
    }
    internal static bool HandleSpiritItemForIndependentRuntime(int id, int tab, int slot)
    {
        if (!WorldEntryAvailable || !Current.State.SpiritAvailable) return false;
        byte kind = 0;
        foreach (var d in Current.State.Spirits) { if (!SpiritDefinitionSupported(d)) continue; if (d.ItemID == id) kind = 4; if (d.SeparatorID == id) kind = 5; }
        foreach (int appearance in Current.State.SpiritAppearanceIDs) if (appearance == id) kind = 6;
        if (kind == 0) return false;
        if (Current.State.Pending || !Current.State.InventorySnapshotCurrent)
        { NewbieTipBox.PushText("器物尚未同步，请稍候再试。"); Current.Send(new EndgameRequest("snapshot")); return true; }
        var source = SpiritBagSelection(tab, slot); if (source == null || source.ItemID != id) return true;
        Current.spiritUseSource = source; Current.spiritUseKind = kind; Current.spiritUseGeneration = Current.State.Generation; Current.spiritUseRevision = Current.State.Revision;
        AvatarFuse.Begin((byte)tab, slot, kind); return true;
    }
    internal static bool ApplySpiritItem(int tab, int slot, string location, int targetTab, int targetSlot, byte kind)
    {
        if (!WorldEntryAvailable || !Current.State.SpiritAvailable || Current.State.Pending || !Current.State.InventorySnapshotCurrent) return false;
        var state = Current.State; var source = Current.spiritUseSource; Current.spiritUseSource = null;
        if (kind < 4 || kind > 6 || kind != Current.spiritUseKind || source == null || source.Tab != tab || source.Slot != slot || Current.spiritUseGeneration != state.Generation || Current.spiritUseRevision != state.Revision || !SpiritSelectionCurrent(source)) return false;
        EndgameItemView target = null;
        foreach (var item in state.Items) if (item.Location == location && item.Slot == targetSlot && (location == "equipped" || item.Tab == targetTab)) { target = item; break; }
        if (target == null || target.Locked || !MatchesNative(target) || location == "bag" && targetTab == tab && targetSlot == slot) return false;
        var request = new EndgameRequest(kind == 4 ? "spirit.fuse" : kind == 5 ? "spirit.separate" : "spirit.appearance", state.Revision, targetUID: target.UID, spiritItem: source);
        if (!Current.Send(request)) return false; Current.spiritAwaiting = request.RequestID; return true;
    }
}
}
