using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static partial class SocialClient
    {
        public static List<byte> PartyProfs = new List<byte>();
        private static readonly string[] ProfNames = { "战士", "剑客", "刺客", "药师", "术士", "符咒" };

        // Official 0x06000d24. Missing entries and unknown profession ids have no label.
        public static string PartyProfName(int index)
        {
            if (index < 0 || index >= PartyProfs.Count) return "";
            if (PartyProfs[index] >= ProfNames.Length) return "";
            return ProfNames[PartyProfs[index]];
        }
    }
}
