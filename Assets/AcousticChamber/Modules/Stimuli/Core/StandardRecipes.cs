namespace GPL.Stimuli
{
    /// <summary>
    /// Ready-made recipes: continuous noise for looping (no ramps; seamless because white samples are independent and pink is periodic),
    /// and stimuli reconstructed from published papers (README "Stimuli from the literature"; "assumed" = not reported in the paper).
    /// All at the module default level (-26 dBFS RMS before the ramps) and 48 kHz.
    /// </summary>
    public static class StandardRecipes
    {
        /// <summary>Gaussian white noise, 4 s, no ramps, seed 1: for a looping source.</summary>
        public static readonly StimulusRecipe WhiteLoop = StimulusRecipe.WhiteNoise(4.0, seed: 1, rampS: 0);

        /// <summary>
        /// Gaussian pink noise 20 Hz-20 kHz, 2^18 samples at 48 kHz (5.46 s), no ramps, seed 1: for a looping source. A power-of-two
        /// length takes the fast DFT path (Editor: 61 ms, against about 0.4 s for 4 s).
        /// </summary>
        public static readonly StimulusRecipe PinkLoop = StimulusRecipe.PinkNoise(262144.0 / StimulusRecipe.DefaultSampleRate, seed: 1, rampS: 0);

        /// <summary>Lewald &amp; Guski 2003: 1 kHz tone, 20 ms, 10 ms rise/fall (shape assumed cos²).</summary>
        public static readonly StimulusRecipe LewaldGuski2003Tone = StimulusRecipe.Tone(1000, 0.020, rampS: 0.010);

        /// <summary>Slutsky &amp; Recanzone 2001: 1 kHz tone, 200 ms, 4 ms linear ramps.</summary>
        public static readonly StimulusRecipe SlutskyRecanzone2001Tone = StimulusRecipe.Tone(1000, 0.200, rampS: 0.004, ramp: RampShape.Linear);

        /// <summary>Slutsky &amp; Recanzone 2001: broadband noise, 200 ms, 4 ms linear ramps (distribution assumed Gaussian).</summary>
        public static readonly StimulusRecipe SlutskyRecanzone2001Noise = StimulusRecipe.WhiteNoise(0.200, seed: 1, rampS: 0.004, ramp: RampShape.Linear);

        /// <summary>Zahorik 2002: Gaussian noise burst, 50 ms, 2.5 ms cos² ramps.</summary>
        public static readonly StimulusRecipe Zahorik2002Noise = StimulusRecipe.WhiteNoise(0.050, seed: 2, rampS: 0.0025);

        /// <summary>Ahrens et al. 2019: pink noise burst, 240 ms, 20 ms tapered-cosine ramps.</summary>
        public static readonly StimulusRecipe Ahrens2019Pink = StimulusRecipe.PinkNoise(0.240, seed: 3, rampS: 0.020);

        /// <summary>Finnegan et al. 2016: pink noise burst, 500 ms (ramps not reported: 10 ms cos² assumed).</summary>
        public static readonly StimulusRecipe Finnegan2016Pink = StimulusRecipe.PinkNoise(0.500, seed: 4, rampS: 0.010);

        /// <summary>Bernal-Berdún et al. 2024: white noise 20 Hz-20 kHz, 2 s (ramps not reported: 10 ms cos² assumed).</summary>
        public static readonly StimulusRecipe BernalBerdun2024Noise = StimulusRecipe.WhiteNoise(2.0, seed: 5, rampS: 0.010, bandLowHz: 20, bandHighHz: 20000);

        /// <summary>Mershon &amp; King 1975: white noise, 5 s (ramps not reported: 10 ms cos² assumed).</summary>
        public static readonly StimulusRecipe MershonKing1975Noise = StimulusRecipe.WhiteNoise(5.0, seed: 6, rampS: 0.010);
    }
}
