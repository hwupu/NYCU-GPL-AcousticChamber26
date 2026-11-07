using UnityEngine;

namespace GPL.SpatialSources
{
    /// <summary>
    /// A visible marker (e.g. a small unlit sphere, URP Unlit material) on a sound source, for Debug: shows where a sound is or
    /// where a response ended. Follows <see cref="target"/> while shown; <see cref="ShowAt"/> pins it to a point. Save it inactive
    /// in the scene (the setup step does); it does not hide itself, because Awake would run again on the first Show.
    /// </summary>
    public class SourceMarker : MonoBehaviour
    {
        public Transform target;
        bool _follow;

        public void Show(bool on)
        {
            _follow = on && target != null;
            if (_follow) transform.position = target.position;
            gameObject.SetActive(on);
        }

        public void ShowAt(Vector3 position)
        {
            _follow = false;
            transform.position = position;
            gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            if (_follow && target != null) transform.position = target.position;
        }
    }
}
