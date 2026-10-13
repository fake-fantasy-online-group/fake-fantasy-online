// Minimal substitutes for actual SocketUI/SculpDlg state and native furnace writers.
using System.Collections.Generic;
namespace UnityEngine
{
    public class MonoBehaviour { protected static void DontDestroyOnLoad(object value) { } }
    public sealed class GameObject { public GameObject(string name) { } public T AddComponent<T>() where T : new() => new T(); }
    public class Texture2D { }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x=x; this.y=y; } }
    public struct Rect { public Rect(float x, float y, float width, float height) { } public bool Contains(Vector2 point) => false; }
    public static class Input { public static Vector2 mousePosition; }
    public static class Screen { public static int width=1024, height=768; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal partial class NetClient { public static NetClient I; }
    internal static class UIScale { public static float Factor => 1f; }
    internal sealed class ResLoader { public static ResLoader I; public UnityEngine.Texture2D LoadGsa(string path) => null; }
    internal static class CraftUpgradeUI { public struct Mat { public int have, need; public string name; } }
    internal static class InventoryDlg
    {
        public struct Item { public int id, bagIdx; public string name; public UnityEngine.Texture2D icon; }
        public static readonly List<Item>[] Items = { new List<Item>(), new List<Item>(), new List<Item>(), new List<Item>() };
    }
}
