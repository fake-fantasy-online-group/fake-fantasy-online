namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    private static bool _quitOnPurpose;

    public static void QuitGame()
    {
        _quitOnPurpose = true;
        UnityEngine.Application.Quit();
    }

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void HookQuitGuard()
    {
        UnityEngine.Application.wantsToQuit += () =>
        {
            if (_quitOnPurpose)
                return true;
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftAlt) ||
                UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightAlt))
                return false;
            return true;
        };
    }
}
}
