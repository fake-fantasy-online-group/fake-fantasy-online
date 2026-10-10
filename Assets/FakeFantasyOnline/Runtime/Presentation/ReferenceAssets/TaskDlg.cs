using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 task window (taskdlg) state and lifecycle.
/// </summary>
/// <remarks>
/// The official drawing body lives in a companion partial file and runs through Unity's automatic
/// OnGUI callback on the default-enabled, persistent component created by Ensure.
/// </remarks>
public partial class TaskDlg : MonoBehaviour
{
    // Official static +0x00. The singleton is created lazily and never destroyed.
    public static TaskDlg I;

    // Official static +0x08 / +0x18 / +0x28, read back from the baked constant blobs the official
    // .cctor (06000e90) copies. Declared as initializers here for the same reason.
    public static readonly Color GOLD = new Color(1f, 0.92f, 0.66f, 1f);
    public static readonly Color GRAY = new Color(0.62f, 0.6f, 0.52f, 1f);
    public static readonly Color WHITE = new Color(0.95f, 0.95f, 0.92f, 1f);

    // Official instance layout. findText/winX/_wdGen are the three fields the official .ctor
    // (06000e8f) initialises; every other field is left at its default there.
    private bool visible;
    private int tab;
    private int selId;
    private bool selIsRec;
    private bool selIsDone;
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private string findText = "";
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoCancel;
    private Texture2D icoFind;
    private Texture2D icoStop;
    private Texture2D tabDoing;
    private Texture2D tabWorld;
    private Texture2D tabTale;
    private bool tried;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;
    private GUIStyle _edit;

    // Official 1.5.8 token 06000e7e.
    public static void Ensure()
    {
        if (I == null)
        {
            GameObject go = new GameObject("TaskDlg");
            I = go.AddComponent<TaskDlg>();
            DontDestroyOnLoad(go);
        }
    }

    // Official 1.5.8 token 06000e7f. The official body flips the visible byte with an XOR 1, which
    // is the same toggle the negation expresses for the 0/1 bool the field always holds.
    public static void Toggle()
    {
        Ensure();
        I.visible = !I.visible;
    }

    // Official 1.5.8 token 06000e80.
    public static void Hide()
    {
        if (I != null) I.visible = false;
    }

    // Official 1.5.8 token 06000e81. The liveness test and the field read are two separate null
    // checks in the official body, which is what the short-circuit form emits.
    public static bool Visible => I != null && I.visible;

    // Actual 1.6.0 token 06000e8e. Both centering decisions test winX; the non-centred
    // vertical position still reads winY. Keep the two sentinel reads separate.
    public static bool MouseOver()
    {
        if (I == null) return false;
        if (!I.visible) return false;

        float scale = UIScale.Factor;
        float left = float.IsNaN(I.winX) ? (Screen.width - scale * 495f) * 0.5f : I.winX;
        float top = float.IsNaN(I.winX) ? (Screen.height - scale * 397f) * 0.5f : I.winY;
        Vector2 mp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return new Rect(left, top, scale * 495f, scale * 397f).Contains(mp);
    }

    // Official 1.5.8 token 06000e83. tried is raised before the loader is consulted and lowered
    // again when the loader is missing, so a failed load stays retryable.
    public void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null)
        {
            tried = false;
            return;
        }

        bg = loader.LoadGsa2021First("res/uires/taskdlg/taskbk.gsa");
        icoClose = loader.LoadGsa2021First("res/uires/common/miniclose.gsa");
        icoCancel = loader.LoadGsa2021First("res/uires/common/closebtn.gsa");
        icoFind = loader.LoadGsa2021First("res/uires/common/findbtn.gsa");
        icoStop = loader.LoadGsa2021First("res/uires/common/stopbtn.gsa");
        tabDoing = loader.LoadGsa2021First("res/uires/taskdlg/doingtab.gsa");
        tabWorld = loader.LoadGsa2021First("res/uires/taskdlg/worldtab.gsa");
        tabTale = loader.LoadGsa2021First("res/uires/taskdlg/taletab.gsa");
    }

    // Official 1.5.8 token 06000e8a. The objectives predicate is the same terminal test the task
    // client owns, plus the ready flag; the def parameter is never read by the official body.
    public static bool ObjectivesMet(TaskClient.ActiveTask a, TaskDB.Def def)
    {
        if (a == null) return false;
        if (a.total < 1 || a.ready != 1) return false;
        return a.total - 1 <= a.step;
    }

    // Official 1.5.8 token 06000e8b. A live task with no database row still gets a synthetic
    // definition so the list can render it; 90001 keeps its own name instead of "任务" + id.
    public static TaskDB.Def DisplayDef(int id)
    {
        TaskDB.Def def = TaskDB.Get(id);
        if (def != null) return def;
        if (!TaskClient.IsActive(id)) return null;
        return new TaskDB.Def
        {
            id = id,
            type = 1,
            name = id == 90001 ? "妖魔结界试炼" : "任务" + id.ToString(),
            desc = "",
            publisher = "",
            place = "",
            award = ""
        };
    }
}

}
