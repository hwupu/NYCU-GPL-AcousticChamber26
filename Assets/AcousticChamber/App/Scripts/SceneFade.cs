using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// Puts Meta's <see cref="OVRScreenFade"/> on the rig's center eye at run time (the rig is a Building Block and is not
    /// edited in the scene), set to fade in from black over <see cref="AppNav.FadeSec"/> when the scene starts. One per
    /// scene, in the XR group; added by the Hall and chamber setup steps. Uses the rig of its own scene: while a scene loads
    /// asynchronously the previous scene's rig still exists.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class SceneFade : MonoBehaviour
    {
        const string CenterEyePath = "TrackingSpace/CenterEyeAnchor";

        void Awake()
        {
            OVRCameraRig rig = null;
            foreach (var r in FindObjectsByType<OVRCameraRig>())
                if (r.gameObject.scene == gameObject.scene) rig = r;
            if (rig == null)
            {
                Debug.LogWarning("[GPL] SceneFade: no OVRCameraRig in this scene; no fade.");
                return;
            }
            // the rig may not have run Awake yet: find the anchor by path instead of creating it
            var eye = rig.centerEyeAnchor != null ? rig.centerEyeAnchor : rig.transform.Find(CenterEyePath);
            if (eye == null)
            {
                Debug.LogWarning("[GPL] SceneFade: no " + CenterEyePath + " under the rig; no fade.");
                return;
            }
            if (eye.GetComponent<OVRScreenFade>() != null) return;
            var fade = eye.gameObject.AddComponent<OVRScreenFade>();
            fade.fadeTime = AppNav.FadeSec;
            fade.fadeOnStart = true;
        }
    }
}
