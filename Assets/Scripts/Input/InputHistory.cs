using System;

namespace Babodayo.Input
{
    [Flags]
    public enum InputButtons { None = 0, LMB = 1, RMB = 2, Jump = 4, Shift = 8 }

    public readonly struct InputFrame
    {
        public readonly int Frame, Horizontal, Vertical;
        public readonly InputButtons Pressed, Held, Released;
        // Snapshot of the primary button chord, even if its modifier is released before Capture.
        public readonly int LmbHorizontal, LmbVertical;
        public readonly bool LmbShiftHeld;
        public readonly UnityEngine.Vector2 PointerPosition;
        public readonly bool HasPointer;
        public InputFrame(int frame, int horizontal, int vertical,
            InputButtons pressed, InputButtons held, InputButtons released,
            int? lmbHorizontal = null, int? lmbVertical = null, bool? lmbShiftHeld = null,
            UnityEngine.Vector2 pointerPosition = default, bool hasPointer = false)
        {
            PointerPosition = pointerPosition; HasPointer = hasPointer;
            Frame = frame; Horizontal = horizontal; Vertical = vertical;
            Pressed = pressed; Held = held; Released = released;
            LmbHorizontal = lmbHorizontal ?? horizontal;
            LmbVertical = lmbVertical ?? vertical;
            LmbShiftHeld = lmbShiftHeld ?? ((held & InputButtons.Shift) != 0);
        }
        public bool WasPressed(InputButtons button) => (Pressed & button) != 0;
        public bool IsHeld(InputButtons button) => (Held & button) != 0;
    }

    public sealed class InputHistory
    {
        private readonly InputFrame[] frames;
        private int next;
        public int Count { get; private set; }
        public int Capacity => frames.Length;
        public InputHistory(int capacity = 30)
        {
            if (capacity < 2) throw new ArgumentOutOfRangeException(nameof(capacity));
            frames = new InputFrame[capacity];
        }
        public void Add(InputFrame frame)
        {
            if (TryGetRecent(0, out var latest) && frame.Frame <= latest.Frame)
                throw new ArgumentException("Input frames must increase monotonically.");
            frames[next] = frame;
            next = (next + 1) % Capacity;
            Count = Math.Min(Count + 1, Capacity);
        }
        public bool TryGetRecent(int age, out InputFrame frame)
        {
            frame = default;
            if (age < 0 || age >= Count) return false;
            frame = frames[(next - 1 - age + Capacity) % Capacity];
            return true;
        }
        public void Clear() { next = 0; Count = 0; }
    }
}
