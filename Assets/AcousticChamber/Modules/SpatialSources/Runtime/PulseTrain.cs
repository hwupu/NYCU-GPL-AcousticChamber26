using UnityEngine;

namespace GPL.SpatialSources
{
    /// <summary>
    /// Plays the AudioSource's clip as a pulse train (<see cref="PulseSchedule"/>): on for <c>onS</c>, off for <c>offS</c>,
    /// repeated until <see cref="Stop"/>. Runs on scaled time, so the Stop dialog (time scale 0) holds it; a pulse longer than the
    /// clip ends with the clip, a shorter one is cut at <c>onS</c>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PulseTrain : MonoBehaviour
    {
        AudioSource _source;
        PulseSchedule _schedule;

        public bool Playing => _schedule != null && _schedule.Running;
        public int Pulses => _schedule != null ? _schedule.PulsesStarted : 0;

        void Awake() => _source = GetComponent<AudioSource>();

        /// <param name="onS">Pulse length (s); 0 or less = the clip length.</param>
        public void Play(float onS, float offS)
        {
            Stop();
            if (_source.clip == null) { Debug.LogWarning("[GPL] PulseTrain: no clip on " + name); return; }
            _source.loop = false;
            _schedule = new PulseSchedule(onS > 0f ? onS : _source.clip.length, offS);
            if (_schedule.Start()) _source.Play();
        }

        public void Stop()
        {
            if (_schedule != null) _schedule.Stop();
            if (_source != null) _source.Stop();
        }

        void Update()
        {
            if (!Playing) return;
            if (_schedule.Advance(Time.deltaTime)) { _source.Stop(); _source.Play(); }
            else if (!_schedule.IsOn && _source.isPlaying) _source.Stop();
        }

        void OnDisable() => Stop();
    }
}
