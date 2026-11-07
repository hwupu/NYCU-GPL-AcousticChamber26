using GPL.Acoustics;
using UnityEngine;

namespace GPL.SpatialSources
{
    /// <summary>
    /// A spatial sound on a circle around an origin (usually the head position at trial start, yaw = forward). <see cref="Place"/>
    /// sets the circle; <see cref="SetAzimuth"/> moves the sound on it with the radius locked (HW2 adjustment task).
    /// <see cref="Anchor"/> decides what a recenter does: <see cref="Anchoring.Replaced"/> rebuilds the circle around the new
    /// origin at the same azimuth (the chamber calls <see cref="Replace"/>), <see cref="Anchoring.RoomFixed"/> stays where it is.
    /// The AudioSource is set to unity gain; spatialization and acoustics stay as the Meta Spatial Audio block sets them.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PlacedSource : MonoBehaviour
    {
        public enum Anchoring { Replaced, RoomFixed }

        public Anchoring Anchor = Anchoring.Replaced;

        public AudioSource Source { get; private set; }
        public Vector3 Origin { get; private set; }
        public Quaternion Yaw { get; private set; } = Quaternion.identity;
        public float Radius { get; private set; } = 1f;
        public float AzimuthDeg { get; private set; }

        MetaXRAudioSource _meta;

        void Awake()
        {
            Source = GetComponent<AudioSource>();
            _meta = GetComponent<MetaXRAudioSource>();
            UnityGain.Apply(Source, loop: false);
        }

        /// <summary>Meta gain boost set on the source's MetaXRAudioSource (0 without one).</summary>
        public float GainBoostDb => _meta != null ? _meta.GainBoostDb : 0f;

        /// <summary>Software level of the clip: RMS + gain boost (dBFS). Needs Decompress On Load.</summary>
        public float LevelDbfs => ClipLevels.SourceDbfs(Source != null ? Source.clip : null, 1f, GainBoostDb);

        public void SetAcoustics(bool on)
        {
            if (_meta != null) _meta.EnableAcoustics = on;
        }

        public void Place(Vector3 origin, Quaternion yaw, float radius, float azDeg)
        {
            Origin = origin;
            Yaw = yaw;
            Radius = radius;
            SetAzimuth(azDeg);
        }

        public void SetAzimuth(float azDeg)
        {
            AzimuthDeg = azDeg;
            transform.position = Ring.Position(Origin, Yaw, Radius, azDeg);
        }

        /// <summary>After a recenter: same azimuth and radius around the new origin (no effect when room-fixed).</summary>
        public void Replace(Vector3 origin, Quaternion yaw)
        {
            if (Anchor == Anchoring.RoomFixed) return;
            Place(origin, yaw, Radius, AzimuthDeg);
        }
    }
}
