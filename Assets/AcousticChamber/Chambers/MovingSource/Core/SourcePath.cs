using System;

namespace AcousticChamber.Chambers.MovingSource
{
    public enum PathKind { None = 0, Circle = 1, FlyBy = 2, Approach = 3 }

    /// <summary>
    /// The scripted movements of the source (Docs/Design.md "Movement"), in world coordinates around the head at the moment the path
    /// starts (position and yaw). Pure: <see cref="Advance"/> takes the elapsed time and the speed factor and returns the new position.
    /// <b>Circle</b>: around the head at the start radius and height, 30°/s × speed. <b>FlyBy</b>: left to right, 2 m ahead, from -5 m to
    /// +5 m and back, 1.5 m/s × speed. <b>Approach</b>: along the start azimuth between 6 m and 0.5 m from the head, 1.5 m/s × speed.
    /// FlyBy and Approach go back and forth until stopped.
    /// </summary>
    public sealed class SourcePath
    {
        public const double CircleDegPerS = 30.0;
        public const double LinearMPerS = 1.5;
        public const double FlyByHalfWidthM = 5.0, FlyByAheadM = 2.0;
        public const double ApproachFarM = 6.0, ApproachNearM = 0.5;
        public const double MinCircleRadiusM = 1.0;

        public static readonly double[] SpeedSteps = { 0.25, 0.5, 1.0, 2.0, 4.0 };
        public const double DefaultSpeed = 1.0;

        double _hx, _hz, _yaw, _y;
        double _radius, _azDeg;   // circle / approach
        double _s;                // distance travelled along a back-and-forth path
        double _x, _z;

        public PathKind Kind { get; private set; }

        public double X => _x;
        public double Y => _y;
        public double Z => _z;

        /// <summary>Starts <paramref name="kind"/> from the source at (x, y, z) with the head at (headX, headZ) looking along <paramref name="yawDeg"/>.</summary>
        public void Start(PathKind kind, double headX, double headZ, double yawDeg, double x, double y, double z)
        {
            Kind = kind;
            _hx = headX; _hz = headZ; _yaw = yawDeg; _y = y;
            _s = 0.0;
            double dx = x - headX, dz = z - headZ;
            _radius = Math.Max(MinCircleRadiusM, Math.Sqrt(dx * dx + dz * dz));
            _azDeg = Math.Atan2(dx, dz) * 180.0 / Math.PI; // world azimuth of the start
            _x = x; _z = z;
            if (kind == PathKind.Approach)
            {
                _radius = ApproachFarM; // starts at the far end, on the start's azimuth
                Place(_azDeg, _radius);
            }
            else if (kind == PathKind.FlyBy) PlaceFlyBy();
        }

        public void Stop() => Kind = PathKind.None;

        /// <summary>Moves on by <paramref name="dt"/> seconds at the speed factor; returns false when no path runs.</summary>
        public bool Advance(double dt, double speed)
        {
            switch (Kind)
            {
                case PathKind.Circle:
                    _azDeg += CircleDegPerS * speed * dt;
                    Place(_azDeg, _radius);
                    return true;
                case PathKind.FlyBy:
                    _s += LinearMPerS * speed * dt;
                    PlaceFlyBy();
                    return true;
                case PathKind.Approach:
                    _s += LinearMPerS * speed * dt;
                    Place(_azDeg, ApproachFarM - PingPong(_s, ApproachFarM - ApproachNearM));
                    return true;
                default:
                    return false;
            }
        }

        void Place(double worldAzDeg, double r)
        {
            double a = worldAzDeg * Math.PI / 180.0;
            _x = _hx + r * Math.Sin(a);
            _z = _hz + r * Math.Cos(a);
        }

        void PlaceFlyBy()
        {
            double lx = -FlyByHalfWidthM + PingPong(_s, 2.0 * FlyByHalfWidthM);
            double a = _yaw * Math.PI / 180.0;
            // local (lx, ahead) rotated by the head yaw: forward = (sin yaw, cos yaw), right = (cos yaw, -sin yaw)
            _x = _hx + lx * Math.Cos(a) + FlyByAheadM * Math.Sin(a);
            _z = _hz - lx * Math.Sin(a) + FlyByAheadM * Math.Cos(a);
        }

        /// <summary>Back-and-forth: 0 → length → 0 as <paramref name="s"/> grows.</summary>
        public static double PingPong(double s, double length)
        {
            double m = s % (2.0 * length);
            return m <= length ? m : 2.0 * length - m;
        }
    }
}
