using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace FakeFantasyOnline.Editor
{
/// <summary>Restore local icon references after their original assets are reimported.</summary>
internal sealed class BuildIconSnapshot
{
    private readonly NamedBuildTarget target;
    private readonly Texture2D[] icons;
    private readonly string[] paths;

    public BuildIconSnapshot(NamedBuildTarget target)
    {
        this.target = target;
        icons = PlayerSettings.GetIcons(target, IconKind.Any);
        paths = new string[icons.Length];
        for (int index = 0; index < icons.Length; index++)
            paths[index] = icons[index] == null ? "" : AssetDatabase.GetAssetPath(icons[index]);
    }

    public void Restore()
    {
        var restored = new Texture2D[icons.Length];
        for (int index = 0; index < restored.Length; index++)
            restored[index] = string.IsNullOrEmpty(paths[index]) ? icons[index] : AssetDatabase.LoadAssetAtPath<Texture2D>(paths[index]);
        PlayerSettings.SetIcons(target, restored, IconKind.Any);
    }
}
}
