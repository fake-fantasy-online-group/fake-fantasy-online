using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Shared server binding for independent NPC workbenches and the active-rift HUD.</summary>
internal enum EndgameModule { Cube, Rift }
public sealed partial class EndgamePanel : MonoBehaviour
{
    internal static EndgamePanel Current { get; private set; }
    private IEndgameClient client;
    private EndgameClientState State => client?.Endgame;
    internal static EndgameClientState GrowthState => WorldEntryAvailable && Current.State.GrowthV5 ? Current.State : null;
    internal static bool RetryGrowthRequest() => GrowthState?.PendingRequest?.Op == "skill.upgrade" && Current.client.RetryEndgame();
    internal static bool RefreshGrowthSkills() => GrowthState != null && Current.client.SendEndgame(new EndgameRequest("skill.ranks"));
    private bool open, confirmLeave;
    private EndgameModule module;
    private int tier = 1;
    private string selectedUID = "", selectedRuneUID = "";
    private readonly List<string> orderedRunes = new List<string>();
    private Vector2 itemScroll, detailScroll, runeScroll;
    private Vector2 panelScreenMouse;
    private float panelScale = 1f;
    private GUIStyle label, heading, small;
    private EndgameRequest quotedInputs;
    private long generation = -1;
    private float nextRiftRefresh;
    private float nextSkillBehaviorRefresh;
    private string skillBehaviorRefreshRevision;
    private int skillBehaviorRefreshAttempts;
    private const float W = 940f, H = 660f;
    private static readonly Color Gold = new Color(1f, .83f, .39f);
    private static readonly Color Jade = new Color(.49f, .88f, .79f);
    private static readonly Color Muted = new Color(.72f, .73f, .73f);

