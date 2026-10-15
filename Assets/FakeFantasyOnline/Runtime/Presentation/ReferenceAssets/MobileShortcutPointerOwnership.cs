using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    // Physical pointer ownership is separate from the currently executing action:
    // cancelling an aim must not turn a still-held skill finger into a chat press.
    internal sealed class MobileShortcutPointerOwnership
    {
        private readonly HashSet<int> pointers = new HashSet<int>();
        private readonly List<int> released = new List<int>();
        private int releaseFrame = -1;
        internal void Claim(int pointer) { pointers.Add(pointer); }
        internal bool Owns(int frame) => pointers.Count > 0 || releaseFrame == frame;
        internal void Refresh(int frame, Func<int, bool> isActive)
        {
            released.Clear();
            foreach (int pointer in pointers)
                if (!isActive(pointer)) released.Add(pointer);
            foreach (int pointer in released)
            {
                pointers.Remove(pointer);
                releaseFrame = frame;
            }
        }
        internal void Clear(int frame) { pointers.Clear(); releaseFrame = frame; }
    }
}
