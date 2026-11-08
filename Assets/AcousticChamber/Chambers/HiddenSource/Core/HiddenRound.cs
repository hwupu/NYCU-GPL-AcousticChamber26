using System;
using System.Globalization;

namespace AcousticChamber.Chambers.HiddenSource
{
    /// <summary>A place on the floor seen from the listener: azimuth (deg, 0 = ahead, negative = left, -180..180) and distance (m).</summary>
    public readonly struct Spot
    {
        public readonly double AzimuthDeg, DistanceM;
        public Spot(double azimuthDeg, double distanceM) { AzimuthDeg = azimuthDeg; DistanceM = distanceM; }
    }

    /// <summary>What a round measured (Docs/Design.md "Error measures"): from the reference head position, on the floor plane.</summary>
    public readonly struct RoundResult
    {
        public readonly Spot Source, Guess;
        /// <summary>Signed angle from the source to the guess, -180..180; positive = the guess is to the right of the source.</summary>
        public readonly double AzimuthErrorDeg;
        /// <summary>Guess distance minus source distance (m); positive = the guess is farther.</summary>
        public readonly double DistanceErrorM;

        public RoundResult(Spot source, Spot guess)
        {
            Source = source;
            Guess = guess;
            AzimuthErrorDeg = HiddenRound.WrapDeg(guess.AzimuthDeg - source.AzimuthDeg);
            DistanceErrorM = guess.DistanceM - source.DistanceM;
        }

        /// <summary>"Azimuth error +12° (source -40°, guess -28°). Distance error -0.8 m (source 3.5 m, guess 2.7 m)."</summary>
        public string Describe() =>
            "Azimuth error " + Signed(AzimuthErrorDeg, "0") + "° (source " + Signed(Source.AzimuthDeg, "0") + "°, guess " + Signed(Guess.AzimuthDeg, "0") + "°). " +
            "Distance error " + Signed(DistanceErrorM, "0.0") + " m (source " + Source.DistanceM.ToString("0.0", CultureInfo.InvariantCulture) +
            " m, guess " + Guess.DistanceM.ToString("0.0", CultureInfo.InvariantCulture) + " m).";

        static string Signed(double v, string format)
        {
            string s = Math.Abs(v).ToString(format, CultureInfo.InvariantCulture);
            return (v < 0 && s.Trim('0', '.').Length > 0 ? "-" : "+") + s; // no "-0"
        }
    }

    /// <summary>Pure maths of a HiddenSource round: drawing the source and measuring the guess (Unity-free, injected random).</summary>
    public static class HiddenRound
    {
        public const double MinSourceM = 1.0, MaxSourceM = 6.0;

        /// <summary>A source uniform in azimuth (-180..180) and in distance (1..6 m).</summary>
        public static Spot Draw(Random rng) =>
            new Spot(rng.NextDouble() * 360.0 - 180.0, MinSourceM + rng.NextDouble() * (MaxSourceM - MinSourceM));

        /// <summary>World position (x, z) of a spot seen from the head at (headX, headZ) looking along <paramref name="yawDeg"/>:
        /// origin + yaw * (r sin az, r cos az).</summary>
        public static void ToWorld(Spot s, double headX, double headZ, double yawDeg, out double x, out double z)
        {
            double a = (s.AzimuthDeg + yawDeg) * Math.PI / 180.0;
            x = headX + s.DistanceM * Math.Sin(a);
            z = headZ + s.DistanceM * Math.Cos(a);
        }

        /// <summary>The spot of the world point (x, z) seen from the reference head.</summary>
        public static Spot FromWorld(double x, double z, double headX, double headZ, double yawDeg)
        {
            double dx = x - headX, dz = z - headZ;
            double az = Math.Atan2(dx, dz) * 180.0 / Math.PI - yawDeg;
            return new Spot(WrapDeg(az), Math.Sqrt(dx * dx + dz * dz));
        }

        /// <summary>Maps an angle in degrees into -180..180.</summary>
        public static double WrapDeg(double deg)
        {
            deg %= 360.0;
            if (deg > 180.0) deg -= 360.0;
            else if (deg <= -180.0) deg += 360.0;
            return deg;
        }
    }
}
