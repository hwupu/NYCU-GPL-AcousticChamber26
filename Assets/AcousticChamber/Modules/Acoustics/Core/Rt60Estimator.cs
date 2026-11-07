using System;
using GPL.Core;

namespace GPL.Acoustics
{
    public struct Rt60Result
    {
        public bool Valid;
        /// <summary>From the -5..-25 dB part of the decay, extrapolated to 60 dB (s).</summary>
        public double T20;
        /// <summary>From -5..-35 dB (NaN when the recording did not decay that far).</summary>
        public double T30;
        /// <summary>Depth of the decay curve in the recording (dB below the start).</summary>
        public double DepthDb;
        public string Reason;

        public static Rt60Result Invalid(string reason) =>
            new Rt60Result { Valid = false, T20 = double.NaN, T30 = double.NaN, Reason = reason ?? "" };

        /// <summary>Log note: <c>valid=1 t20=0.712 t30=0.698 depth_db=48</c> (or <c>valid=0 reason=...</c>).</summary>
        public string Note() => Valid
            ? "valid=1 t20=" + Fmt.F(T20, 3) + " t30=" + Fmt.F(T30, 3) + " depth_db=" + Fmt.F(DepthDb, 0)
            : "valid=0 reason=" + (Reason ?? "").Replace(' ', '_');

        public override string ToString() => Valid
            ? "T20 " + Fmt.F(T20, 2) + " s, T30 " + (double.IsNaN(T30) ? "n/a" : Fmt.F(T30, 2) + " s") + ", depth " + Fmt.F(DepthDb, 0) + " dB"
            : "invalid (" + Reason + ")";
    }

    /// <summary>
    /// Reverberation time from a recorded impulse response (ISO 3382 style): Schroeder backward integration of the energy, then a
    /// least-squares line through the decay between -5 dB and -25 dB (T20) or -35 dB (T30), extrapolated to -60 dB.
    /// </summary>
    public static class Rt60Estimator
    {
        /// <param name="energy">Squared samples (summed over the channels).</param>
        /// <param name="start">Index of the impulse (the direct sound).</param>
        /// <param name="count">Samples from <paramref name="start"/> to evaluate.</param>
        public static Rt60Result Estimate(float[] energy, int start, int count, double sampleRate)
        {
            if (energy == null || start < 0 || count < 100 || start + count > energy.Length || sampleRate <= 0)
                return Rt60Result.Invalid("no data");

            // remove the noise floor (mean of the last tenth) so that the integral does not flatten the curve
            int tail = Math.Max(10, count / 10);
            double floor = 0;
            for (int i = count - tail; i < count; i++) floor += energy[start + i];
            floor /= tail;

            // the recording must have decayed well before its end, otherwise the floor estimate would eat the real decay
            int headCount = Math.Max(10, (int)(0.02 * sampleRate));
            double head = 0;
            for (int i = 0; i < headCount && i < count; i++) head += energy[start + i];
            head /= headCount;
            if (head <= 0 || floor > head * 1e-3) return Rt60Result.Invalid("recording ends before the decay is complete");

            var edc = new double[count];
            double sum = 0;
            for (int i = count - 1; i >= 0; i--)
            {
                sum += Math.Max(0.0, energy[start + i] - floor);
                edc[i] = sum;
            }
            if (edc[0] <= 0) return Rt60Result.Invalid("silent");

            var db = new double[count];
            double minDb = 0;
            for (int i = 0; i < count; i++)
            {
                db[i] = edc[i] <= 0 ? double.NegativeInfinity : 10.0 * Math.Log10(edc[i] / edc[0]);
                if (!double.IsNegativeInfinity(db[i]) && db[i] < minDb) minDb = db[i];
            }

            var result = Rt60Result.Invalid("");
            result.DepthDb = -minDb;
            int i5 = IndexBelow(db, -5.0), i25 = IndexBelow(db, -25.0), i35 = IndexBelow(db, -35.0);
            if (i5 < 0 || i25 < 0 || i25 <= i5 + 2)
            {
                result.Reason = "decay does not reach -25 dB (depth " + Fmt.F(-minDb, 0) + " dB)";
                return result;
            }
            result.T20 = Fit(db, i5, i25, sampleRate);
            if (i35 > i5 + 2) result.T30 = Fit(db, i5, i35, sampleRate);
            result.Valid = !double.IsNaN(result.T20) && result.T20 > 0;
            result.Reason = result.Valid ? "" : "fit failed";
            return result;
        }

        static int IndexBelow(double[] db, double level)
        {
            for (int i = 0; i < db.Length; i++) if (db[i] <= level) return i;
            return -1;
        }

        /// <summary>Least-squares line through the decay curve between two indices; the time for 60 dB of decay.</summary>
        static double Fit(double[] db, int from, int to, double sampleRate)
        {
            double n = to - from + 1, sx = 0, sy = 0, sxx = 0, sxy = 0;
            for (int i = from; i <= to; i++)
            {
                double t = i / sampleRate;
                sx += t; sy += db[i]; sxx += t * t; sxy += t * db[i];
            }
            double denom = n * sxx - sx * sx;
            if (Math.Abs(denom) < 1e-18) return double.NaN;
            double slope = (n * sxy - sx * sy) / denom; // dB per second (negative)
            return slope < 0 ? -60.0 / slope : double.NaN;
        }
    }
}
