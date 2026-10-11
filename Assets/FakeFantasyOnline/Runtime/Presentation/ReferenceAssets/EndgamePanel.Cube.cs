using System;
using System.Text;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private bool selectingCubeTarget, selectingCubeOutput;
    private void DrawCube()
    {
        EndgameRecipeView chosen = null; foreach (var recipe in State.Recipes) if (recipe.ID == recipeID) chosen = recipe;
        DrawCubeChoices(chosen);
        ArtworkSkin.Panel(new Rect(279, 142, 639, 444));
        Text(new Rect(298, 151, 585, 31), chosen == null ? "周天鼎" : chosen.Name, heading);
        Text(new Rect(299, 186, 581, 48), chosen == null ? "以旧物承星，以炉火去煞。投料前可查看所得与耗费。" : chosen.Description, small);
        EndgameItemView target = Selected();
        if (chosen != null && chosen.TargetRequired)
        {
            Text(new Rect(299, 239, 427, 26), target == null ? "器物：未选择" : "器物：" + target.Name, small, target != null && target.UniqueID > 0 ? (Color?)Gold : null);
            if (ArtworkSkin.Button(new Rect(746, 239, 145, 26), "选择器物", enabled: !State.Pending))
            { selectingCubeTarget = true; selectingCubeOutput = false; itemScroll = Vector2.zero; }
        }
        else if (chosen != null && chosen.OutputItemID > 0)
        {
            Text(new Rect(299, 239, 427, 26), "成品：" + RuneName(chosen.OutputItemID), small);
            if (ArtworkSkin.Button(new Rect(746, 239, 145, 26), "选择成品星篆", enabled: !State.Pending))
            { selectingCubeOutput = true; selectingCubeTarget = false; recipeScroll = Vector2.zero; }
        }
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
            Text(new Rect(407, 277, 477, 83), chosen == null ? "正在读取炼化方式…" : "投料：" + (chosen.TargetRequired ? "背包中的未锁定器物" : "所需材料") + "\n基础铜币：" + chosen.Money + "\n具体消耗、回收与风险以问鼎预览为准。", small);
        }
        EndgameQuoteView quote = State.Quote;
        ArtworkSkin.Panel(new Rect(298, 381, 594, 145), highlighted: quote != null);
        var costs = new StringBuilder("材料与耗费（实际耗费以问鼎为准）");
        if (chosen != null)
        {
            costs.Append("\n铜币：").Append(chosen.Money);
            foreach (var material in chosen.Materials)
            {
                int owned = 0; foreach (var wallet in State.Materials) if (wallet.ItemID == material.ItemID) owned = wallet.Count;
                costs.Append("\n").Append(material.Name).Append("：持有 ").Append(owned).Append(" / 需要 ").Append(material.Count).Append(owned < material.Count ? "（不足）" : "");
            }
        }
        if (chosen != null)
            foreach (var material in chosen.Materials)
            {
                string source = MaterialSource(material.ItemID);
                if (source.Length > 0) costs.Append("\n").Append(material.Name).Append("来源：").Append(source);
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
    private static string OperationName(EndgameRecipeView recipe)
    {
        if (recipe.Operation == "rune.inscribe") return "凝髓成篆";
        if (recipe.Operation == "rune.convert") return "同象易宿";
        return recipe.Name;
    }
    private void ChooseRecipe(EndgameRecipeView recipe)
    {
        recipeID = recipe.ID; selectedRuneUID = ""; orderedRunes.Clear();
        quotedInputs = null; State.InvalidateQuote(); detailScroll = Vector2.zero;
        selectingCubeTarget = selectingCubeOutput = false;
    }
    private void DrawCubeChoices(EndgameRecipeView chosen)
    {
        ArtworkSkin.Panel(new Rect(20, 142, 247, 444));
        Text(new Rect(32, 151, 220, 28), selectingCubeTarget ? "投放器物" : selectingCubeOutput ? "选择成品" : "炼化方式", heading);
        if (selectingCubeTarget || selectingCubeOutput)
        {
            if (ArtworkSkin.Button(new Rect(32, 184, 220, 27), "返回炼化方式"))
            { selectingCubeTarget = selectingCubeOutput = false; recipeScroll = Vector2.zero; }
        }
        if (selectingCubeTarget)
        {
            DrawItemList(new Rect(27, 221, 233, 353), true);
            return;
        }
        var rows = new List<EndgameRecipeView>(); var operations = new HashSet<string>();
        foreach (var recipe in State.Recipes)
        {
            if (selectingCubeOutput)
            { if (chosen != null && recipe.Operation == chosen.Operation) rows.Add(recipe); }
            else if (operations.Add(recipe.Operation)) rows.Add(recipe);
        }
        Rect list = new Rect(27, selectingCubeOutput ? 221 : 187, 233, selectingCubeOutput ? 353 : 387);
        recipeScroll = GUI.BeginScrollView(list, recipeScroll, new Rect(0, 0, 210, Math.Max(list.height, rows.Count * 38)));
        bool choosingOutput = selectingCubeOutput;
        for (int i = 0; i < rows.Count; i++)
        {
            var recipe = rows[i];
            bool selected = choosingOutput ? recipeID == recipe.ID : chosen != null && chosen.Operation == recipe.Operation;
            if (ArtworkSkin.Button(new Rect(1, i * 38, 207, 34), choosingOutput ? RuneName(recipe.OutputItemID) : OperationName(recipe), enabled: !State.Pending, selected: selected))
                ChooseRecipe(recipe);
        }
        if (rows.Count == 0) Text(new Rect(4, 8, 202, 70), State.CatalogRequested("recipes") ? "正在读取炼化方式…" : "暂无可用炼化方式，请刷新后重试。", small);
        GUI.EndScrollView();
    }
    private static string MaterialSource(int itemID)
    {
        switch (itemID)
        {
            case 1900028201: return "周天鼎解器归元或解篆归髓。";
            case 1900028202: return "演劫洞天结算所得。";
            case 1900028203: return "周天鼎分解仙遗。";
            default: return "";
        }
    }
    private void DrawInstalledOrder(EndgameItemView target, Rect rect)
    {
        if (target == null) return;
        foreach (var rune in target.Sockets)
        {
            if (rune.UID.Length == 0 || rune.ItemID == 0) continue;
            int position = orderedRunes.IndexOf(rune.UID);
            if (ArtworkSkin.Button(new Rect(rect.x + rune.Index * 117, rect.y, 111, 43), (position < 0 ? "" : (position + 1) + "·") + (rune.Name.Length > 0 ? rune.Name : RuneName(rune.ItemID)), enabled: !State.Pending, selected: position >= 0))
            { if (!orderedRunes.Remove(rune.UID)) orderedRunes.Add(rune.UID); quotedInputs = null; State.InvalidateQuote(); }
        }
    }
}
}
