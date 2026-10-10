using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Native initialization dependency of ZoneOverlay reception. Roster data/UI recovery is separate.
public partial class ArenaRosterDlg : MonoBehaviour
{
    public class Card
    {
        public int skillId;
        public int lv;
        public string name = "";
    }

    public class Row
    {
        public int entityId;
        public string name = "";
        public bool alive;
        public int deaths;
        public List<Card> cards = new List<Card>();
    }

    public static ArenaRosterDlg I;
    private static List<Row> _rows = new List<Row>();
    private static Dictionary<int, string> _descs = new Dictionary<int, string>();

    // Official 06000078; the static initializers above carry 06000085.
    public static void Ensure()
    {
        if (I != null) return;
        var owner = new GameObject("ArenaRosterDlg");
        I = owner.AddComponent<ArenaRosterDlg>();
        DontDestroyOnLoad(owner);
    }
}
}
