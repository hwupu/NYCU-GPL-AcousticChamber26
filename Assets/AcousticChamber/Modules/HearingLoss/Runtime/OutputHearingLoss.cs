using System;
using UnityEngine;

namespace GPL.HearingLoss
{
    /// <summary>
    /// Hearing loss on the app's final output. Lives on the AudioListener's GameObject: OnAudioFilterRead there sees the final
    /// interleaved mix after the Meta spatializer, the room and the mixer. Channel 0 = left ear, channel 1 = right ear, each through its
    /// own 10-band peaking bank (<see cref="EarFilterBank"/>); further channels pass unchanged, a mono output uses the left bank.
    /// <see cref="PlayTestTone"/> adds a sine to one ear *before* the filters (the per-ear check: is left left, and is the loss on it?).
    /// Changes apply at the next audio block, instantly (no ramp, like the original). Allocation-free on the audio thread.
    /// </summary>
    [DisallowMultipleComponent]
    public class OutputHearingLoss : MonoBehaviour
    {
        readonly object _gate = new object();
        readonly int[] _pendingLeft = new int[ANSIS344.BandCount];
        readonly int[] _pendingRight = new int[ANSIS344.BandCount];
        volatile int _version;
        int _appliedVersion = -1;
        volatile int _sampleRate;
        int _appliedRate;

        EarFilterBank _left, _right;
        readonly ToneGenerator _tone = new ToneGenerator();
        volatile bool _toneRequest, _toneStop;
        Ear _toneEar; double _toneHz, _toneDbfs, _toneSeconds;

        volatile float _preL = -200f, _preR = -200f, _postL = -200f, _postR = -200f;

        public HearingProfile Profile { get; private set; } = HearingProfile.Normal;
        public event Action<HearingProfile> ProfileChanged;

        /// <summary>The OutputHearingLoss on the active AudioListener, added if missing; null when there is no listener.</summary>
        public static OutputHearingLoss Ensure()
        {
            var listener = FindAnyObjectByType<AudioListener>();
            if (listener == null) return null;
            var c = listener.GetComponent<OutputHearingLoss>();
            return c != null ? c : listener.gameObject.AddComponent<OutputHearingLoss>();
        }

        void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            _appliedRate = _sampleRate;
            _left = new EarFilterBank(_appliedRate);
            _right = new EarFilterBank(_appliedRate);
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        void OnDestroy() => AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;

        void OnAudioConfigurationChanged(bool deviceWasChanged) => _sampleRate = AudioSettings.outputSampleRate;

        /// <summary>New per-ear bands. Main thread.</summary>
        public void SetProfile(HearingProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var l = profile.Left; var r = profile.Right;
            lock (_gate)
            {
                Array.Copy(l, _pendingLeft, l.Length);
                Array.Copy(r, _pendingRight, r.Length);
            }
            _version++;
            Profile = profile;
            ProfileChanged?.Invoke(profile);
        }

        /// <summary>Plays a sine on one ear (or both) through the hearing loss. Default 1 kHz, -20 dBFS peak, 1 s.</summary>
        public void PlayTestTone(Ear ear, float hz = 1000f, float levelDbfs = -20f, float seconds = 1f)
        {
            lock (_gate) { _toneEar = ear; _toneHz = hz; _toneDbfs = levelDbfs; _toneSeconds = seconds; }
            _toneStop = false;
            _toneRequest = true;
        }

        public void StopTestTone() { _toneRequest = false; _toneStop = true; }
        public bool TestTonePlaying => _toneRequest || _tone.Playing;

        /// <summary>RMS of the last audio block in dBFS per ear, before and after the filters (-200 = silence).</summary>
        public float InputRmsDbfs(Ear ear) => ear == Ear.Right ? _preR : _preL;
        public float OutputRmsDbfs(Ear ear) => ear == Ear.Right ? _postR : _postL;

        /// <summary>Text for the log or a Debug status: <c>hl L=...;R=... q=24,12,... fs=48000</c>.</summary>
        public string Note() => $"hl {Profile.Note()} q={string.Join(",", EarFilterBank.DefaultQ)} fs={_appliedRate}";

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (_left == null || channels <= 0) return;

            int rate = _sampleRate;
            if (rate > 0 && rate != _appliedRate)
            {
                _left.SetSampleRate(rate);
                _right.SetSampleRate(rate);
                _appliedRate = rate;
            }
            if (_appliedVersion != _version)
            {
                lock (_gate)
                {
                    _appliedVersion = _version;
                    _left.SetGains(_pendingLeft);
                    _right.SetGains(_pendingRight);
                }
            }
            if (_toneStop) { _toneStop = false; _tone.Stop(); }
            if (_toneRequest)
            {
                lock (_gate) { _tone.Start(_toneEar, _toneHz, _toneDbfs, _toneSeconds, _appliedRate); }
                _toneRequest = false;
            }
            _tone.Mix(data, channels);

            _preL = RmsDb(data, channels, 0);
            _preR = channels > 1 ? RmsDb(data, channels, 1) : _preL;
            _left.Process(data, channels, 0);
            if (channels > 1) _right.Process(data, channels, 1);
            _postL = RmsDb(data, channels, 0);
            _postR = channels > 1 ? RmsDb(data, channels, 1) : _postL;
        }

        static float RmsDb(float[] data, int channels, int channel)
        {
            double sum = 0; int n = 0;
            for (int i = channel; i < data.Length; i += channels) { sum += data[i] * data[i]; n++; }
            if (n == 0 || sum <= 0) return -200f;
            return (float)(10.0 * Math.Log10(sum / n));
        }
    }
}
