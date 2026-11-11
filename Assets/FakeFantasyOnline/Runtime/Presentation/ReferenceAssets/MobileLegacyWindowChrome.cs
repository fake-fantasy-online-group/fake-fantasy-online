using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Mobile-only uniform close target for legacy windows whose native close art is 20x19
// design pixels. The original button keeps working; this one sits over the window's
// top-right corner with a 44-point touch area and calls the same close as Esc/UiEsc.
internal sealed class MobileLegacyWindowChrome : MonoBehaviour
{
    internal const float HitPoints = 40f;
    internal const float FacePoints = 24f;

    private static readonly Dictionary<Type, Action> Closers = new Dictionary<Type, Action>
    {
        { typeof(MailDlg), MailDlg.Toggle },
        { typeof(FamilyDlg), FamilyDlg.Toggle },
        { typeof(FamilyDetailDlg), FamilyDetailDlg.Toggle },
        { typeof(FamilyStashDlg), FamilyStashDlg.Close },
        { typeof(GossipDlg), GossipDlg.Toggle },
        { typeof(RackShopDlg), RackShopDlg.Toggle },
        { typeof(TaskDlg), TaskDlg.Toggle },
        { typeof(TeamDlg), TeamDlg.Toggle },
        { typeof(StoreDlg), StoreDlg.Toggle },
        { typeof(HelpWinDlg), HelpWinDlg.Toggle },
        { typeof(MasterListDlg), MasterListDlg.Toggle },
        { typeof(GroceryStatusDlg), GroceryStatusDlg.Toggle },
        { typeof(PetArousalDlg), PetArousalDlg.Toggle },
        { typeof(PetForgeDlg), PetForgeDlg.CloseNow },
        { typeof(VoiceSetDlg), VoiceSetDlg.Toggle },
        { typeof(SculpDlg), SculpDlg.Toggle },
        { typeof(ComposeDlg), ComposeDlg.Toggle },
        { typeof(AttrWashDlg), AttrWashDlg.CloseNow },
        { typeof(EquipFuseDlg), EquipFuseDlg.CloseNow },
        { typeof(FigureDlg), FigureDlg.Toggle },
        { typeof(SysConfigDlg), SysConfigDlg.Toggle },
        { typeof(ShopDlg), ShopDlg.Hide },
        { typeof(StallDlg), StallDlg.CloseNow },
        { typeof(RepairDlg), RepairDlg.Hide },
        { typeof(InventoryDlg), InventoryDlg.Toggle },
        { typeof(EquipDlg), EquipDlg.Toggle },
        { typeof(Win05), Win05.CloseNow },
    };

    private readonly List<object> owners = new List<object>();
    private readonly List<Rect> rects = new List<Rect>();
    private object pressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!UnityEngine.Application.isMobilePlatform) return;
        var go = new GameObject("~MobileLegacyWindowChrome");
        DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<MobileLegacyWindowChrome>();
    }

    // Top-right square inside the window, so the hit never leaks into the world.
    internal static Rect HitRect(Rect window, float scale)
    {
        float size = Mathf.Min(HitPoints * scale, window.width * .5f, window.height * .5f);
        return new Rect(window.xMax - size, window.y, size, size);
    }

    internal static Rect FaceRect(Rect hit, float scale)
    {
        float face = Mathf.Min(FacePoints * scale, hit.width);
        return new Rect(hit.xMax - face - 3f * scale, hit.y + 3f * scale, face, face);
    }

    private void OnGUI()
    {
        if (!MobileClientPresentation.Enabled || MobileClientPresentation.TrueModalOpen) { pressed = null; return; }
        GUI.depth = 1;
        WinZ.CollectFresh(owners, rects);
        Event e = Event.current;
        float scale = MobileClientPresentation.Scale;
        Matrix4x4 matrix = GUI.matrix; GUI.matrix = Matrix4x4.identity;
        try
        {
            for (int i = owners.Count - 1; i >= 0; i--)
            {
                object owner = owners[i];
                if (!Closers.TryGetValue(owner.GetType(), out Action close)) continue;
                Rect hit = HitRect(rects[i], scale);
                if (Covered(i, hit.center)) continue;
                bool inside = hit.Contains(e.mousePosition);
                if (e.type == EventType.MouseDown && inside)
                {
                    pressed = owner; e.Use();
                }
                else if (e.type == EventType.MouseUp && ReferenceEquals(pressed, owner))
                {
                    pressed = null; e.Use();
                    if (inside) { close(); return; }
                }
                else if (e.type == EventType.Repaint)
                    ArtworkSkin.CloseFace(FaceRect(hit, scale), inside, ReferenceEquals(pressed, owner) && inside);
            }
        }
        finally { GUI.matrix = matrix; }
    }

    private bool Covered(int index, Vector2 point)
    {
        for (int j = index + 1; j < rects.Count; j++)
            if (rects[j].Contains(point)) return true;
        return false;
    }
}

}
