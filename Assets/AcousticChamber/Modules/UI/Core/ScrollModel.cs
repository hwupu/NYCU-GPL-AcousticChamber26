using System;

namespace GPL.UI
{
    /// <summary>
    /// Scroll state of a borderless list (DesignSystem section 6), Unity-free. Units are canvas units; positions are the
    /// pointer's height on the list (up = positive). <see cref="Offset"/> = how far the content has moved up
    /// (0 = top of the content visible, <see cref="Max"/> = bottom visible).
    /// <list type="bullet">
    /// <item>Drag: a press becomes a drag after <see cref="DragThreshold"/> units of movement (shorter presses stay clicks);
    /// the content follows the pointer.</item>
    /// <item>Momentum: after a drag the content keeps the release velocity and slows down exponentially
    /// (<see cref="DecaySeconds"/>); it stops at the ends (no rubber band).</item>
    /// <item>Thumbstick: <see cref="Stick"/> scrolls at up to <see cref="StickSpeed"/> u/s.</item>
    /// </list>
    /// </summary>
    public class ScrollModel
    {
        public const float DragThreshold = 12f;
        public const float StickDeadZone = 0.2f;

        public float StickSpeed = 1200f;
        public float DecaySeconds = 0.35f;
        public float MinVelocity = 20f;

        public float Offset { get; private set; }
        public float Max { get; private set; }
        public float Velocity { get; private set; }

        /// <summary>A press is active (between <see cref="PointerDown"/> and <see cref="PointerUp"/>).</summary>
        public bool Pressed { get; private set; }

        /// <summary>The active press has become a drag.</summary>
        public bool Dragging { get; private set; }

        public bool CanScroll => Max > 0f;
        public bool MoreAbove => Offset > 0.5f;
        public bool MoreBelow => Offset < Max - 0.5f;

        float _startPos, _startOffset, _lastPos;

        public void SetExtent(float contentHeight, float viewportHeight)
        {
            Max = Math.Max(0f, contentHeight - viewportHeight);
            Offset = Clamp(Offset);
        }

        public void ScrollTo(float offset)
        {
            Offset = Clamp(offset);
            Velocity = 0f;
        }

        public void ScrollBy(float delta) => ScrollTo(Offset + delta);

        /// <summary>Thumbstick up (y &gt; 0) shows content above. Returns true if the list moved.</summary>
        public bool Stick(float y, float dt)
        {
            if (Math.Abs(y) < StickDeadZone || dt <= 0f) return false;
            float before = Offset;
            ScrollBy(-y * StickSpeed * dt);
            return Offset != before;
        }

        public void PointerDown(float pos)
        {
            Pressed = true;
            Dragging = false;
            Velocity = 0f;
            _startPos = _lastPos = pos;
            _startOffset = Offset;
        }

        /// <summary>Pointer moved to <paramref name="pos"/>. Returns true if the press is (now) a drag.</summary>
        public bool PointerMove(float pos, float dt)
        {
            if (!Pressed) return false;
            if (!Dragging && Math.Abs(pos - _startPos) >= DragThreshold && CanScroll)
            {
                Dragging = true;
                _startPos = pos; // no jump: the content starts following from here
                _startOffset = Offset;
            }
            if (Dragging)
            {
                float before = Offset;
                Offset = Clamp(_startOffset + (pos - _startPos));
                if (dt > 0f)
                {
                    float v = (Offset - before) / dt;
                    Velocity = Velocity * 0.5f + v * 0.5f; // light smoothing of the release velocity
                }
            }
            _lastPos = pos;
            return Dragging;
        }

        /// <summary>Ends the press. Returns true if it was a drag (no click), false if it was a click.</summary>
        public bool PointerUp()
        {
            bool wasDrag = Dragging;
            Pressed = false;
            Dragging = false;
            if (!wasDrag) Velocity = 0f;
            return wasDrag;
        }

        /// <summary>Advances momentum by <paramref name="dt"/> seconds. Returns true if the list moved.</summary>
        public bool Step(float dt)
        {
            if (Pressed || Velocity == 0f || dt <= 0f) return false;
            float before = Offset;
            Offset = Clamp(Offset + Velocity * dt);
            Velocity *= (float)Math.Exp(-dt / DecaySeconds);
            if (Math.Abs(Velocity) < MinVelocity || Offset <= 0f || Offset >= Max) Velocity = 0f;
            return Offset != before;
        }

        float Clamp(float v) => v < 0f ? 0f : v > Max ? Max : v;
    }
}
