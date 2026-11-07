using System;
using System.Globalization;
using System.Text;

namespace GPL.HearingLoss
{
    /// <summary>Which ear(s) a loss applies to.</summary>
    public enum Ear { Both = 0, Left = 1, Right = 2 }

    /// <summary>
    /// Band values per ear (dB HL, negative = loss, 10 bands of <see cref="ANSIS344.BandHz"/>). Unilateral loss = the other ear None.
    /// Immutable; <see cref="Note"/> / <see cref="Parse"/> give the text form for presets and logs: <c>L=0,0,0,0,-5,-10,-10,-20,-10,0;R=0,...</c>.
    /// </summary>
    public sealed class HearingProfile : IEquatable<HearingProfile>
    {
        readonly int[] _left, _right;

        public HearingProfile(int[] left, int[] right)
        {
            _left = Check(left, nameof(left));
            _right = Check(right, nameof(right));
        }

        public static HearingProfile Normal => new HearingProfile(new int[ANSIS344.BandCount], new int[ANSIS344.BandCount]);

        /// <summary>The same bands on the ears selected by <paramref name="ear"/>; the other ear normal.</summary>
        public static HearingProfile For(Ear ear, int[] bands)
        {
            var none = new int[ANSIS344.BandCount];
            return new HearingProfile(ear == Ear.Right ? none : bands, ear == Ear.Left ? none : bands);
        }

        /// <summary>A fixed preset (None / Mild / Moderate / Severe) on the ears selected by <paramref name="ear"/>.</summary>
        public static HearingProfile For(Ear ear, HearingLossLevel preset) => For(ear, HearingPresets.Of(preset));

        public int[] Left => (int[])_left.Clone();
        public int[] Right => (int[])_right.Clone();
        public int[] Bands(Ear ear) => ear == Ear.Right ? Right : Left;

        public bool IsNormal(Ear ear)
        {
            if (ear != Ear.Right && !AllZero(_left)) return false;
            if (ear != Ear.Left && !AllZero(_right)) return false;
            return true;
        }

        public string Note() => "L=" + Join(_left) + ";R=" + Join(_right);
        public override string ToString() => Note();

        /// <summary>Parses <see cref="Note"/> output. A single list without "L="/"R=" applies to both ears.</summary>
        public static HearingProfile Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            text = text.Trim();
            if (text.Length == 0) return Normal;
            int[] left = null, right = null;
            foreach (var part in text.Split(';'))
            {
                var p = part.Trim();
                if (p.StartsWith("L=", StringComparison.OrdinalIgnoreCase)) left = ParseList(p.Substring(2));
                else if (p.StartsWith("R=", StringComparison.OrdinalIgnoreCase)) right = ParseList(p.Substring(2));
                else if (p.Length > 0) left = right = ParseList(p);
            }
            return new HearingProfile(left ?? new int[ANSIS344.BandCount], right ?? new int[ANSIS344.BandCount]);
        }

        public bool Equals(HearingProfile other)
        {
            if (other == null) return false;
            for (int i = 0; i < ANSIS344.BandCount; i++)
                if (_left[i] != other._left[i] || _right[i] != other._right[i]) return false;
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as HearingProfile);
        public override int GetHashCode() => Note().GetHashCode();

        static int[] Check(int[] bands, string name)
        {
            if (bands == null) throw new ArgumentNullException(name);
            if (bands.Length != ANSIS344.BandCount) throw new ArgumentException($"Need {ANSIS344.BandCount} bands.", name);
            var copy = new int[ANSIS344.BandCount];
            for (int i = 0; i < copy.Length; i++)
            {
                if (bands[i] < ANSIS344.MinBandDb || bands[i] > ANSIS344.MaxBandDb)
                    throw new ArgumentOutOfRangeException(name, $"Band {i} = {bands[i]} dB outside [{ANSIS344.MinBandDb}, {ANSIS344.MaxBandDb}].");
                copy[i] = bands[i];
            }
            return copy;
        }

        static int[] ParseList(string s)
        {
            var items = s.Split(',');
            if (items.Length != ANSIS344.BandCount) throw new FormatException($"Need {ANSIS344.BandCount} band values: '{s}'.");
            var r = new int[ANSIS344.BandCount];
            for (int i = 0; i < r.Length; i++) r[i] = int.Parse(items[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture);
            return r;
        }

        static string Join(int[] a)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < a.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(a[i].ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        static bool AllZero(int[] a)
        {
            foreach (var v in a) if (v != 0) return false;
            return true;
        }
    }
}
