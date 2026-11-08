using System;
using GPL.Input;
using GPL.SpatialSources;
using UnityEngine;

namespace AcousticChamber.Chambers.MovingSource
{
    /// <summary>
    /// The source the visitor grabs in 3D (Docs/Design.md "Movement"): point a controller's ray at it and press that hand's grip; while the
    /// grip is held the source stays fixed to the controller (it keeps its offset in the controller's frame, so a hand turn swings it);
    /// releasing leaves it where it is (no throw physics). Kept 0.5-10 m from the head and above the floor (<see cref="SourceLimits"/>).
    /// The grip comes through the scene's <see cref="ActionReader"/> (GripLeft / GripRight), the pose from the rig's controller anchors.
    /// </summary>
    public class GrabSource : MonoBehaviour
    {
        public float hitRadiusM = 0.3f;
        public Renderer[] tinted = new Renderer[0];
        public Color normalColor = new Color(0.82f, 0.82f, 0.86f);
        public Color hoverColor = Color.white;
        public Color heldColor = new Color(1f, 0.78f, 0.35f);

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        OVRCameraRig _rig;
        GlobalActions _actions;
        MaterialPropertyBlock _block;
        Transform _hand;
        bool _handIsLeft;
        Vector3 _offset;
        bool _hover;

        public bool Held { get; private set; }

        /// <summary>Raised when a grip grabs / releases the source.</summary>
        public event Action Grabbed, Released;

        public void Init(OVRCameraRig rig, GlobalActions actions)
        {
            _rig = rig;
            _actions = actions;
            ApplyTint();
        }

        void OnDisable()
        {
            if (Held) Release();
        }

        void Update()
        {
            if (_rig == null || _actions == null) return;
            var reader = _actions.Reader;
            if (_actions.Blocked)
            {
                if (Held) Release();
                return;
            }
            if (Held)
            {
                if (!reader.Held(_handIsLeft ? InputActions.GripLeft : InputActions.GripRight)) { Release(); return; }
                Vector3 p = _hand.TransformPoint(_offset);
                Vector3 head = _rig.centerEyeAnchor.position;
                double x = p.x, y = p.y, z = p.z;
                SourceLimits.Clamp(ref x, ref y, ref z, head.x, head.y, head.z);
                transform.position = new Vector3((float)x, (float)y, (float)z);
                return;
            }

            bool left = Aims(_rig.leftControllerAnchor), right = Aims(_rig.rightControllerAnchor);
            if ((left || right) != _hover) SetHover(left || right);
            if (left && reader.Down(InputActions.GripLeft)) Grab(_rig.leftControllerAnchor, true);
            else if (right && reader.Down(InputActions.GripRight)) Grab(_rig.rightControllerAnchor, false);
        }

        bool Aims(Transform hand)
        {
            if (hand == null) return false;
            Vector3 c = transform.position;
            return FloorDrag.RaySphere(hand.position.x, hand.position.y, hand.position.z, hand.forward.x, hand.forward.y, hand.forward.z,
                                       c.x, c.y, c.z, hitRadiusM, SourceLimits.MaxDistanceM + 4.0, out _);
        }

        void Grab(Transform hand, bool left)
        {
            _hand = hand;
            _handIsLeft = left;
            _offset = hand.InverseTransformPoint(transform.position);
            Held = true;
            ApplyTint();
            Grabbed?.Invoke();
        }

        void Release()
        {
            Held = false;
            _hand = null;
            ApplyTint();
            Released?.Invoke();
        }

        void SetHover(bool on)
        {
            _hover = on;
            ApplyTint();
        }

        void ApplyTint()
        {
            if (tinted == null || tinted.Length == 0) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            Color c = Held ? heldColor : _hover ? hoverColor : normalColor;
            foreach (var r in tinted)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, c);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
