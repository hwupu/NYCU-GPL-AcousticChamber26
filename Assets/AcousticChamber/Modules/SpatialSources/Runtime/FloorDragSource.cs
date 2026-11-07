using System;
using GPL.UI;
using UnityEngine;

namespace GPL.SpatialSources
{
    /// <summary>
    /// A sound object that rests on the floor and is dragged with the controller ray (GPL.UI <see cref="UiPointer"/>, trigger held):
    /// it follows the point where the ray meets the floor, keeps its height (on the floor) and stays between
    /// <see cref="minDistanceM"/> and <see cref="maxDistanceM"/> from <see cref="center"/> (the listener's head, horizontally).
    /// The ray picks it with a sphere (<see cref="hitRadiusM"/> around <see cref="hitCenterHeightM"/>), no physics collider. Hover
    /// and hold tint the <see cref="tinted"/> renderers (URP <c>_BaseColor</c>). It is fixed in the room: a recenter does not move it.
    /// Optionally turns to face the centre. Put the AudioSource on this object or a child.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloorDragSource : MonoBehaviour, IRayTarget
    {
        [Tooltip("Height of the floor (rig at floor level: 0).")]
        public float floorY = 0f;
        public float hitRadiusM = 0.3f;
        [Tooltip("Centre of the pick sphere above the object's origin (m).")]
        public float hitCenterHeightM = 0.25f;
        public float minDistanceM = 0.5f;
        public float maxDistanceM = 6f;
        [Tooltip("The listener (head). Distances are horizontal from it. Null = the world origin.")]
        public Transform center;
        [Tooltip("Turn about the vertical axis to face the centre.")]
        public bool faceCenter = true;
        public Renderer[] tinted = new Renderer[0];
        public Color normalColor = new Color(0.82f, 0.82f, 0.86f);
        public Color hoverColor = Color.white;
        public Color heldColor = new Color(1f, 0.78f, 0.35f);

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock _block;
        Vector3 _grabOffset;

        public bool Hovered { get; private set; }
        public bool Held { get; private set; }

        /// <summary>Raised when the trigger grabs / releases the object (the position at release is final).</summary>
        public event Action Grabbed, Released;

        void OnEnable()
        {
            RayTargets.Register(this);
            ApplyTint();
        }

        void OnDisable()
        {
            RayTargets.Unregister(this);
            Hovered = false;
            if (Held) { Held = false; Released?.Invoke(); }
        }

        void LateUpdate()
        {
            if (!faceCenter) return;
            Vector3 c = CenterPoint();
            Vector3 d = c - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        /// <summary>Puts the object on the floor at <paramref name="p"/> (clamped to the ring around the centre).</summary>
        public void PlaceAt(Vector3 p)
        {
            Vector3 c = CenterPoint();
            double x = p.x, z = p.z;
            FloorDrag.ClampToRing(ref x, ref z, c.x, c.z, minDistanceM, maxDistanceM);
            transform.position = new Vector3((float)x, floorY, (float)z);
            LateUpdate();
        }

        /// <summary>On the floor <paramref name="distanceM"/> straight ahead of <paramref name="head"/> (its yaw only).</summary>
        public void PlaceInFront(Transform head, float distanceM)
        {
            if (head == null) head = center;
            Vector3 origin = head != null ? head.position : Vector3.zero;
            Vector3 fwd = head != null ? head.forward : Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
            PlaceAt(origin + fwd.normalized * distanceM);
        }

        /// <summary>Horizontal distance (m) and azimuth (deg, lab convention relative to the centre's yaw) of the object.</summary>
        public void Polar(out float distanceM, out float azimuthDeg)
        {
            Vector3 c = CenterPoint();
            Vector3 d = transform.position - c;
            d.y = 0f;
            distanceM = d.magnitude;
            float yaw = center != null ? center.eulerAngles.y : 0f;
            Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * d;
            azimuthDeg = (float)GPL.SpatialSources.Polar.AzimuthDeg(local.x, local.z);
        }

        // ---- IRayTarget ---------------------------------------------------------------------------------------------

        public bool RayHit(Ray ray, float maxDistance, out float distance)
        {
            Vector3 s = transform.position + Vector3.up * hitCenterHeightM;
            bool hit = FloorDrag.RaySphere(ray.origin.x, ray.origin.y, ray.origin.z, ray.direction.x, ray.direction.y, ray.direction.z,
                                           s.x, s.y, s.z, hitRadiusM, maxDistance, out double d);
            distance = (float)d;
            return hit;
        }

        public void RayHover(bool on)
        {
            if (this == null) return;
            Hovered = on;
            ApplyTint();
        }

        public void RayPress(Ray ray)
        {
            Held = true;
            _grabOffset = FloorHit(ray, out Vector3 p) ? transform.position - p : Vector3.zero;
            _grabOffset.y = 0f;
            ApplyTint();
            Grabbed?.Invoke();
        }

        public Vector3 RayDrag(Ray ray)
        {
            bool onFloor = FloorHit(ray, out Vector3 p);
            PlaceAt(p + _grabOffset);
            return onFloor ? p : transform.position + Vector3.up * hitCenterHeightM;
        }

        public void RayRelease()
        {
            if (this == null) return;
            Held = false;
            ApplyTint();
            Released?.Invoke();
        }

        // ---- helpers ------------------------------------------------------------------------------------------------

        Vector3 CenterPoint() => center != null ? center.position : Vector3.zero;

        bool FloorHit(Ray ray, out Vector3 p)
        {
            float range = maxDistanceM + 4f;
            bool hit = FloorDrag.FloorPoint(ray.origin.x, ray.origin.y, ray.origin.z, ray.direction.x, ray.direction.y, ray.direction.z,
                                            floorY, range, out double x, out double z);
            p = new Vector3((float)x, floorY, (float)z);
            return hit;
        }

        void ApplyTint()
        {
            if (tinted == null || tinted.Length == 0) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            Color c = Held ? heldColor : Hovered ? hoverColor : normalColor;
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
