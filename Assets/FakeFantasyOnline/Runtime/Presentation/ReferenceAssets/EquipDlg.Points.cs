using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipDlg
{
    // 030d ARM2197..3428 / x862238..3232, reached only under the native font gate.
    private void DrawPoints(GuiFrame f, Event ev)
    {
        f.V(112,243,At(34).ToString());
        f.V(112,257,At(7).ToString());
        f.V(92,270,At(15).ToString());
        f.V(171,244,At(8).ToString());
        f.V(171,257,At(17).ToString());
        f.V(163,271,At(16).ToString());
        V6(f,53,341,At(10),0);
        V6(f,53,358,At(12),3);
        V6(f,53,377,At(21),1);
        V6(f,169,341,At(11),2);
        V6(f,169,358,At(13),4);
        V6(f,169,377,At(14),6);
        int remaining = At(24) - PendSum;
        var buttons = new (float x, float y, int index)[]
        { (87,341,0), (87,358,3), (87,377,1), (203,341,2), (203,358,4), (203,377,6) };
        foreach (var button in buttons)
        {
            if (Spin(f.L(button.x,button.y,20,19),plusBtn,remaining > 0,ev))
            { pend[button.index]++; remaining--; }
            if (Spin(f.L(button.x + 20,button.y,20,19),minusBtn,pend[button.index] > 0,ev) &&
                pend[button.index] > 0)
            { pend[button.index]--; remaining++; }
        }
        if (Btn(f.L(155,396,90,22),assignBtn,ev)) AutoAssignPointsForIndependentRuntime();
        if (Btn(f.L(155,417,44,22),affirmBtn,ev)) ConfirmPointsForIndependentRuntime();
        if (Btn(f.L(201,417,44,22),restoreBtn,ev)) Array.Clear(pend,0,7);
        f.V(66,442,CharData.honor.ToString());
        f.V(203,442,(At(24) - PendSum).ToString());
        // Re-read the live total and pending array after the label, before animation.
        if (At(24) - PendSum > 0 && pointFire != null)
        {
            Texture2D frame = pointFire[(int)(Time.unscaledTime * 10f) % 8];
            if (frame != null) GUI.DrawTexture(f.L(177,427,30,30),frame);
        }
        f.V(83,459,"0");
        f.V(83,479,CharData.exp.ToString() + "/" + CharData.expToNext.ToString());
    }

    private void V6(GuiFrame f, float x, float y, int baseValue, int index)
    {
        string text = (baseValue + pend[index]).ToString();
        BitmapFont.DrawOutline(f.X + x * s,f.Y + y * s,text,
            pend[index] > 0 ? new Color(1f,230f/255f,120f/255f,1f) : Color.white,s);
    }

    internal void AutoAssignPointsForIndependentRuntime()
    {
        // The first guard uses the total, not the remaining amount. Template lookup happens
        // before the second At/PendSum pair even when all points were already previewed.
        if (At(24) <= 0) return;
        int[] template = AutoTpl[Mathf.Clamp(CharData.race,0,4)];
        int remaining = At(24) - PendSum;
        while (remaining >= 10)
        {
            for (int index = 0; index < 7; index++) pend[index] += template[index];
            remaining -= 10;
        }
        for (int index = 0; remaining > 0; index = (index + 1) % 7)
        {
            if (template[index] > 0) { pend[index]++; remaining--; }
        }
    }

    internal void ConfirmPointsForIndependentRuntime()
    {
        if (PendSum > 0 && NetClient.I != null)
        {
            NetClient client = NetClient.I;
            int[] values = (int[])pend.Clone();
            client.AllocPointsForIndependentRuntime(values);
            Array.Clear(pend,0,7);
        }
    }

    internal int PendingPointForIndependentRuntime(int index) =>
        index >= 0 && index < pend.Length ? pend[index] : 0;
    internal int RemainingPointsForIndependentRuntime() => At(24) - PendSum;
}
}
