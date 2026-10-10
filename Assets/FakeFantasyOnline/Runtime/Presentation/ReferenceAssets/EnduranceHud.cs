using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class EnduranceHud : MonoBehaviour
{
    private const int W = 80;
    private const int H = 98;
    private const float THRESH_RED = 0.2f;
    private const float THRESH_GOLD = 0.33f;
    public static EnduranceHud I;
    private Texture2D bk;
    private readonly Texture2D[] gold = new Texture2D[10];
    private readonly Texture2D[] red = new Texture2D[10];
    private bool loaded;
    private readonly int[] partState = new int[10];
    private bool show;

    public static void Ensure()
    {
        if (I == null) I = new GameObject("EnduranceHud").AddComponent<EnduranceHud>();
    }

    private void Awake()
    {
        if (I == null) I = this;
    }

    private static int PartIndex(int slot)
    {
        if (slot >= 1 && slot <= 10) return unchecked(slot - 1);
        return slot == 13 ? 3 : -1;
    }

    public static void UpdateFromEquipped(IReadOnlyList<WorldInventoryEquippedItemState> equipped)
    {
        Ensure();
        I.Recalc(equipped);
    }

    private void Recalc(IReadOnlyList<WorldInventoryEquippedItemState> equipped)
    {
        for (int i = 0; i < 10; i++) partState[i] = 0;
        bool any = false;
        if (equipped != null)
        {
            foreach (WorldInventoryEquippedItemState item in equipped)
            {
                if (item.MaximumDurability <= 0) continue;
                int part = PartIndex(item.Slot);
                if (part < 0) continue;
                int state = (float)item.CurrentDurability <= (float)item.MaximumDurability * THRESH_RED ? 2
                    : (float)item.CurrentDurability <= (float)item.MaximumDurability * THRESH_GOLD ? 1 : 0;
                if (state != 0) any = true;
                partState[part] = state;
            }
        }
        show = any;
    }

    private void EnsureLoaded()
    {
        if (loaded) return;
        if (ResLoader.I == null) return;
        bk = ResLoader.I.LoadGsa("res/endurance/endurancebk.gsa");
        for (int i = 0; i < 10; i++)
        {
            string path = string.Format("res/endurance/endurance{0:D2}.gsa", i);
            gold[i] = ResLoader.I.LoadGsaLayer(path, 0);
            red[i] = ResLoader.I.LoadGsaLayer(path, 1);
        }
        loaded = bk != null;
    }

    private void OnGUI()
    {
        if (!show) return;
        EnsureLoaded();
        if (bk == null) return;
        float scale = UIScale.Factor;
        float width = W * scale, height = H * scale;
        float x = (float)Screen.width - width + (-16f * scale);
        float y = (float)Screen.height - height - width;
        int oldDepth = GUI.depth;
        GUI.depth = 50;
        GUI.DrawTexture(new Rect(x, y, width, height), bk);
        for (int i = 0; i < 10; i++)
        {
            Texture2D texture = partState[i] == 2 ? red[i] : partState[i] == 1 ? gold[i] : null;
            if (texture != null) GUI.DrawTexture(new Rect(x, y, width, height), texture);
        }
        GUI.depth = oldDepth;
    }
}

}
