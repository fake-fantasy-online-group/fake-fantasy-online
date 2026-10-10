using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
    // Original PetBrief value. Protocol and native owners share its skill-list reference.
    public struct PetBrief
    {
        public int model;
        public int slot;
        public string nickname;
        public string prename;
        public string species;
        public byte gender;
        public byte habit;
        public byte opened;
        public int level;
        public int str;
        public int intel;
        public int vit;
        public int agi;
        public int dex;
        public int spi;
        public int starve;
        public int trust;
        public int freePts;
        public int freeUsed;
        public List<(string, int)> skills;
        public bool bound;
        public bool locked;
        public int ppAiUsed;
        public int ppAiCap;
        public int ppShUsed;
        public int ppShCap;
        public int growTier;
    }
}
