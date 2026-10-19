using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    // Official 06000ee9, in the same existing carrier as the title panel/button styles.
    // DrawLogin calls this after its disclaimer, before the notice button.
    // The version coroutine still has no activation supplied by this title consumer.
    private void DrawUpdateGate()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0f, 0f, 800f, 600f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Box(new Rect(190f, 210f, 420f, 190f), GUIContent.none,
            _panelStyle ?? GUI.skin.box);

        GUIStyle heading = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15,
            wordWrap = true,
            clipping = TextClipping.Overflow
        };
        heading.normal.textColor = new Color(1f, 0.9f, 0.6f, 1f);
        GUI.Label(new Rect(190f, 226f, 420f, 26f), "发现新版本", heading);

        GUIStyle detail = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            wordWrap = true,
            clipping = TextClipping.Overflow
        };
        detail.normal.textColor = Color.white;
        GUI.Label(new Rect(210f, 260f, 380f, 70f),
            "当前客户端 v" + RecoveredTitleLoginSurface.ClientVer + "。请重启游戏，返回统一更新页完成更新。", detail);

        // Share the original four-argument RegBtn and its null-style fallback.
        // Evaluate both buttons independently, even if the first requests application quit.
        if (RegBtn(238f, 352f, 152f, "退出后手动重启"))
            LaunchUpdaterAndQuit();
        if (RegBtn(410f, 352f, 152f, "退出游戏"))
            GameBootstrap.QuitGame();
    }

    // Official 06000eea in both frozen Mac slices is only this call. No Windows
    // updater command/path or additional platform branch is inferred from that input.
    private void LaunchUpdaterAndQuit()
    {
        GameBootstrap.QuitGame();
    }
}
}
