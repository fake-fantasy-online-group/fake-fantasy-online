using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Shared server binding for independent NPC workbenches and the active-rift HUD.</summary>
internal enum EndgameModule { Runes, Cube, Rift }
public sealed partial class EndgamePanel : MonoBehaviour
{
    internal static EndgamePanel Current { get; private set; }
    private IEndgameClient client;
    private EndgameClientState State => client?.Endgame;
    private bool open, confirmLeave;
    private EndgameModule module;
    private int socket, recipeID = 1010, tier = 1;
    private string selectedUID = "", selectedRuneUID = "";
    private readonly List<string> orderedRunes = new List<string>();
    private Vector2 itemScroll, detailScroll, runeScroll, recipeScroll;
    private Vector2 panelScreenMouse;
    private float panelScale = 1f;
    private GUIStyle label, heading, small, textField;
    private EndgameRequest quotedInputs;
    private long generation = -1;
    private float nextRiftRefresh;
    private string requestedRecipeCursor;
    private const float W = 940f, H = 660f;
    private static readonly Color Gold = new Color(1f, .83f, .39f);
    private static readonly Color Jade = new Color(.49f, .88f, .79f);
    private static readonly Color Muted = new Color(.72f, .73f, .73f);

    public void Bind(IEndgameClient source)
    {
        if (!ReferenceEquals(client, source)) { client = source; ClearSelection(); open = false; generation = -1; observedGeneration = -1; announcement = ""; }
        Current = this;
    }
    public void Unbind(IEndgameClient source)
    {
        if (!ReferenceEquals(client, source)) return;
        WinZ.Forget(this); client = null; open = false; announcement = ""; observedGeneration = -1; ClearSelection();
    }
    private void OnDestroy() { WinZ.Forget(this); if (Current == this) Current = null; }
    private void ClearSelection()
    {
        selectingCubeTarget = selectingCubeOutput = false; requestedRecipeCursor = null;
        selectedUID = selectedRuneUID = ""; orderedRunes.Clear(); quotedInputs = null; confirmLeave = false;
        itemScroll = detailScroll = runeScroll = recipeScroll = Vector2.zero;
    }
    private void Close() { WinZ.Forget(this); open = false; ClearSelection(); State?.InvalidateQuote(); }
    private void Update()
    {
        if (State == null || !client.EndgameInWorld)
        { if (open || selectedUID.Length > 0) { WinZ.Forget(this); ClearSelection(); } open = false; announcement = ""; observedGeneration = -1; return; }
        if (generation != State.Generation) { generation = State.Generation; ClearSelection(); open = false; }
        ObserveRiftPhase();
        if (open && module == EndgameModule.Cube) LoadRecipeMetadata();
        if (open && Input.GetKeyDown(KeyCode.Escape)) Close();
        if (State.Negotiated && State.Rift != null && PollRun(State.Rift) && Time.unscaledTime >= nextRiftRefresh)
        { nextRiftRefresh = Time.unscaledTime + 3f; QueryRift(); }
    }
    private bool Send(EndgameRequest request) => client != null && client.SendEndgame(request);
    private void QueryRift() => Send(new EndgameRequest("rift.state", runID: State.Rift == null ? "" : State.Rift.RunID));
    private void Refresh()
    {
        requestedRecipeCursor = null;
        Send(new EndgameRequest("snapshot"));
        if (module == EndgameModule.Rift) QueryRift();
        if (open) EnsureModuleData();
    }
    private void EnsureModuleData()
    {
        if (module == EndgameModule.Runes || module == EndgameModule.Cube) LoadCatalog("runes");
        if (module == EndgameModule.Cube) LoadRecipeMetadata();
    }
    private void LoadRecipeMetadata()
    {
        // Recipes are actionable workbench metadata, never a player encyclopedia.
        // Follow the authoritative cursor so later rune outputs stay craftable.
        string cursor = State.NextCatalogCursor("recipes");
        // Record the attempt before Send: transport failure releases read-request state
        // and returns false. Both transport and server failures wait for explicit refresh.
        if (State.CatalogRequested("recipes") || !State.CatalogHasMore("recipes") || requestedRecipeCursor == cursor) return;
        requestedRecipeCursor = cursor;
        Send(new EndgameRequest("catalog", catalog: "recipes", cursor: cursor));
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
        textField = new GUIStyle(GUI.skin.textField) { font = NpcChatter.CnFont(), fontSize = 14 };
    }
    private void OnGUI()
    {
        if (client == null || !client.EndgameInWorld) return;
        EnsureStyles();
        float scale = Mathf.Max(.4f, Mathf.Min(UIScale.Factor, Mathf.Min((Screen.width - 20f) / W, (Screen.height - 20f) / H)));
        GUI.depth = -15;
        if (!State.Negotiated) return;
        DrawRiftHud(scale); DrawRiftMarkers(scale); DrawRiftAnnouncement(scale);
        if (!open) return;
        Rect window = new Rect((Screen.width - W * scale) * .5f, (Screen.height - H * scale) * .5f, W * scale, H * scale);
        var frame = WinZ.Frame(this, window, 4); GUI.depth = frame.Item1;
        Vector2 oldMouse = Event.current.mousePosition;
        panelScreenMouse = oldMouse; panelScale = scale;
        if (frame.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        Matrix4x4 previous = GUI.matrix;
        try
        {
            GUI.matrix = Matrix4x4.TRS(new Vector3(window.x, window.y, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            ArtworkSkin.Panel(new Rect(0, 0, W, H), highlighted: true);
            ArtworkSkin.Panel(new Rect(9, 9, W - 18, 79));
            Text(new Rect(26, 17, 760, 32), module == EndgameModule.Runes ? "星篆镶嵌" : module == EndgameModule.Cube ? "周天鼎" : "演劫洞天", heading);
            Text(new Rect(27, 52, 790, 25), module == EndgameModule.Runes ? "选择器物、孔位与背包星篆。" : module == EndgameModule.Cube ? "选择炼化方式与材料，问鼎后确认投料。" : "持符入境，镇煞定星。", small);
            if (ArtworkSkin.Button(new Rect(W - 48, 18, 27, 27), "×")) Close();
            if (module == EndgameModule.Runes) DrawRunes();
            else if (module == EndgameModule.Cube) DrawCube();
            else DrawRift();
            ArtworkSkin.Panel(new Rect(19, H - 58, W - 38, 39));
            string status = State.Pending ? "结果确认中：" + State.PendingRequest.Op + "。查询会沿用同一请求。" : State.SnapshotLoading ? "正在拼合完整星图…" : State.Message;
            Text(new Rect(30, H - 50, 645, 26), status, small, State.Error.Length > 0 ? new Color(1f, .6f, .48f) : Jade);
            if (State.Pending && ArtworkSkin.Button(new Rect(W - 254, H - 50, 105, 25), "查询原请求")) client.RetryEndgame();
            if (ArtworkSkin.Button(new Rect(W - 140, H - 50, 108, 25), "刷新")) Refresh();
            if (Event.current.type == EventType.MouseDown && new Rect(0, 0, W, H).Contains(Event.current.mousePosition))
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
