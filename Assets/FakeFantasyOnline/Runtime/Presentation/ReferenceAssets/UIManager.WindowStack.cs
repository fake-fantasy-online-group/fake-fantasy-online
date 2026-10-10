using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official UIManager (type 0x02000281) window stack. Every entry is a world-space sprite window
// built from the Resources pair "UI/win/<name>" (a Texture2D background and a TextAsset layout)
// and parented under the main camera. The bodies below are the reviewed 1.5.8 owners
// 0x06000f9f..0x06000fab; only GameHud.OnGUI (0x06000414, arm64 0x431d30) calls Toggle, as the
// fallback of its GameHud.CtrlBtns name dispatch.
public partial class UIManager
{
    // UIManager+Win (0x02000282): five public fields, no field initializers (0x06000fac only runs
    // System.Object..ctor).
    private class Win
    {
        public string name;
        public Transform root;
        public SpriteRenderer chrome;
        public UIWinMeta meta;
        public int order;
    }

    public static UIManager I;

    // 0x06000fab .cctor: eight Adds in this exact order, then the dictionary is stored in To05.
    private static readonly Dictionary<string, string> To05 = new Dictionary<string, string>
    {
        { "仓库", "storedlg" },
        { "合成", "composedlg" },
        { "修理", "repairdlg" },
        { "人物", "figuredlg" },
        { "杂货", "grocerystatusdlg" },
        { "交易", "tradedlg" },
        { "任务", "taskdlg" },
        { "邮件", "maillistdlg" }
    };

    private Camera cam;

    // 0x06000faa .ctor: stack is allocated first, then baseOrder is set to 25000.
    private readonly List<Win> stack = new List<Win>();
    private int baseOrder = 25000;

    private void Awake()
    {
        I = this;
        cam = Camera.main;
    }

    public void Toggle(string name)
    {
        string tab;
        if (To05.TryGetValue(name, out tab))
        {
            Win05.Toggle(tab);
            return;
        }

        if (!IsOpen(name))
        {
            Open(name);
            return;
        }

        Close(name);
    }

    public bool IsOpen(string name)
    {
        return stack.Exists(w => w.name == name);
    }

    public void Open(string name)
    {
        if (IsOpen(name))
        {
            Bring(name);
            return;
        }

        Texture2D texture = Resources.Load<Texture2D>("UI/win/" + name);
        TextAsset layout = Resources.Load<TextAsset>("UI/win/" + name);
        if (texture == null) return;
        if (layout == null) return;

        UIWinMeta meta = JsonUtility.FromJson<UIWinMeta>(layout.text);
        GameObject window = new GameObject("Win_" + name);
        Transform root = window.transform;
        if (cam != null)
        {
            root.SetParent(cam.transform, false);
        }

        int order = baseOrder + stack.Count * 100;
        GameObject chromeObject = new GameObject("chrome");
        chromeObject.transform.SetParent(root, false);
        SpriteRenderer chrome = chromeObject.AddComponent<SpriteRenderer>();
        chrome.sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        chrome.sortingOrder = order;

        Win win = new Win
        {
            name = name,
            root = root,
            chrome = chrome,
            meta = meta,
            order = order
        };
        stack.Add(win);
        Layout(win);
    }

    public void Close(string name)
    {
        Win win = stack.Find(w => w.name == name);
        if (win != null)
        {
            Object.Destroy(win.root.gameObject);
        }

        stack.RemoveAll(w => w.name == name);
    }

    public void CloseTop()
    {
        if (stack.Count < 1) return;

        Win win = stack[stack.Count - 1];
        Object.Destroy(win.root.gameObject);
        stack.RemoveAt(stack.Count - 1);
    }

    private void Bring(string name)
    {
        Win win = stack.Find(w => w.name == name);
        if (win == null) return;

        stack.Remove(win);
        stack.Add(win);
        win.order = baseOrder + stack.Count * 100 - 50;
        win.chrome.sortingOrder = win.order;
    }

    private void Layout(Win win)
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        float byWidth = 2f * cam.orthographicSize * cam.aspect * 0.92f / (win.meta.w / 100f);
        float byHeight = 2f * cam.orthographicSize * 0.62f / (win.meta.h / 100f);
        float scale = Mathf.Min(byWidth, byHeight);
        win.root.localScale = new Vector3(scale, scale, 1f);
        win.root.localPosition = new Vector3(0f, 0f, 1f);
    }

    private UIWinWidget HitWidget(Win win, float px, float py)
    {
        UIWinWidget[] widgets = win.meta.widgets;
        if (widgets == null || widgets.Length < 1) return null;

        UIWinWidget hit = null;
        long smallest = long.MaxValue;
        for (int i = 0; i < widgets.Length; i++)
        {
            UIWinWidget widget = widgets[i];
            int[] rect = widget.rect;
            if (rect == null || rect.Length <= 3) continue;
            if (rect[0] > px || px > rect[0] + rect[2]) continue;
            if (rect[1] > py || py > rect[1] + rect[3]) continue;

            long area = (long)rect[3] * rect[2];
            if (area < smallest)
            {
                hit = widget;
                smallest = area;
            }
        }

        return hit;
    }
}

}
