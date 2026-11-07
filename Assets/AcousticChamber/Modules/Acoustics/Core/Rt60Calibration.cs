using System;

namespace GPL.Acoustics
{
    /// <summary>
    /// Maps a target reverberation time to the uniform reflection coefficient sent to Meta XR Audio, from a MEASURED table
    /// (coefficient -> T30), and back. For a room of another size the time is scaled by V/S (the reverberation time of a uniform
    /// shoebox at a fixed coefficient is proportional to V/S). Between table entries the time is interpolated on a log scale.
    /// Outside the table the nearest entry is used: <see cref="LowestRt60"/> is the shortest time the table can give.
    /// </summary>
    public sealed class Rt60Calibration
    {
        readonly double[] _reflection;
        readonly double[] _rt60;

        /// <summary>V / S of the room the table was measured in (m).</summary>
        public double CalibrationVolumeOverSurface { get; }
        /// <summary>Where the table comes from (for logs and READMEs).</summary>
        public string Source { get; }

        /// <param name="reflection">Coefficients, strictly ascending.</param>
        /// <param name="rt60S">Measured T30 (s) for each coefficient, strictly ascending.</param>
        public Rt60Calibration(double[] reflection, double[] rt60S, double roomWidth, double roomDepth, double roomHeight, string source)
        {
            if (reflection == null || rt60S == null || reflection.Length != rt60S.Length || reflection.Length < 2)
                throw new ArgumentException("need two equally long tables with at least 2 entries");
            for (int i = 1; i < reflection.Length; i++)
                if (reflection[i] <= reflection[i - 1] || rt60S[i] <= rt60S[i - 1] || rt60S[i - 1] <= 0)
                    throw new ArgumentException("tables must be strictly ascending and positive (entry " + i + ")");
            _reflection = (double[])reflection.Clone();
            _rt60 = (double[])rt60S.Clone();
            CalibrationVolumeOverSurface = RoomMath.VolumeOverSurface(roomWidth, roomDepth, roomHeight);
            Source = source ?? "";
        }

        /// <summary>
        /// Meta XR Audio 85 (Windows plugin), Unity spatializer + "Meta XR Audio Reflection" mixer effect, room 8.72 x 15 x 3.5 m
        /// (W x D x H), one coefficient on all six surfaces, no clutter, source 2 m ahead; T30 by <see cref="Rt60Estimator"/>
        /// (HW2 PB-15, 2026-10-18). Below about 0.3 s the decay is not a clean exponential, so 0.3 s is the lowest entry.
        /// Not yet re-measured with the Android plugin.
        /// </summary>
        public static readonly Rt60Calibration MetaXrAudioShoebox = new Rt60Calibration(
            new[] { 0.50, 0.60, 0.65, 0.70, 0.75, 0.80, 0.85, 0.88, 0.90, 0.92, 0.94, 0.95, 0.96, 0.97, 0.975, 0.98, 0.985, 0.99 },
            new[] { 0.30, 0.33, 0.37, 0.42, 0.51, 0.65, 0.89, 1.10, 1.30, 1.58, 1.89, 2.11, 2.49, 3.05, 3.52, 3.94, 4.81, 5.74 },
            8.72, 15.0, 3.5, "HW2 PB-15 2026-10-18, Windows plugin, 8.72x15x3.5 m");

        public int Count => _reflection.Length;
        public double LowestRt60 => _rt60[0];
        public double HighestRt60 => _rt60[_rt60.Length - 1];

        /// <summary>Lowest / highest RT60 the table can give in a room of this size.</summary>
        public double LowestRt60In(double volumeOverSurface) => _rt60[0] * Scale(volumeOverSurface);
        public double HighestRt60In(double volumeOverSurface) => _rt60[_rt60.Length - 1] * Scale(volumeOverSurface);

        public double ReflectionForRt60(double rt60S, double volumeOverSurface)
        {
            double t = rt60S / Scale(volumeOverSurface); // the equivalent time in the calibration room
            int last = _rt60.Length - 1;
            if (t <= _rt60[0]) return _reflection[0];
            if (t >= _rt60[last]) return _reflection[last];
            for (int i = 1; i <= last; i++)
            {
                if (t > _rt60[i]) continue;
                double f = (Math.Log(t) - Math.Log(_rt60[i - 1])) / (Math.Log(_rt60[i]) - Math.Log(_rt60[i - 1]));
                return _reflection[i - 1] + f * (_reflection[i] - _reflection[i - 1]);
            }
            return _reflection[last];
        }

        public double Rt60ForReflection(double reflection, double volumeOverSurface)
        {
            int last = _reflection.Length - 1;
            double t;
            if (reflection <= _reflection[0]) t = _rt60[0];
            else if (reflection >= _reflection[last]) t = _rt60[last];
            else
            {
                int i = 1;
                while (reflection > _reflection[i]) i++;
                double f = (reflection - _reflection[i - 1]) / (_reflection[i] - _reflection[i - 1]);
                t = Math.Exp(Math.Log(_rt60[i - 1]) + f * (Math.Log(_rt60[i]) - Math.Log(_rt60[i - 1])));
            }
            return t * Scale(volumeOverSurface);
        }

        double Scale(double volumeOverSurface) =>
            volumeOverSurface > 0 ? volumeOverSurface / CalibrationVolumeOverSurface : 1.0;
    }
}
