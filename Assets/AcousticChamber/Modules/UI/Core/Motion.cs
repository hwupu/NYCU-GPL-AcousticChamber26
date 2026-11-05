using System;

namespace GPL.UI
{
    /// <summary>
    /// A CSS-style cubic Bézier timing curve from (0,0) to (1,1) with control points (x1,y1), (x2,y2).
    /// <see cref="Evaluate"/> maps linear progress 0..1 to eased progress.
    /// </summary>
    public readonly struct CubicBezier
    {
        public readonly double X1, Y1, X2, Y2;

        public CubicBezier(double x1, double y1, double x2, double y2)
        {
            if (x1 < 0 || x1 > 1 || x2 < 0 || x2 > 1) throw new ArgumentException("x control points must be in 0..1");
            X1 = x1; Y1 = y1; X2 = x2; Y2 = y2;
        }

        public float Evaluate(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            double s = SolveX(t);
            return (float)Bez(s, Y1, Y2);
        }

        static double Bez(double s, double p1, double p2)
        {
            double u = 1 - s;
            return 3 * u * u * s * p1 + 3 * u * s * s * p2 + s * s * s;
        }

        static double BezDerivative(double s, double p1, double p2)
        {
            double u = 1 - s;
            return 3 * u * u * p1 + 6 * u * s * (p2 - p1) + 3 * s * s * (1 - p2);
        }

        /// <summary>Curve parameter s with x(s) = x (Newton, falling back to bisection).</summary>
        double SolveX(double x)
        {
            double s = x;
            for (int i = 0; i < 8; i++)
            {
                double err = Bez(s, X1, X2) - x;
                if (Math.Abs(err) < 1e-6) return s;
                double d = BezDerivative(s, X1, X2);
                if (Math.Abs(d) < 1e-6) break;
                s -= err / d;
            }
            double lo = 0, hi = 1;
            s = x;
            for (int i = 0; i < 40; i++)
            {
                double v = Bez(s, X1, X2);
                if (Math.Abs(v - x) < 1e-7) break;
                if (v < x) lo = s; else hi = s;
                s = (lo + hi) / 2;
            }
            return s;
        }
    }

    /// <summary>Motion curves and helpers of the design system (DesignSystem section 5).</summary>
    public static class Motion
    {
        /// <summary>"Settle": fast start, long soft landing, no overshoot. cubic-bezier(.16, 1, .3, 1).</summary>
        public static readonly CubicBezier Settle = new CubicBezier(0.16, 1, 0.3, 1);

        /// <summary>Eased progress of a transition that started <paramref name="elapsed"/> seconds ago.</summary>
        public static float Progress(float elapsed, float duration)
        {
            if (duration <= 0f) return 1f;
            return Settle.Evaluate(elapsed / duration);
        }
    }
}
