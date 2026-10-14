using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public enum ClientTouchPhase { Began, Moved, Stationary, Ended, Cancelled }
    public struct ClientPointerFrame
    {
        public Vector2 Position;
        public bool Available, Down, Held, Up, Cancelled;
    }

    // Ownership is chosen once at Began. A UI finger never becomes a world finger,
    // and a second finger cannot steal either the stick or the world pointer.
    public sealed class TouchPointerRouter
    {
        private enum Owner { Ui, Stick, World }
        private readonly Dictionary<int, Owner> owners = new Dictionary<int, Owner>();
        private int? stick, world;
        private readonly HashSet<int> seen = new HashSet<int>();
        private readonly List<int> missing = new List<int>();
        public ClientPointerFrame Pointer { get; private set; }
        public Vector2 MovementAxis { get; private set; }
        public bool JoystickHeld => stick.HasValue;

        public void BeginFrame()
        {
            seen.Clear();
            ClientPointerFrame p = Pointer;
            p.Down = p.Up = p.Cancelled = false;
            p.Available = p.Held;
            Pointer = p;
        }

        public void Feed(int id, Vector2 position, ClientTouchPhase phase, bool overUi,
            bool overStick, Vector2 center, float radius)
        {
            seen.Add(id);
            if (phase == ClientTouchPhase.Began)
            {
                if (owners.ContainsKey(id)) return;
                Owner owner = Owner.Ui;
                if (overStick && !stick.HasValue) { owner = Owner.Stick; stick = id; }
                else if (!overUi && !overStick && !world.HasValue) { owner = Owner.World; world = id; }
                owners[id] = owner;
            }
            Owner current;
            if (!owners.TryGetValue(id, out current)) return; // held across reset: await a fresh Began
            bool ended = phase == ClientTouchPhase.Ended || phase == ClientTouchPhase.Cancelled;
            if (current == Owner.Stick)
            {
                Vector2 axis = radius > 0f ? (position - center) / radius : Vector2.zero;
                MovementAxis = ended ? Vector2.zero : Vector2.ClampMagnitude(axis, 1f);
                if (ended) stick = null;
            }
            else if (current == Owner.World)
            {
                Pointer = new ClientPointerFrame { Position = position, Available = true,
                    Down = phase == ClientTouchPhase.Began, Held = !ended,
                    Up = phase == ClientTouchPhase.Ended, Cancelled = phase == ClientTouchPhase.Cancelled };
                if (ended) world = null;
            }
            if (ended) owners.Remove(id);
        }

        public void EndFrame()
        {
            missing.Clear();
            foreach (int id in owners.Keys) if (!seen.Contains(id)) missing.Add(id);
            foreach (int id in missing)
                Feed(id, Pointer.Position, ClientTouchPhase.Cancelled, true, false, Vector2.zero, 1f);
        }

        public void CancelAll()
        {
            owners.Clear(); stick = world = null; MovementAxis = Vector2.zero;
            Pointer = new ClientPointerFrame { Position = Pointer.Position, Cancelled = true };
        }
    }
}
