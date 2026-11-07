using System.Text;
using GPL.Acoustics;
using GPL.Core;
using UnityEngine;

namespace GPL.SpatialSources
{
    /// <summary>
    /// Several looping recordings around the listener (HW2 Noisy: three clips at -90, 90, 180 deg, 2 m, eye level). Placed once
    /// when it starts and then fixed in the room: turning the head turns the sounds the other way, a recenter does not move them
    /// (positions are re-applied every frame so nothing drags them along). Unity gain; <see cref="LevelDbfs"/> is the power sum.
    /// </summary>
    public class BackgroundBed : MonoBehaviour
    {
        public AudioSource[] sources = new AudioSource[0];
        [Tooltip("Optional: assigned to the sources in Awake (by index).")]
        public AudioClip[] clips = new AudioClip[0];
        [Tooltip("RMS of each clip in dBFS, measured in the Editor (long clips can then stream: no Decompress On Load). " +
                 "Empty or a different count = measured in Awake, which needs Decompress On Load.")]
        public float[] clipLevelsDbfs = new float[0];

        public bool Playing { get; private set; }
        public bool Paused { get; private set; }

        Vector3[] _fixed;
        float _levelDbfs = AudioLevelMath.Silent;

        /// <summary>Combined software level of the clips (dBFS); <see cref="AudioLevelMath.Silent"/> when not playing.</summary>
        public float LevelDbfs => Playing ? _levelDbfs : AudioLevelMath.Silent;

        void Awake()
        {
            bool stored = clipLevelsDbfs != null && clipLevelsDbfs.Length == sources.Length;
            var levels = new float[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                UnityGain.Apply(sources[i], loop: true);
                if (i < clips.Length && clips[i] != null) sources[i].clip = clips[i];
                levels[i] = stored ? clipLevelsDbfs[i] : ClipLevels.RmsDbfs(sources[i].clip);
            }
            _levelDbfs = AudioLevelMath.PowerSumDb(levels);
        }

        public void SetAcoustics(bool on)
        {
            foreach (var s in sources)
            {
                var meta = s != null ? s.GetComponent<MetaXRAudioSource>() : null;
                if (meta != null) meta.EnableAcoustics = on;
            }
        }

        /// <summary>Places the sources around <paramref name="origin"/> and plays them (from the start). No effect while playing.</summary>
        public void Begin(Vector3 origin, Quaternion yaw, float radius, float[] azimuthsDeg)
        {
            if (Playing) return;
            _fixed = new Vector3[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                float az = azimuthsDeg != null && i < azimuthsDeg.Length ? azimuthsDeg[i] : 0f;
                _fixed[i] = Ring.Position(origin, yaw, radius, az);
                sources[i].transform.position = _fixed[i];
                if (sources[i].clip != null) sources[i].Play();
            }
            Playing = true;
            Paused = false;
        }

        public void End()
        {
            foreach (var s in sources) if (s != null) s.Stop();
            Playing = false;
            Paused = false;
        }

        /// <summary>Pause / resume where the recordings are (RT60 measurement, Debug).</summary>
        public void Pause(bool pause)
        {
            if (!Playing || Paused == pause) return;
            foreach (var s in sources) { if (pause) s.Pause(); else s.UnPause(); }
            Paused = pause;
        }

        void LateUpdate()
        {
            if (!Playing || _fixed == null) return;
            for (int i = 0; i < sources.Length && i < _fixed.Length; i++)
            {
                var t = sources[i].transform;
                if ((t.position - _fixed[i]).sqrMagnitude > 1e-8f) t.position = _fixed[i];
            }
        }

        /// <summary>Log note: <c>bg1=-2,0,0;bg2=2,0,0;bg3=0,0,-2 spatial=1</c>, positions relative to the listener (m).</summary>
        public string Note(Transform listener)
        {
            var sb = new StringBuilder();
            bool allSpatial = true;
            for (int i = 0; i < sources.Length; i++)
            {
                Vector3 p = listener != null ? listener.InverseTransformPoint(sources[i].transform.position) : sources[i].transform.position;
                if (i > 0) sb.Append(';');
                sb.Append("bg").Append(i + 1).Append('=').Append(Fmt.F(p.x, 2)).Append(',').Append(Fmt.F(p.y, 2)).Append(',').Append(Fmt.F(p.z, 2));
                allSpatial &= sources[i].spatialize;
            }
            sb.Append(" spatial=").Append(allSpatial ? 1 : 0);
            return sb.ToString();
        }
    }
}
