using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class NpcChatter
{
    // Keep BeforeFieldInit: the official 06000921 body is emitted from these
    // ordered initializers, not an explicit C# static constructor.
    private static float sMinGap = 40f;
    private static float sMaxGap = 90f;
    private static float sShowSec = 5f;
    private static bool sEnabled = true;
    private static readonly HashSet<string> allNpcNames = new HashSet<string>();
    private static readonly Dictionary<string, Texture2D[]> bubbleCache = new Dictionary<string, Texture2D[]>();
    private static readonly Dictionary<int, Texture2D> symCache = new Dictionary<int, Texture2D>();
}

}
