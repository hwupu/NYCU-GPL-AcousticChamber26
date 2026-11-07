using System;

namespace GPL.HearingLoss
{
    /// <summary>
    /// Sine test tone added to one or both channels of an interleaved buffer (the per-ear check: left only, right only).
    /// 10 ms raised-cosine fade in and out; allocation-free; call <see cref="Mix"/> from the audio thread only.
    /// </summary>
    public sealed class ToneGenerator
    {
        const double FadeSeconds = 0.01;

        double _phase, _step, _amplitude;
        long _framesLeft, _framesTotal, _fadeFrames;
        Ear _ear;

        public bool Playing => _framesLeft > 0;
        public Ear Ear => _ear;

        /// <summary>Starts a tone (replaces a running one). Amplitude from <paramref name="levelDbfs"/> (peak, e.g. -20).</summary>
        public void Start(Ear ear, double hz, double levelDbfs, double seconds, double sampleRate)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            _ear = ear;
            _step = 2.0 * Math.PI * hz / sampleRate;
            _amplitude = Math.Pow(10.0, levelDbfs / 20.0);
            _framesTotal = _framesLeft = Math.Max(1L, (long)(seconds * sampleRate));
            _fadeFrames = Math.Max(1L, Math.Min((long)(FadeSeconds * sampleRate), _framesTotal / 2));
            _phase = 0;
        }

        public void Stop() { _framesLeft = 0; }

        /// <summary>Adds the tone to channel 0 (left) and/or 1 (right). A mono buffer gets the tone whatever the ear.</summary>
        public void Mix(float[] data, int channels)
        {
            if (_framesLeft <= 0 || channels <= 0) return;
            bool left = _ear != Ear.Right || channels == 1;
            bool right = _ear != Ear.Left && channels > 1;
            int frames = data.Length / channels;
            for (int f = 0; f < frames && _framesLeft > 0; f++, _framesLeft--)
            {
                long done = _framesTotal - _framesLeft;
                double env = 1.0;
                if (done < _fadeFrames) env = 0.5 - 0.5 * Math.Cos(Math.PI * done / _fadeFrames);
                else if (_framesLeft <= _fadeFrames) env = 0.5 - 0.5 * Math.Cos(Math.PI * (_framesLeft - 1) / _fadeFrames);
                float s = (float)(_amplitude * env * Math.Sin(_phase));
                _phase += _step;
                if (_phase > 2.0 * Math.PI) _phase -= 2.0 * Math.PI;
                int i = f * channels;
                if (left) data[i] += s;
                if (right) data[i + 1] += s;
            }
        }
    }
}
