using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Real server-driven endgame controls, bound to the existing native host and artwork.</summary>
public sealed partial class EndgamePanel : MonoBehaviour
{
    internal static EndgamePanel Current { get; private set; }
    private IEndgameClient client;
    private EndgameClientState State => client?.Endgame;
    private bool open, showCatalog, showRuneCatalog, confirmLeave;
    private int tab, socket, recipeID = 1010, tier = 1;
    private string selectedUID = "", selectedRuneUID = "", search = "";
    private readonly List<string> orderedRunes = new List<string>();
    private Vector2 itemScroll, catalogScroll, detailScroll, runeScroll, recipeScroll;
    private Vector2 panelScreenMouse;
    private float panelScale = 1f;
    private GUIStyle label, heading, small, textField;
    private EndgameRequest quotedInputs;
    private long generation = -1;
    private float nextRiftRefresh;
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
        showCatalog = showRuneCatalog = showFormationCatalog = false;
        selectedUID = selectedRuneUID = ""; orderedRunes.Clear(); quotedInputs = null; confirmLeave = false;
        itemScroll = catalogScroll = detailScroll = runeScroll = recipeScroll = Vector2.zero;
    }
    private void Close() { WinZ.Forget(this); open = false; ClearSelection(); State?.InvalidateQuote(); }
    private void Toggle()
    {
        if (open) { Close(); return; }
        open = true; Refresh(); EnsureCatalogs();
    }
    private void Update()
    {
        if (State == null || !client.EndgameInWorld)
        { if (open || selectedUID.Length > 0) { WinZ.Forget(this); ClearSelection(); } open = false; announcement = ""; observedGeneration = -1; return; }
        if (generation != State.Generation) { generation = State.Generation; ClearSelection(); open = false; }
        ObserveRiftPhase();
        if (State.Negotiated && Input.GetKeyDown(KeyCode.F8)) Toggle();
        if (open && Input.GetKeyDown(KeyCode.Escape)) Close();
        if (State.Negotiated && State.Rift != null && PollRun(State.Rift) && Time.unscaledTime >= nextRiftRefresh)
        { nextRiftRefresh = Time.unscaledTime + 3f; QueryRift(); }
    }
    private bool Send(EndgameRequest request) => client != null && client.SendEndgame(request);
    private void QueryRift() => Send(new EndgameRequest("rift.state", runID: State.Rift == null ? "" : State.Rift.RunID));
    private void Refresh() { Send(new EndgameRequest("snapshot")); QueryRift(); if (open) EnsureCatalogs(); }
    private void EnsureCatalogs()
    {
        if (tab == 1 || tab == 2) LoadCatalog("runes");
        if (tab == 1 && showFormationCatalog) LoadCatalog("formations");
        if (tab == 2) LoadCatalog("recipes");
        if (tab == 0 && showCatalog) LoadCatalog("uniques");
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
        Rect launch = new Rect(Screen.width - 144f * scale, 178f * scale, 130f * scale, 32f * scale);
        if (!State.Negotiated)
        {
            ArtworkSkin.ButtonFace(launch, "演劫 · 未开放", scale, false);
            if (launch.Contains(Event.current.mousePosition)) UITip.Show(State.Message.Length > 0 ? State.Message : "当前服务器尚未启用演劫系统。", scale);
            return;
        }
        if (ArtworkSkin.Button(launch, "演劫 · F8", scale, selected: open)) Toggle();
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
            Text(new Rect(26, 17, 760, 32), "太清演劫录", heading);
            Text(new Rect(27, 52, 790, 25), "持符入境，镇煞定星。以旧物承星，以炉火去煞。", small);
            if (ArtworkSkin.Button(new Rect(W - 48, 18, 27, 27), "×")) Close();
            string[] names = { "仙遗", "星篆 · 宿阵", "周天鼎", "演劫洞天" };
            for (int i = 0; i < names.Length; i++)
                if (ArtworkSkin.Tab(new Rect(20 + i * 222, 97, 214, 32), names[i], selected: tab == i)) { tab = i; detailScroll = Vector2.zero; EnsureCatalogs(); }
            if (tab == 0) DrawEquipment(); else if (tab == 1) DrawRunes(); else if (tab == 2) DrawCube(); else DrawRift();
            ArtworkSkin.Panel(new Rect(19, H - 58, W - 38, 39));
            string status = State.Pending ? "结果确认中：" + State.PendingRequest.Op + "。查询会沿用同一请求。" : State.SnapshotLoading ? "正在拼合完整星图…" : State.Message;
            Text(new Rect(30, H - 50, 645, 26), status, small, State.Error.Length > 0 ? new Color(1f, .6f, .48f) : Jade);
            if (State.Pending && ArtworkSkin.Button(new Rect(W - 254, H - 50, 105, 25), "查询原请求")) client.RetryEndgame();
            if (ArtworkSkin.Button(new Rect(W - 140, H - 50, 108, 25), "刷新星图")) Refresh();
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
    private void MoreCatalog(string name, Rect rect)
    {
        bool pending = State.CatalogRequested(name), more = State.CatalogHasMore(name);
        if (ArtworkSkin.Button(rect, pending ? "载入中…" : more ? "继续翻页" : "图鉴已尽", enabled: !pending && more))
            Send(new EndgameRequest("catalog", catalog: name, cursor: State.NextCatalogCursor(name)));
    }
}
}
