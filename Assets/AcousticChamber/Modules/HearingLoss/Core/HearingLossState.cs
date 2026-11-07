using System;

namespace GPL.HearingLoss
{
    /// <summary>
    /// The HLSim state machine (HLSIM_GUIDELINES section 6) for one set of bands: presets, Manual (restores the last manual values
    /// when switched back) and Predicted (ANSI S3.44). Use one instance per ear when the ears differ.
    /// </summary>
    public sealed class HearingLossState
    {
        readonly int[] _bands = new int[ANSIS344.BandCount];
        readonly int[] _previousManual = new int[ANSIS344.BandCount];

        public HearingLossLevel SelectedLevel { get; private set; } = HearingLossLevel.None;

        /// <summary>Copy of the current band values (dB HL, negative = loss).</summary>
        public int[] Bands => (int[])_bands.Clone();
        public int[] PreviousManualBands => (int[])_previousManual.Clone();

        public event Action<int[]> Changed;

        public void Set(HearingLossLevel level, PredictionParameters prediction = null)
        {
            if (level == HearingLossLevel.Predicted && prediction == null)
                throw new ArgumentException("Prediction parameters cannot be null when hearing loss level is Predicted.");

            int[] src;
            switch (level)
            {
                case HearingLossLevel.Manual: src = _previousManual; break;
                case HearingLossLevel.Predicted: src = ANSIS344.Apply(prediction); break;
                default: src = HearingPresets.Of(level); break;
            }
            Array.Copy(src, _bands, ANSIS344.BandCount);
            SelectedLevel = level;
            Changed?.Invoke(Bands);
        }

        /// <summary>Manual mode only (the sliders are disabled otherwise): sets one band and remembers it.</summary>
        public void SetManualBand(int index, int db)
        {
            if (SelectedLevel != HearingLossLevel.Manual) throw new InvalidOperationException("Bands are editable in Manual mode only.");
            if (index < 0 || index >= ANSIS344.BandCount) throw new ArgumentOutOfRangeException(nameof(index));
            db = Math.Max(ANSIS344.MinBandDb, Math.Min(ANSIS344.MaxBandDb, db));
            _bands[index] = db;
            _previousManual[index] = db;
            Changed?.Invoke(Bands);
        }

        /// <summary>"Reset all to zero" (Manual mode).</summary>
        public void ResetManual()
        {
            if (SelectedLevel != HearingLossLevel.Manual) throw new InvalidOperationException("Bands are editable in Manual mode only.");
            Array.Clear(_bands, 0, _bands.Length);
            Array.Clear(_previousManual, 0, _previousManual.Length);
            Changed?.Invoke(Bands);
        }
    }
}
