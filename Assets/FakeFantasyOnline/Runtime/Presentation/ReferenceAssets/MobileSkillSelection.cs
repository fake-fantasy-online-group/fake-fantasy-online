namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    // Touch selection is separate from learning, casting and committing a hotbar slot.
    internal sealed class MobileSkillSelection
    {
        internal int Skill { get; private set; }
        internal bool ChoosingSlot { get; private set; }
        private bool pressed, moved;
        internal bool Pressed => pressed;
        internal bool Moved => moved;
        internal void ConsumeHold() { pressed = false; }
        private float startX, startY, lastY;
        internal void Begin(float x, float y) { pressed = true; moved = false; startX = x; startY = lastY = y; }
        internal float Move(float x, float y, float threshold)
        {
            if (!pressed) return 0;
            if ((x-startX)*(x-startX)+(y-startY)*(y-startY)>threshold*threshold) moved=true;
            float delta = moved ? lastY-y : 0;
            lastY=y;
            return delta;
        }
        internal bool End(float x, float y, float threshold, bool canceled)
        {
            if (!pressed) return false;
            Move(x,y,threshold);
            bool tap=!moved && !canceled;
            pressed=false;
            return tap;
        }
        internal void Select(int skill) { Skill=skill; ChoosingSlot=false; }
        internal bool Choose(bool usable) { ChoosingSlot=Skill>0 && usable; return ChoosingSlot; }
        internal bool Commit(int slot, bool usable, System.Action<int,int> bind)
        {
            if (!ChoosingSlot || !usable || slot<0 || slot>=24 || bind==null) return false;
            ChoosingSlot=false;
            bind(slot,Skill);
            return true;
        }
        internal void CancelSlots() { ChoosingSlot=false; }
        internal void Reset() { Skill=0; ChoosingSlot=false; pressed=moved=false; }
    }
}
