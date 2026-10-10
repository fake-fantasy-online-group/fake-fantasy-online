using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class AccountSecDlg : MonoBehaviour
{
    public static AccountSecDlg I;
    public static bool Visible => I != null && I.visible;
    private const float BusyTimeout = 4f;
    private const float NW = 268f;
    private bool visible;
    private int mode;
    private string f1 = "", f2 = "", f3 = "", f4 = "";
    private string status = "";
    private bool statusBad;
    private bool busy;
    private float busySince;
    private GUIStyle _e;
    private List<string> statusLines;
    private float statusWrapScale;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    public static void CloseNow()
    {
        if (I != null)
        {
            I.visible = false;
            I.f4 = "";
            I.f3 = "";
            I.f2 = "";
            I.f1 = "";
        }
    }

    public static void OnResult(int kind, bool ok, string msg)
    {
        if (I == null || !I.visible || kind != I.mode) return;
        I.busy = false;
        if (ok) CloseNow();
        else I.SetStatus(msg, true);
    }

    private void SetStatus(string msg, bool bad)
    {
        status = msg ?? "";
        statusBad = bad;
        RewrapStatus();
    }

    private void RewrapStatus()
    {
        statusWrapScale = Mathf.Max(0.01f, UIScale.Factor);
        statusLines = string.IsNullOrEmpty(status) ? null : BitmapFont.Wrap(status, 252f * statusWrapScale, statusWrapScale);
    }
}
}
