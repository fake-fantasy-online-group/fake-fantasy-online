using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    private void SetPickVal(int v)
    {
        pickQty = Mathf.Clamp(v, 1, pickMax);
        pickText = pickQty.ToString();
    }

    private GUIStyle PickNumStyle()
    {
        if (_pickNumSt == null)
        {
            _pickNumSt = new GUIStyle(GUIStyle.none)
            {
                alignment = TextAnchor.MiddleCenter,
                font = NpcChatter.CnFont()
            };
            _pickNumSt.normal.textColor = Color.white;
        }
        _pickNumSt.fontSize = Mathf.RoundToInt(12f * s);
        return _pickNumSt;
    }

    private long CartTotal()
    {
        long total = 0;
        foreach (Cart item in cart) total = unchecked(total + item.unit * item.qty);
        return total;
    }

    private int AffordMax(int price)
    {
        if (price <= 0) return 0;
        return (int)System.Math.Min(999L, System.Math.Max(0L, InventoryDlg.Money / price));
    }
}
}
