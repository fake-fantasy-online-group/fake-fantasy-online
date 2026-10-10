using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class SecCodeAskDlg : MonoBehaviour
{
    public static SecCodeAskDlg I;
    private bool visible;
    private string code = "";
    private string what = "";
    private string tip = "";
    private Action<string> onOk;
    private GUIStyle _e;
    private List<string> tipLines;
    private float tipWrapScale;
    private const float NW = 262f;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;
    private float ExtraTipH => Mathf.Max(0, (tipLines?.Count ?? 0) - 1) * 13f;
    private float NH => 118f + ExtraTipH;

    public static void Open(string itemName, string tip, Action<string> cb)
    {
        if (I == null)
        {
            var go = new GameObject("SecCodeAskDlg");
            I = go.AddComponent<SecCodeAskDlg>();
            DontDestroyOnLoad(go);
        }
        I.what = itemName ?? "";
        I.tip = tip ?? "";
        I.code = "";
        I.onOk = cb;
        I.visible = true;
        I.RewrapTip();
    }

    private void RewrapTip()
    {
        tipWrapScale = Mathf.Max(0.01f, UIScale.Factor);
        tipLines = string.IsNullOrEmpty(tip) ? null : BitmapFont.Wrap(tip, 246f * tipWrapScale, tipWrapScale);
    }

    public static void CloseNow()
    {
        if (I != null)
        {
            I.visible = false;
            I.code = "";
            I.onOk = null;
        }
    }

    public static bool Visible => I != null && I.visible;
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        float height = I.NH;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        // Official 0x06000020 tests winX (+0x60) for BOTH axes: arm64 0x211368 and x86_64 0x21d347
        // read winX again for the y fallback, and only the non-NaN path reads winY (+0x64)
        // (arm64 0x2113b0 / x86_64 0x21d38a). winY has no NaN initializer in .ctor (0x06000026),
        // so this is official behavior, not a typo: keep the winX test.
        float y = float.IsNaN(I.winX) ? (Screen.height - height * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, height * scale).Contains(new Vector2(mx, my));
    }

    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);

    private void Confirm()
    {
        string value = (code ?? "").Trim();
        if (value.Length == 0) return;
        Action<string> callback = onOk;
        visible = false;
        code = "";
        onOk = null;
        callback?.Invoke(value);
    }
}
}
