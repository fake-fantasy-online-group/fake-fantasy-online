using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        // Official 0x06000aba.
        private void DrawFittingRoom(Vector2 mouse)
        {
            FrameBox(L(28f, 65f, 812f, 524f));
            tipText = null;
            FrameBox(L(36f, 73f, 393f, 508f));
            FrameBox(L(439f, 73f, 393f, 508f));
            if (FitSubTab(44f, 79f, "人物", fitSub == 0, mouse)) fitSub = 0;
            if (FitSubTab(118f, 79f, "宠物", fitSub == 1, mouse)) fitSub = 1;
            Rect viewport = L(44f, 105f, 377f, fitSub == 0 ? 300f : 468f);
            FrameBox(viewport);
            Rect previewArea = new Rect(viewport.x + 7f * s, viewport.y + 7f * s,
                viewport.width - 14f * s, viewport.height - 14f * s);
            FittingRoomPreview preview = FittingRoomPreview.Ensure();
            RenderTexture rt = preview.RT;
            if (rt != null)
            {
                float aspect = (float)rt.width / rt.height;
                float width = previewArea.width;
                float height = previewArea.height;
                if (previewArea.width / previewArea.height > aspect) width = height * aspect;
                else height = width / aspect;
                GUI.DrawTexture(new Rect(previewArea.center.x - width * 0.5f, previewArea.center.y - height * 0.5f, width, height), rt);
            }
            if (fitSub == 0) DrawFitCharacter(mouse, preview);
            else DrawFitPet(mouse, preview);
            DrawFitRightList(mouse, 439f, 393f, 73f, 508f);
            if (!string.IsNullOrEmpty(tipText)) UITip.Show(tipText, s);
        }

        // Official 0x06000acd.
        private bool FitSubTab(float x, float y, string name, bool selected, Vector2 mouse)
        {
            Rect rect = L(x, y, 70f, 22f);
            TextButtonFace(rect, name, mouse, selected);
            return Click(rect, mouse);
        }

        // Official 0x06000ac3.
        private bool FitRow(float x, float y, float width, float height, Texture2D icon, string name,
            string tag, int price, bool selected, Vector2 mouse)
        {
            Rect rect = L(x, y, width, height);
            ArtworkSkin.Row(rect, s, selected || rect.Contains(mouse));
            if (icon != null) ItemIconUI.Draw(L(x + 2f, y + (height - 28f) * 0.5f, 28f, 28f), icon, s);
            if (!string.IsNullOrEmpty(tag)) LblC(L(x + width - 30f, y, 28f, height), tag, GRAY);
            if (price >= 0) LblC(L(x + width - 94f, y, 60f, height), price + "彩玉", YEL);
            Lbl(x + 34f, y + (height - 12f) * 0.5f, Trunc(name, price >= 0 ? 8 : 12), selected ? GOLD : Color.white);
            return Click(rect, mouse);
        }

        // Official 0x06000abe. The server page is consumed in its received order.
        private void DrawFitRightList(Vector2 mouse, float boxX, float boxWidth, float boxTop, float boxHeight)
        {
            InputStyle(ref _fitSearchSt, 12f, TextAnchor.MiddleLeft);
            float x = boxX + 8f;
            Rect searchRect = L(x, boxTop + 8f, boxWidth - 16f, 22f);
            ArtworkSkin.Input(searchRect, s, GUI.GetNameOfFocusedControl() == "fitSearch");
            GUI.SetNextControlName("fitSearch");
            string edit = GUI.TextField(searchRect, fitSearch ?? "", _fitSearchSt);
            if (edit != fitSearch) { fitSearch = edit; fitPage = 0; }
            if (string.IsNullOrEmpty(fitSearch) && GUI.GetNameOfFocusedControl() != "fitSearch")
                LblC(searchRect, fitSub == 0 ? "搜索变装/部位灵…" : "搜索宠物蛋…", GRAY);
            float listY;
            int perPage;
            if (fitSub == 1)
            {
                listY = boxTop + 38f;
                perPage = Mathf.Max(1, Mathf.Max(1, (int)((boxHeight - 46f) / 34f)) - 1);
                SyncFitPage(2, "", fitSearch, perPage);
                for (int i = 0; i < perPage && i < fitCatalog.Count; i++)
                {
                    FitItem item = fitCatalog[i];
                    float y = listY + i * 34f;
                    if (FitRow(x, y, boxWidth - 26f, 31f, FitIconOf(item.id), item.name, "", item.price, fitPetItem == item.id, mouse))
                    { fitPetModel = item.model; fitPetItem = item.id; }
                    if (L(x, y, boxWidth - 26f, 31f).Contains(mouse)) tipText = FitTip(item.id);
                }
                if (fitCatalog.Count == 0) LblC(L(x, listY + 40f, boxWidth - 26f, 14f), fitPaged ? "无宠物蛋" : "载入中…", GRAY);
            }
            else
            {
                if (FitSubTab(x, boxTop + 34f, "变装", fitCatTab == 0, mouse)) { fitCatTab = 0; fitPartTab = -1; fitPage = 0; }
                if (FitSubTab(boxX + 82f, boxTop + 34f, "部位灵", fitCatTab == 1, mouse)) { fitCatTab = 1; fitPartTab = -1; fitPage = 0; }
                string[] parts = fitCatTab == 0 ? AvatarPartOrder : SoulPartOrder;
                int category = fitCatTab;
                float partY = boxTop + 60f;
                if (FitPartTab(x, partY, 32f, "全部", fitPartTab < 0, mouse)) { fitPartTab = -1; fitPage = 0; }
                float partX = x + 35f;
                for (int i = 0; i < parts.Length; i++)
                {
                    float width = parts[i].Length * 12f + 8f;
                    if (partX + width > boxX + boxWidth - 14f) { partX = x; partY += 20f; }
                    if (FitPartTab(partX, partY, width, parts[i], fitPartTab == i, mouse)) { fitPartTab = i; fitPage = 0; }
                    partX += width + 3f;
                }
                listY = partY + 24f;
                perPage = Mathf.Max(1, Mathf.Max(1, (int)((boxTop + boxHeight - listY - 4f) / 34f)) - 1);
                string part = fitPartTab >= 0 && fitPartTab < parts.Length ? parts[fitPartTab] : "";
                SyncFitPage((byte)(category == 0 ? 0 : 1), part, fitSearch, perPage);
                for (int i = 0; i < perPage && i < fitCatalog.Count; i++)
                {
                    FitItem item = fitCatalog[i];
                    bool selected;
                    if (category == 0)
                    {
                        string skin = SuitFxDB.Skin(item.id).Item1;
                        selected = !string.IsNullOrEmpty(skin) && fitOn.TryGetValue(skin, out int currentSkin) && currentSkin == item.id;
                    }
                    else selected = fitSouls.TryGetValue(string.IsNullOrEmpty(item.part) ? "灵" : item.part, out int soulCurrent) && soulCurrent == item.id;
                    float y = listY + i * 34f;
                    if (FitRow(x, y, boxWidth - 26f, 31f, FitIconOf(item.id), item.name, "", item.price, selected, mouse)) ToggleTryOn(item.id);
                    if (L(x, y, boxWidth - 26f, 31f).Contains(mouse)) tipText = FitTip(item.id);
                }
                if (fitCatalog.Count == 0) LblC(L(x, listY + 40f, boxWidth - 26f, 14f), fitPaged ? "无匹配" : "载入中…", GRAY);
            }
            FitPageBar(boxX, boxWidth, listY + perPage * 34f + 2f, perPage, mouse);
        }

        // Official 0x06000ac1. Disabled page arrows still consume their Click.
        private void FitPageBar(float x, float width, float y, int perPage, Vector2 mouse)
        {
            int pages = Mathf.Max(1, (perPage + fitTotal - 1) / perPage);
            if (fitPage >= pages) fitPage = pages - 1;
            Rect up = L(x + 20f, y, 44f, 22f);
            Rect down = L(x + width - 78f, y, 44f, 22f);
            ArtworkSkin.IconFace(up, pageUp, up.Contains(mouse), up.Contains(mouse) && Input.GetMouseButton(0),
                fitPage > 0 && !SysMsgBox.Visible);
            ArtworkSkin.IconFace(down, pageDn, down.Contains(mouse), down.Contains(mouse) && Input.GetMouseButton(0),
                fitPage < pages - 1 && !SysMsgBox.Visible);
            if (Click(up, mouse) && fitPage > 0) fitPage--;
            if (Click(down, mouse) && fitPage < pages - 1) fitPage++;
            LblC(L(x + 66f, y + 2f, width - 150f, 18f), (fitPage + 1) + " / " + pages + "  (共 " + fitTotal + " 件)", GRAY);
            Event e = Event.current;
            if (L(x, y - perPage * 34f, width, perPage * 34f + 24f).Contains(mouse) && e.type == EventType.ScrollWheel)
            {
                fitPage = Mathf.Clamp(fitPage + (e.delta.y < 0f ? -1 : 1), 0, pages - 1);
                e.Use();
            }
        }
    }
}