    public void Bind(IEndgameClient source)
    {
        if (!ReferenceEquals(client, source)) { ClearRiftScenePortal(); client = source; ClearSelection(); open = false; generation = -1; observedGeneration = -1; }
        Current = this;
    }
    public void Unbind(IEndgameClient source)
    {
        if (!ReferenceEquals(client, source)) return;
        ClearRiftScenePortal(); ReleaseRiftProgressArtwork(); WinZ.Forget(this); client = null; open = false; observedGeneration = -1; ClearSelection();
    }
    private void OnDestroy() { ClearRiftScenePortal(); ReleaseRiftProgressArtwork(); WinZ.Forget(this); if (Current == this) Current = null; }
    private void ClearSelection()
    {
        ClearRiftEntrance();
        ResetRiftTierDropdown();
        ClearCubeInputs();
        selectedUID = selectedRuneUID = ""; orderedRunes.Clear(); quotedInputs = null; confirmLeave = false;
        itemScroll = detailScroll = runeScroll = Vector2.zero;
    }
    private void Close() { WinZ.Forget(this); open = false; ClearSelection(); State?.InvalidateQuote(); }
    private void Update()
    {
        SyncRiftScenePortal();
        if (State == null || !client.EndgameInWorld)
        { if (open || selectedUID.Length > 0) { WinZ.Forget(this); ClearSelection(); } open = false; observedGeneration = -1; return; }
        if (generation != State.Generation)
        {
            generation = State.Generation; ClearSelection(); open = false;
            skillBehaviorRefreshRevision = null; skillBehaviorRefreshAttempts = 0; nextSkillBehaviorRefresh = 0;
        }
        RiftEntranceCurrent();
        if (State.SkillBehaviorV1)
        {
            if (skillBehaviorRefreshRevision != State.Revision)
            { skillBehaviorRefreshRevision = State.Revision; skillBehaviorRefreshAttempts = 0; nextSkillBehaviorRefresh = 0; }
            if (State.HasSkillRanks && State.SkillRanksRevision == State.Revision) skillBehaviorRefreshAttempts = 0;
            else if (!State.Pending && skillBehaviorRefreshAttempts < 3 && Time.unscaledTime >= nextSkillBehaviorRefresh)
            {
                skillBehaviorRefreshAttempts++;
                nextSkillBehaviorRefresh = Time.unscaledTime + 5f;
                RefreshGrowthSkills();
            }
        }
        ObserveRiftPhase();
        if (open && module == EndgameModule.Cube && !CubeSourceCurrent()) Close();
        if (open && Input.GetKeyDown(KeyCode.Escape))
        { if (riftTierOpen) ResetRiftTierDropdown(); else Close(); }
        if (State.Negotiated && State.Rift != null && PollRun(State.Rift) && Time.unscaledTime >= nextRiftRefresh)
        { nextRiftRefresh = Time.unscaledTime + 3f; QueryRift(); }
    }
    private bool Send(EndgameRequest request) => client != null && client.SendEndgame(request);
    private void QueryRift() => Send(new EndgameRequest("rift.state", runID: State.Rift == null ? "" : State.Rift.RunID));
    private void Refresh()
    {
        Send(new EndgameRequest("snapshot"));
        if (module == EndgameModule.Rift) QueryRift();
        if (open) EnsureModuleData();
    }
    private void EnsureModuleData()
    {
        if (module == EndgameModule.Cube) LoadCatalog("runes");
    }
    private void LoadCatalog(string name)
    {
        if (!State.CatalogLoaded(name) && !State.CatalogRequested(name)) Send(new EndgameRequest("catalog", catalog: name));
    }
    private void EnsureStyles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { font = NpcChatter.CnFont(), fontSize = 14, wordWrap = true, richText = false, padding = new RectOffset(2, 2, 2, 2) };
        label.normal.textColor = new Color(.91f, .9f, .84f);
        small = new GUIStyle(label) { fontSize = 12 }; small.normal.textColor = Muted;
        heading = new GUIStyle(label) { fontSize = 21, fontStyle = FontStyle.Bold }; heading.normal.textColor = Gold;
    }
    private void OnGUI()
    {
        if (client == null || !client.EndgameInWorld) return;
        EnsureStyles();
        float width = module == EndgameModule.Rift ? RiftWindowWidth : W;
        float height = module == EndgameModule.Rift ? RiftWindowHeight : H;
        float scale = Mathf.Max(.4f, Mathf.Min(UIScale.Factor, Mathf.Min((Screen.width - 20f) / width, (Screen.height - 20f) / height)));
        GUI.depth = -15;
        if (!State.Negotiated) return;
        float hudScale = Mathf.Max(.4f, Mathf.Min(UIScale.Factor, Mathf.Min((Screen.width - 20f) / W, (Screen.height - 20f) / H)));
        DrawRiftHud(hudScale); DrawRiftMarkers(hudScale);
        if (!open) return;
        Rect window = new Rect((Screen.width - width * scale) * .5f, (Screen.height - height * scale) * .5f, width * scale, height * scale);
        var frame = WinZ.Frame(this, window, 4); GUI.depth = frame.Item1;
        Vector2 oldMouse = Event.current.mousePosition;
        panelScreenMouse = oldMouse; panelScale = scale;
        if (frame.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        Matrix4x4 previous = GUI.matrix;
        try
        {
            GUI.matrix = Matrix4x4.TRS(new Vector3(window.x, window.y, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            if (module == EndgameModule.Rift)
            {
                DrawRiftWindowArtwork(new Rect(0, 0, width, height));
                if (Event.current.type != EventType.Repaint) DrawRiftTierDropdown();
            }
            else
            {
                ArtworkSkin.Panel(new Rect(0, 0, width, height), highlighted: true);
                ArtworkSkin.Panel(new Rect(9, 9, width - 18, 79));
            }
            Text(new Rect(26, 17, module == EndgameModule.Rift ? width - 82 : 760, 32), module == EndgameModule.Cube ? "周天鼎" : "演劫洞天", heading);
            Text(new Rect(27, 52, module == EndgameModule.Rift ? width - 74 : 790, 25), module == EndgameModule.Cube ? "将背包物品放入鼎中，问鼎后确认炼化。" : "持符钥开启洞天；队友需在队长附近。", small);
            if (ArtworkSkin.Button(new Rect(width - 48, 18, 27, 27), "×")) Close();
            if (module == EndgameModule.Cube) DrawCube();
            else DrawRift();
            if (module != EndgameModule.Rift) ArtworkSkin.Panel(new Rect(19, height - 58, width - 38, 39));
            string status = State.Pending ? "结果确认中：" + State.PendingRequest.Op + "。查询会沿用同一请求。" : State.SnapshotLoading ? "正在拼合完整星图…" : State.Message;
            float statusY = module == EndgameModule.Rift ? height - 64 : height - 50;
            Text(new Rect(30, statusY, module == EndgameModule.Rift ? width - 290 : 645, module == EndgameModule.Rift ? 36 : 26), status, small, State.Error.Length > 0 ? new Color(1f, .6f, .48f) : Jade);
            if (State.Pending && ArtworkSkin.Button(new Rect(width - 254, statusY, 105, 25), "查询原请求")) client.RetryEndgame();
            if (ArtworkSkin.Button(new Rect(width - 140, statusY, 108, 25), "刷新")) Refresh();
            if (module == EndgameModule.Rift && Event.current.type == EventType.Repaint) DrawRiftTierDropdown();
            if (Event.current.type == EventType.MouseDown && new Rect(0, 0, width, height).Contains(Event.current.mousePosition))
            { Event.current.Use(); PlayerController.SuppressWorldClick(); }
        }
        finally { GUI.matrix = previous; if (frame.Item2) Event.current.mousePosition = oldMouse; }
    }
    private void ShowPanelTooltip(string text) => UITip.ShowAtForIndependentRuntime(text, panelScale, panelScreenMouse);
    private static void Text(Rect rect, string value, GUIStyle style, Color? color = null)
    {
        Color previous = GUI.color; if (color.HasValue) GUI.color = color.Value;
        GUI.Label(rect, value ?? "", style); GUI.color = previous;
    }
}
}
