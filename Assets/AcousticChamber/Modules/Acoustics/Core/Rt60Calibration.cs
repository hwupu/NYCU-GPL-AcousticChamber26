using System;
using System.Collections.Generic;

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
        /// <summary>Plugin platform the table was measured on: <c>Android</c> (Quest) or <c>Windows</c> (Editor / Link).</summary>
        public string Platform { get; }
        /// <summary><see cref="ProtocolIso"/> (T_mid of the ISO 3382-2 protocol) or <see cref="ProtocolHw2"/> (broadband T30).</summary>
        public string Protocol { get; }
        public double RoomWidth { get; }
        public double RoomDepth { get; }
        public double RoomHeight { get; }

        public const string ProtocolIso = "iso3382-2_tmid";
        public const string ProtocolHw2 = "hw2_broadband_t30";

        /// <summary>
        /// True when this table counts as calibrated for a room: ISO protocol, the same platform, the same size (1 cm). Anything
        /// else is an estimate (the RT60 is extrapolated by V/S or measured another way).
        /// </summary>
        public bool CalibratedFor(string platform, double width, double depth, double height) =>
            Protocol == ProtocolIso && string.Equals(Platform, platform, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(RoomWidth - width) < 0.01 && Math.Abs(RoomDepth - depth) < 0.01 && Math.Abs(RoomHeight - height) < 0.01;

        /// <param name="reflection">Coefficients, strictly ascending.</param>
        /// <param name="rt60S">Measured T30 (s) for each coefficient, strictly ascending.</param>
        public Rt60Calibration(double[] reflection, double[] rt60S, double roomWidth, double roomDepth, double roomHeight, string source)
            : this(reflection, rt60S, roomWidth, roomDepth, roomHeight, source, "Windows", ProtocolHw2) { }

        public Rt60Calibration(double[] reflection, double[] rt60S, double roomWidth, double roomDepth, double roomHeight, string source,
                               string platform, string protocol)
        {
            Platform = platform ?? "";
            Protocol = protocol ?? "";
            RoomWidth = roomWidth; RoomDepth = roomDepth; RoomHeight = roomHeight;
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

        public double ReflectionAt(int i) => _reflection[i];
        public double Rt60At(int i) => _rt60[i];

        /// <summary>One line for statuses and reports.</summary>
        public string Describe() => Source + " [" + Protocol + ", " + Platform + ", " +
                                    Rt60Protocol.RoomText(RoomWidth, RoomDepth, RoomHeight) + " m, " + Count + " points]";

        /// <summary>
        /// Table from a calibration-sweep CSV (<see cref="Rt60Report.Csv"/>, kind <c>sweep</c>): per reflection coefficient the mean
        /// T_mid (T30, ear mean) over positions and repeats. Throws with the reason when the CSV is not a usable sweep (other kind,
        /// several rooms or platforms, a coefficient without a valid T_mid, or times that do not rise with the coefficient).
        /// </summary>
        public static Rt60Calibration FromSweepCsv(string csvText, string source)
        {
            var lines = (csvText ?? "").Replace("\r", "").Split('\n');
            if (lines.Length < 2) throw new FormatException("empty CSV");
            var head = GPL.Core.Csv.SplitLine(lines[0]);
            int Col(string name)
            {
                int i = head.IndexOf(name);
                if (i < 0) throw new FormatException("column " + name + " missing");
                return i;
            }
            int cKind = Col("kind"), cPlatform = Col("platform"), cRoom = Col("room_m"), cRefl = Col("reflection"), cPos = Col("position"),
                cRep = Col("repeat"), cEar = Col("ear"), cBand = Col("band_hz"), cT30 = Col("t30_s"), cValid = Col("valid_t30");
            string platform = null, room = null;
            // reflection -> (position, repeat) -> [T30 500, T30 1000]
            var data = new SortedDictionary<double, Dictionary<string, double[]>>();
            for (int n = 1; n < lines.Length; n++)
            {
                if (lines[n].Length == 0) continue;
                var f = GPL.Core.Csv.SplitLine(lines[n]);
                if (f.Count < head.Count) throw new FormatException("line " + (n + 1) + ": too few fields");
                if (f[cKind] != Rt60Report.KindSweep) throw new FormatException("line " + (n + 1) + ": kind is " + f[cKind] + ", not sweep");
                if (platform == null) { platform = f[cPlatform]; room = f[cRoom]; }
                else if (platform != f[cPlatform] || room != f[cRoom]) throw new FormatException("several platforms or rooms in one sweep");
                if (f[cEar] != "mean" || (f[cBand] != "500" && f[cBand] != "1000")) continue;
                if (!GPL.Core.Fmt.TryDouble(f[cRefl], out double refl)) throw new FormatException("line " + (n + 1) + ": no reflection");
                if (!data.TryGetValue(refl, out var recs)) data[refl] = recs = new Dictionary<string, double[]>();
                string key = f[cPos] + "/" + f[cRep];
                if (!recs.TryGetValue(key, out var pair)) recs[key] = pair = new[] { double.NaN, double.NaN };
                double t = f[cValid] == "true" && GPL.Core.Fmt.TryDouble(f[cT30], out double v) ? v : double.NaN;
                pair[f[cBand] == "500" ? 0 : 1] = t;
            }
            if (data.Count < 2) throw new FormatException("a sweep needs at least 2 coefficients");
            if (!Rt60Protocol.TryParseRoom(room, out double w, out double d, out double h)) throw new FormatException("bad room_m " + room);
            var rs = new List<double>();
            var ts = new List<double>();
            foreach (var kv in data)
            {
                double sum = 0; int count = 0;
                foreach (var pair in kv.Value.Values)
                    if (!double.IsNaN(pair[0]) && !double.IsNaN(pair[1])) { sum += (pair[0] + pair[1]) / 2.0; count++; }
                if (count == 0 && kv.Key >= Rt60Protocol.CoreSweepFloor)
                    throw new FormatException("reflection " + GPL.Core.Fmt.F(kv.Key, 4) + ": no valid T_mid");
                rs.Add(kv.Key);
                ts.Add(count == 0 ? double.NaN : sum / count);
            }
            int core = 0;
            while (core < rs.Count && rs[core] < Rt60Protocol.CoreSweepFloor) core++;
            for (int i = core + 1; i < ts.Count; i++)
                if (ts[i] <= ts[i - 1])
                    throw new FormatException("T_mid does not rise with the coefficient at " + GPL.Core.Fmt.F(rs[i], 4) +
                                              " (" + GPL.Core.Fmt.F(ts[i - 1], 3) + " -> " + GPL.Core.Fmt.F(ts[i], 3) + " s)");
            // extension points below the core floor: keep each only while it lies below the next kept point, from the top down
            var dropped = new List<string>();
            int first = core;
            for (int i = core - 1; i >= 0; i--)
            {
                bool keep = first < ts.Count && !double.IsNaN(ts[i]) && ts[i] < ts[first] && first == i + 1;
                if (keep) { first = i; continue; }
                dropped.Add(GPL.Core.Fmt.F(rs[i], 4) + (double.IsNaN(ts[i]) ? " (no valid T_mid)" : " (T_mid " + GPL.Core.Fmt.F(ts[i], 3) + " s does not lie below the next point)"));
            }
            dropped.Reverse();
            var cal = new Rt60Calibration(rs.GetRange(first, rs.Count - first).ToArray(), ts.GetRange(first, ts.Count - first).ToArray(),
                                          w, d, h, source, platform, ProtocolIso);
            cal.DroppedPoints.AddRange(dropped);
            return cal;
        }

        /// <summary>Extension points left out by <see cref="FromSweepCsv"/>, with the reason (for the import log).</summary>
        public List<string> DroppedPoints { get; } = new List<string>();

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
