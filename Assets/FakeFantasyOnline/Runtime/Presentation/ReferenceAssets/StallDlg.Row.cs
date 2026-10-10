using PetBrief = FakeFantasyOnline.Application.PetBrief;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class StallDlg
{
    public struct Row
    {
        public int itemId, count;
        public long price;
        public byte blocked, quality;
        public string name, desc;
        public Texture2D icon;
        internal PetBrief? pet;
    }
}
}
