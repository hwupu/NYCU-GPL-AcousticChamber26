using System;

namespace GPL.SpatialSources
{
    /// <summary>
    /// Timing of a pulse train: a sound of <see cref="OnS"/> seconds, then <see cref="OffS"/> of silence, repeated (HW2: 2 s white
    /// noise, 1 s gap, until the participant confirms). Pure: advance it with the frame's (scaled) delta time; it reports when a new
    /// pulse must start, so pausing the clock (Stop dialog, time scale 0) pauses the train.
    /// </summary>
    public sealed class PulseSchedule
    {
        public double OnS { get; }
        public double OffS { get; }
        public double PeriodS => OnS + OffS;

        /// <summary>Time since the train started (s).</summary>
        public double Elapsed { get; private set; }
        /// <summary>Pulses started so far (the first starts at time 0).</summary>
        public int PulsesStarted { get; private set; }
        public bool Running { get; private set; }

        public PulseSchedule(double onS, double offS)
        {
            if (onS <= 0) throw new ArgumentException("onS must be > 0");
            OnS = onS;
            OffS = Math.Max(0, offS);
        }

        /// <summary>Starts the train; true = start the first pulse now.</summary>
        public bool Start()
        {
            Elapsed = 0;
            PulsesStarted = 1;
            Running = true;
            return true;
        }

        public void Stop() => Running = false;

        /// <summary>Advance by <paramref name="dt"/> seconds; true when a new pulse must start in this step.</summary>
        public bool Advance(double dt)
        {
            if (!Running || dt <= 0) return false;
            Elapsed += dt;
            int due = (int)Math.Floor(Elapsed / PeriodS) + 1; // pulses that should have started by now
            if (due <= PulsesStarted) return false;
            PulsesStarted = due; // a long frame skips missed pulses instead of stacking them
            return true;
        }

        /// <summary>True while the current pulse is sounding.</summary>
        public bool IsOn => Running && Elapsed - (PulsesStarted - 1) * PeriodS < OnS;
    }
}
