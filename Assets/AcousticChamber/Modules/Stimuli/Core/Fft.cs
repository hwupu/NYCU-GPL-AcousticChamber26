using System;

namespace GPL.Stimuli
{
    /// <summary>
    /// Complex discrete Fourier transform of any length, in place: radix-2 for powers of two, Bluestein (chirp-z through a
    /// power-of-two convolution) otherwise, so stimulus lengths need not be powers of two. Forward: X[k] = sum x[n] e^(-2 pi i k n / N).
    /// Inverse includes the 1/N scale. Pure; allocates for Bluestein.
    /// </summary>
    public static class Fft
    {
        public static void Forward(double[] re, double[] im) => Transform(re, im, false);

        public static void Inverse(double[] re, double[] im)
        {
            Transform(re, im, true);
            double s = 1.0 / re.Length;
            for (int i = 0; i < re.Length; i++) { re[i] *= s; im[i] *= s; }
        }

        public static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;

        public static int NextPowerOfTwo(int n)
        {
            int p = 1;
            while (p < n) p <<= 1;
            return p;
        }

        /// <summary>Unscaled transform; <paramref name="inverse"/> flips the exponent sign.</summary>
        static void Transform(double[] re, double[] im, bool inverse)
        {
            if (re == null || im == null || re.Length != im.Length) throw new ArgumentException("re and im must have the same length");
            int n = re.Length;
            if (n <= 1) return;
            if (IsPowerOfTwo(n)) Radix2(re, im, inverse);
            else Bluestein(re, im, inverse);
        }

        static void Radix2(double[] re, double[] im, bool inverse)
        {
            int n = re.Length;
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i < j)
                {
                    double t = re[i]; re[i] = re[j]; re[j] = t;
                    t = im[i]; im[i] = im[j]; im[j] = t;
                }
            }
            double sign = inverse ? 1.0 : -1.0;
            for (int len = 2; len <= n; len <<= 1)
            {
                int half = len >> 1;
                double step = sign * 2.0 * Math.PI / len;
                for (int k = 0; k < half; k++)
                {
                    // the twiddle factor is computed directly (no recurrence), so the error does not grow with n
                    double wr = Math.Cos(step * k), wi = Math.Sin(step * k);
                    for (int i = k; i < n; i += len)
                    {
                        int j = i + half;
                        double tr = re[j] * wr - im[j] * wi;
                        double ti = re[j] * wi + im[j] * wr;
                        re[j] = re[i] - tr; im[j] = im[i] - ti;
                        re[i] += tr; im[i] += ti;
                    }
                }
            }
        }

        static void Bluestein(double[] re, double[] im, bool inverse)
        {
            int n = re.Length;
            int m = NextPowerOfTwo(2 * n - 1);
            double sign = inverse ? 1.0 : -1.0;
            // chirp w[k] = exp(sign * i * pi * k^2 / n); k^2 mod 2n keeps the angle small and exact
            var wr = new double[n];
            var wi = new double[n];
            long twoN = 2L * n;
            for (int k = 0; k < n; k++)
            {
                long kk = (long)k * k % twoN;
                double a = sign * Math.PI * kk / n;
                wr[k] = Math.Cos(a); wi[k] = Math.Sin(a);
            }
            var ar = new double[m]; var ai = new double[m];
            var br = new double[m]; var bi = new double[m];
            for (int k = 0; k < n; k++)
            {
                ar[k] = re[k] * wr[k] - im[k] * wi[k];
                ai[k] = re[k] * wi[k] + im[k] * wr[k];
            }
            br[0] = wr[0]; bi[0] = -wi[0];
            for (int k = 1; k < n; k++)
            {
                br[k] = br[m - k] = wr[k];
                bi[k] = bi[m - k] = -wi[k];
            }
            Radix2(ar, ai, false);
            Radix2(br, bi, false);
            for (int k = 0; k < m; k++)
            {
                double r = ar[k] * br[k] - ai[k] * bi[k];
                double i = ar[k] * bi[k] + ai[k] * br[k];
                ar[k] = r; ai[k] = i;
            }
            Radix2(ar, ai, true);
            double s = 1.0 / m;
            for (int k = 0; k < n; k++)
            {
                double cr = ar[k] * s, ci = ai[k] * s;
                re[k] = cr * wr[k] - ci * wi[k];
                im[k] = cr * wi[k] + ci * wr[k];
            }
        }
    }
}
