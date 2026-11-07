using System;
using System.Collections;
using UnityEngine;

namespace GPL.Acoustics
{
    /// <summary>
    /// Measures the reverberation time of the rendered audio. Put it on the AudioListener's GameObject: OnAudioFilterRead there sees
    /// the final mix (after the Meta spatializer and the mixer's Reflection effect), both ears summed. Procedure: the caller's other
    /// sounds are silenced, the probe source plays a single-sample impulse, the output is recorded for a few seconds and evaluated with
    /// <see cref="Rt60Estimator"/>. Takes about settle + record seconds (5 s by default). Runs on scaled time: do not measure while the
    /// Stop dialog is open.
    /// </summary>
    public class Rt60Meter : MonoBehaviour
    {
        [Range(0.05f, 1f)] public float impulseAmplitude = 0.5f;
        public float recordSeconds = 3.5f;
        [Tooltip("Silence before the impulse (at least this and 1.2 x the expected reverb time).")]
        public float settleSeconds = 1f;

        float[] _energy = new float[0];
        volatile int _write;
        volatile bool _recording;
        int _sampleRate = 48000;
        AudioClip _impulse;

        public bool Measuring { get; private set; }

        void Awake() => _sampleRate = AudioSettings.outputSampleRate;

        // Audio thread: only copies the squared output into a preallocated buffer.
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_recording) return;
            var energy = _energy;
            int frames = data.Length / channels;
            int w = _write;
            for (int f = 0; f < frames && w < energy.Length; f++, w++)
            {
                float e = 0f;
                for (int c = 0; c < channels; c++) { float s = data[f * channels + c]; e += s * s; }
                energy[w] = e;
            }
            _write = w;
        }

        /// <summary>
        /// One measurement at the current acoustic settings. <paramref name="probe"/> is routed like the stimulus (same mixer group,
        /// same spatial settings) and is restored afterwards. <paramref name="silenceOthers"/> (true = silence, false = restore) pauses
        /// the caller's other sounds (background, stimulus).
        /// </summary>
        public IEnumerator Measure(AudioSource probe, float expectedRt60, Action<bool> silenceOthers, Action<Rt60Result> done)
        {
            if (probe == null) { done?.Invoke(Rt60Result.Invalid("no probe source")); yield break; }
            Measuring = true;
            int needed = (int)(_sampleRate * (recordSeconds + 1.5f));
            if (_energy.Length != needed) _energy = new float[needed];
            if (_impulse == null) _impulse = MakeImpulse(_sampleRate, impulseAmplitude);

            var clipBefore = probe.clip;
            bool loopBefore = probe.loop;
            probe.Stop();
            silenceOthers?.Invoke(true);
            yield return new WaitForSeconds(Mathf.Max(settleSeconds, 1.2f * expectedRt60));

            _write = 0;
            _recording = true;
            yield return null;
            probe.clip = _impulse;
            probe.loop = false;
            probe.Play();
            yield return new WaitForSeconds(recordSeconds);
            _recording = false;
            probe.Stop();
            probe.clip = clipBefore;
            probe.loop = loopBefore;
            silenceOthers?.Invoke(false);

            yield return null;
            int recorded = _write;
            // the impulse is the loudest event in the first part of the recording
            int peak = 0;
            float max = 0f;
            int search = Mathf.Min(recorded, (int)(_sampleRate * 1.5f));
            for (int i = 0; i < search; i++) if (_energy[i] > max) { max = _energy[i]; peak = i; }
            var result = max <= 0f ? Rt60Result.Invalid("no signal recorded")
                                   : Rt60Estimator.Estimate(_energy, peak, recorded - peak, _sampleRate);
            Debug.Log("[GPL] RT60 measurement: " + result + " (expected " + GPL.Core.Fmt.F(expectedRt60, 2) + " s)");
            Measuring = false;
            done?.Invoke(result);
        }

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
