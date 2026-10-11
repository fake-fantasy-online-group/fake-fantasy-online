using System;
using System.Text;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private void DrawCube()
    {
        ArtworkSkin.Panel(new Rect(20, 142, 247, 444)); Text(new Rect(32, 151, 220, 28), "鼎谱", heading);
        Rect list = new Rect(27, 187, 233, 353);
        recipeScroll = GUI.BeginScrollView(list, recipeScroll, new Rect(0, 0, 210, Math.Max(list.height, State.Recipes.Count * 38)));
        for (int i = 0; i < State.Recipes.Count; i++)
        {
            var recipe = State.Recipes[i];
            if (ArtworkSkin.Button(new Rect(1, i * 38, 207, 34), recipe.Name, enabled: !State.Pending, selected: recipeID == recipe.ID))
            { recipeID = recipe.ID; orderedRunes.Clear(); quotedInputs = null; State.InvalidateQuote(); detailScroll = Vector2.zero; }
        }
        GUI.EndScrollView(); MoreCatalog("recipes", new Rect(32, 550, 221, 26));
        ArtworkSkin.Panel(new Rect(279, 142, 639, 444));
        EndgameRecipeView chosen = null; foreach (var recipe in State.Recipes) if (recipe.ID == recipeID) chosen = recipe;
        Text(new Rect(298, 151, 585, 31), chosen == null ? "周天鼎" : chosen.Name, heading);
        Text(new Rect(299, 186, 581, 48), chosen == null ? "以旧物承星，以炉火去煞。投料前可查看所得与耗费。" : chosen.Description, small);
        EndgameItemView target = Selected();
        Text(new Rect(299, 239, 427, 26), target == null ? "器物：未选择" : "器物：" + target.Name + " · " + target.UID, small, target != null && target.UniqueID > 0 ? (Color?)Gold : null);
        if (ArtworkSkin.Button(new Rect(746, 239, 145, 26), "选择器物", enabled: !State.Pending)) { showCatalog = false; tab = 0; }
        bool reorder = chosen != null && chosen.Operation == "socket.reorder";
        bool salvageRune = chosen != null && chosen.Operation == "salvage.rune";
        bool multiple = reorder || chosen != null && (chosen.Operation == "socket.array" || chosen.Operation == "rune.convert");
        if (multiple)
        {
            Text(new Rect(299, 272, 587, 22), reorder ? "依次点击已嵌星篆，指定新宿序；再次点击可移除。" : "依次选择背包星篆；数字即投料顺序，再次点击可移除。", small);
            if (reorder) DrawInstalledOrder(target, new Rect(298, 299, 594, 49));
            else DrawRuneBank(new Rect(298, 298, 594, 76), true);
        }
        else if (salvageRune) DrawRuneBank(new Rect(298, 280, 594, 92), false);
        else
        {
            Texture2D[] frames = ArtworkSkin.Frames("res/uires/composedlg/composebtn.gsa", 1);
            if (frames != null && frames.Length > 0 && frames[0] != null) GUI.DrawTexture(new Rect(299, 275, 94, 74), frames[0], ScaleMode.ScaleToFit);
            Text(new Rect(407, 277, 477, 83), chosen == null ? "请先载入鼎谱。" : "投料：" + (chosen.TargetRequired ? "背包中的未锁定器物" : "依鼎谱选取材料") + "\n基础铜币：" + chosen.Money + "\n具体消耗、回收与风险以问鼎预览为准。", small);
        }
        EndgameQuoteView quote = State.Quote;
        ArtworkSkin.Panel(new Rect(298, 381, 594, 145), highlighted: quote != null);
        var costs = new StringBuilder("基础鼎谱耗费（实际耗费以问鼎为准）");
        if (chosen != null)
        {
            costs.Append("\n铜币：").Append(chosen.Money);
            foreach (var material in chosen.Materials)
            {
                int owned = 0; foreach (var wallet in State.Materials) if (wallet.ItemID == material.ItemID) owned = wallet.Owned;
                costs.Append("\n").Append(material.Name).Append("：持有 ").Append(owned).Append(" / 需要 ").Append(material.Count).Append(owned < material.Count ? "（不足）" : "");
            }
        }
        costs.Append(State.QuotePending ? "\n问鼎中…" : "\n预览不会扣除器物、材料或铜币。");
        string preview = costs.ToString();
        if (quote != null)
        {
            var text = new StringBuilder(quote.Description).Append("\n铜币：").Append(quote.Money);
            foreach (var material in quote.Materials) text.Append("\n").Append(material.Name).Append("：").Append(material.Owned).Append(" / ").Append(material.Count).Append(material.Owned < material.Count ? "（不足）" : "");
            text.Append("\n有效至 ").Append(quote.ExpiresAt); preview = text.ToString();
        }
        float height = Math.Max(130, small.CalcHeight(new GUIContent(preview), 548));
        detailScroll = GUI.BeginScrollView(new Rect(307, 387, 576, 131), detailScroll, new Rect(0, 0, 552, height));
        Text(new Rect(2, 1, 548, height), preview, small, quote != null ? (Color?)Gold : null); GUI.EndScrollView();
        bool validTarget = chosen != null && (!chosen.TargetRequired || target != null && target.Location == "bag");
        if (ArtworkSkin.Button(new Rect(299, 542, 278, 30), State.QuotePending ? "等待预览…" : "问鼎 · 查看完整耗费",
            enabled: validTarget && !State.Pending && !State.QuotePending && (!salvageRune || selectedRuneUID.Length > 0)))
        {
            quotedInputs = new EndgameRequest("cube.quote", State.Revision,
                targetUID: chosen.TargetRequired && target != null ? target.UID : "", runeUID: salvageRune ? selectedRuneUID : "",
                recipeID: chosen.ID, itemUIDs: multiple ? orderedRunes : null);
            Send(quotedInputs);
        }
        bool fresh = quote != null && quotedInputs != null && quote.RecipeID == recipeID && EndgameClientState.CompareRevision(quote.Revision, State.Revision) == 0 &&
            DateTimeOffset.TryParse(quote.ExpiresAt, out DateTimeOffset expiry) && expiry > DateTimeOffset.UtcNow;
        if (ArtworkSkin.Button(new Rect(591, 542, 300, 30), "照此投料 · 炼化", enabled: fresh && !State.Pending && !State.QuotePending))
            Send(new EndgameRequest("cube.execute", quote.Revision, quoteID: quote.QuoteID));
    }
    private void DrawInstalledOrder(EndgameItemView target, Rect rect)
    {
        if (target == null) return;
        foreach (var rune in target.Sockets)
        {
            if (rune.UID.Length == 0 || rune.ItemID == 0) continue;
            int position = orderedRunes.IndexOf(rune.UID);
            if (ArtworkSkin.Button(new Rect(rect.x + rune.Index * 117, rect.y, 111, 43), (position < 0 ? "" : (position + 1) + "·") + rune.Name, enabled: !State.Pending, selected: position >= 0))
            { if (!orderedRunes.Remove(rune.UID)) orderedRunes.Add(rune.UID); quotedInputs = null; State.InvalidateQuote(); }
        }
    }
}
}
