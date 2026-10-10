using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class PetForgeDlg
{
    // Native OnGUI confirmation block and captured callback 06000955.
    private void DrawConfirmation(Vector2 mp)
    {
        string label = kind == 2 ? "炼 妖" : kind == 0 ? "炼 化" : "学 习";
        if (Btn(L(107f, 372f, 110f, 26f), label, mp, ok))
        {
            string message = confirmText.Length > 0 ? confirmText :
                kind == 2 ? "确定炼妖吗?" : kind == 0 ? "确定炼化吗?" : "确定学习吗?";
            int k = kind;
            int pt = petTab;
            int ps = petSlot;
            int it2 = itemTab;
            int isl = itemSlot;
            int sid = selSkill;
            int p2t = pet2Tab;
            int p2s = pet2Slot;
            SysMsgBox.Show(message, () =>
            {
                NetClient.I?.PetForgeForIndependentRuntime(1, unchecked((byte)k), unchecked((byte)pt), ps,
                    unchecked((byte)it2), isl, sid, unchecked((byte)(p2t < 0 ? 0 : p2t)),
                    p2t < 0 ? -1 : p2s);
            }, null);
        }
    }
}
}
