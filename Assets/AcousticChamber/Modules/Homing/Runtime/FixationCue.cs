using UnityEngine;

namespace GPL.Homing
{
    /// <summary>
    /// The fixation dot of the head gate (DesignSystem section 8): a small unlit sphere straight ahead at eye level,
    /// <see cref="diameterDeg"/> of visual angle at <see cref="distanceM"/>. The material must be a URP Unlit material
    /// asset (single-pass stereo); <c>Magic</c> sets it up. Hidden until <see cref="Show"/>.
    /// </summary>
    public class FixationCue : MonoBehaviour
    {
        public float distanceM = 2f;
        [Tooltip("Diameter in degrees of visual angle (DesignSystem: 26 u = 1.5 deg).")]
        public float diameterDeg = 1.5f;
        public Material material;

        Transform _dot;

        /// <summary>Sphere diameter (m) for a visual angle at a distance.</summary>
        public static float DiameterM(float deg, float distanceM) => 2f * distanceM * Mathf.Tan(0.5f * deg * Mathf.Deg2Rad);

        public bool Visible => _dot != null && _dot.gameObject.activeSelf;

        /// <summary>The dot's own GameObject (a child created at run time; null before the first Show). Hiding it leaves this component and its GameObject alone.</summary>
        public GameObject Dot => _dot != null ? _dot.gameObject : null;

        /// <summary>Places the dot straight ahead of the rig's forward at the head's height and shows it.</summary>
        public void Show(OVRCameraRig rig)
        {
            EnsureDot();
            var head = rig.centerEyeAnchor.position;
            var yaw = Quaternion.Euler(0f, rig.transform.eulerAngles.y, 0f);
            _dot.SetPositionAndRotation(head + yaw * Vector3.forward * distanceM, yaw);
            _dot.localScale = Vector3.one * DiameterM(diameterDeg, distanceM);
            _dot.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_dot != null) _dot.gameObject.SetActive(false);
        }

        void EnsureDot()
        {
            if (_dot != null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Fixation Dot";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (material != null) r.sharedMaterial = material;
            else Debug.LogWarning("[GPL] FixationCue: no material (run Magic > App > Homing Test Panel or assign a URP Unlit material).");
            _dot = go.transform;
            _dot.SetParent(transform, true);
            go.SetActive(false);
        }
    }
}
