using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysMsgBox : MonoBehaviour
{
    public static SysMsgBox I;
    private const float BG_W = 346f;
    private const float BG_H = 128f;
    private const float BTN_W = 70f;
    private const float BTN_H = 22f;
    private const float BTN_MX = 36f;
    private const float BTN_MY = 16f;
    private const float TEXT_TOP = 20f;
    private const float LINE_H = 16f;
    private const float TEXT_GAP = 6f;
    private const float FRAME_CAP = 44f;
    private bool shown;
    private string msg;
    private Action onOk;
    private Action onCancel;
    private Texture2D bg;
    private Texture2D okN;
    private Texture2D okH;
    private Texture2D caN;
    private Texture2D caH;
    private bool triedAssets;

    public static bool Visible => I != null && I.shown;
    public static bool MouseOver() => I != null && I.shown;

    public static void Show(string message, Action ok, Action cancel = null)
    {
        if (I == null) I = new GameObject("SysMsgBox").AddComponent<SysMsgBox>();
        I.msg = message;
        I.onOk = ok;
        I.onCancel = cancel;
        I.shown = true;
    }

    public static void Close()
    {
        if (I != null)
        {
            I.shown = false;
            I.onOk = null;
            I.onCancel = null;
        }
    }

    private void EnsureAssets()
    {
        if (triedAssets) return;
        triedAssets = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) return;
        bg = loader.LoadGsa("res/uires/common/msgboxbk.gsa");
        okN = loader.LoadGsaFrame("res/uires/common/affirmbtn.gsa", 0);
        okH = loader.LoadGsaFrame("res/uires/common/affirmbtn.gsa", 1);
        caN = loader.LoadGsaFrame("res/uires/common/cancelbtn.gsa", 0);
        caH = loader.LoadGsaFrame("res/uires/common/cancelbtn.gsa", 1);
    }

    private void Fire(Action callback)
    {
        Close();
        callback?.Invoke();
    }

    internal string MessageForIndependentRuntime => msg;
    internal void FireAffirmativeForIndependentRuntime() => Fire(onOk);
    internal void FireCancelForIndependentRuntime() => Fire(onCancel);
}

}
