using System;
using System.Collections;
using UnityEngine;

namespace GPL.Acoustics
{
    /// <summary>
    /// Records the rendered impulse response. Lives on the AudioListener's GameObject (<see cref="Ensure"/>): OnAudioFilterRead there
    /// sees the final interleaved mix (after the Meta spatializer and the mixer's Reflection effect); channel 0 = left ear, 1 = right
    /// ear (a mono output fills both). Used by <see cref="Rt60Runner"/>; the evaluation is in GPL.Acoustics.Core. Runs on scaled
    /// time: do not measure while the Stop dialog is open.
    /// </summary>
    public class Rt60Meter : MonoBehaviour
    {
        public const float ImpulseAmplitude = 0.5f;

        float[] _left = new float[0], _right = new float[0];
        volatile int _write;
        volatile bool _recording;
        int _sampleRate = 48000;
        AudioClip _impulse;

        public bool Measuring { get; private set; }
        public int SampleRate => _sampleRate;

        /// <summary>The meter on the active AudioListener, added if missing; null without a listener.</summary>
        public static Rt60Meter Ensure()
        {
            var listener = FindAnyObjectByType<AudioListener>();
            if (listener == null) return null;
            var m = listener.GetComponent<Rt60Meter>();
            return m != null ? m : listener.gameObject.AddComponent<Rt60Meter>();
        }

        void Awake() => _sampleRate = AudioSettings.outputSampleRate;

        // Audio thread: copies the two ears into preallocated buffers.
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_recording || channels <= 0) return;
            var l = _left; var r = _right;
            int frames = data.Length / channels;
            int w = _write;
            for (int f = 0; f < frames && w < l.Length; f++, w++)
            {
                l[w] = data[f * channels];
                r[w] = channels > 1 ? data[f * channels + 1] : l[w];
            }
            _write = w;
        }

        /// <summary>
        /// Plays one impulse through <paramref name="probe"/> after <paramref name="settleSeconds"/> of silence and records
        /// <paramref name="recordSeconds"/> from the impulse on. <paramref name="done"/> gets the left and right channels, starting
        /// at the recording start (the impulse is inside, about 100 ms in), and the sample rate.
        /// </summary>
        public IEnumerator Record(AudioSource probe, double settleSeconds, double recordSeconds, Action<float[], float[], int> done)
        {
            if (probe == null) { done?.Invoke(null, null, _sampleRate); yield break; }
            Measuring = true;
            _sampleRate = AudioSettings.outputSampleRate;
            int needed = (int)(_sampleRate * (recordSeconds + 0.5));
            if (_left.Length != needed) { _left = new float[needed]; _right = new float[needed]; }
            if (_impulse == null || _impulse.frequency != _sampleRate) _impulse = MakeImpulse(_sampleRate, ImpulseAmplitude);

            probe.Stop();
            yield return new WaitForSeconds((float)settleSeconds);

            _write = 0;
            _recording = true;
            yield return null;
            probe.clip = _impulse;
            probe.loop = false;
            probe.Play();
            yield return new WaitForSeconds((float)recordSeconds);
            _recording = false;
            probe.Stop();
            yield return null;

            int n = _write;
            var l = new float[n]; var r = new float[n];
            Array.Copy(_left, l, n);
            Array.Copy(_right, r, n);
            Measuring = false;
            done?.Invoke(l, r, _sampleRate);
        }

        /// <summary>Stops a recording that was interrupted (runner cancelled).</summary>
        public void Abort() { _recording = false; Measuring = false; }

        void OnDisable() => Abort();

        static AudioClip MakeImpulse(int sampleRate, float amplitude)
        {
            // 200 ms with one non-zero sample after 100 ms: the spatializer fades a new voice in over its first buffers,
            // which would swallow an impulse at the very start
            var samples = new float[sampleRate / 5];
            samples[sampleRate / 10] = amplitude;
            var clip = AudioClip.Create("rt60_impulse", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
