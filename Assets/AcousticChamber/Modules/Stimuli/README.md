# GPL.Stimuli

Auditory stimuli for perception studies: pure tones and white or pink noise **generated in code** from a recipe (no audio file,
no licence, no repository space), a small **library of credited recordings** shared by all chambers, and the rules for recordings
(preparation, import, level, credit).
Built 2026-11-09 (ADR-0008) from the lab's stimulus catalog and the HW2 audio notes.

**Copy:** this folder + `Core/` (the `Library/` recordings travel with it, with their credits). **Dependencies:** GPL.Core (`Rng`, `Fmt`); Unity audio for `Runtime/` and `Editor/` only. No GPL module depends on it.

## Parts
| Part | What |
|---|---|
| `Core/StimulusRecipe.cs` | immutable recipe: `Tone(hz, dur, ramp, level)`, `WhiteNoise(dur, seed, ramp, level, band)`, `PinkNoise(dur, seed, ramp, level, band)`; `WithSampleRate`, `WithSeed` (token bank); `Name` (`noise_pink_240ms_r20_s3`), `Note()` for logs (`stimulus=noise color=pink dur_s=0.24 ramp_s=0.02 ramp=cos2 rms_dbfs=-26 seed=3 band_hz=20-20000 sr=48000`) |
| `Core/Synth.cs` | `Render(recipe)` → `float[]`: generate, scale the steady signal to the RMS level, ramps, throws instead of clipping; `Gaussian`, `ShapedNoise` (1/f^x in the frequency domain), `Sine` (exact integer phase), `RampGain`, `ApplyRamps`, `RmsDbfs` |
| `Core/Fft.cs` | complex DFT of any length (radix-2, Bluestein otherwise), used for pink and band-limited noise |
| `Core/StandardRecipes.cs` | `WhiteLoop` (4 s), `PinkLoop` (2^18 samples = 5.46 s at 48 kHz), no ramps, for looping sources, and stimuli rebuilt from papers (table below) |
| `Core/StimulusLibrary.cs`, `Core/OpenAir.cs` | catalog of the recordings in `Library/`: id, file, title, creator, source, licence, attribution; `Find(id)`, `AssetPath(entry)`, `entry.Note()` for logs |
| `Runtime/StimulusClips.cs` | `Create(recipe)`, `CreateAtOutputRate(recipe)` → `AudioClip` (mono float PCM in memory), `Leveled(recording, dbfs, out gainDb)` (mono copy of a recording at an RMS level; the file is not changed), `Release(clip)`, `OutputRate` |
| `Editor/LibraryImport.cs` | import rule for `Library/` (PCM, Decompress On Load, sample rate kept); `Magic > Stimuli > Validate Library` (every entry has its file, every file has an entry) |
| `Library/` | the recordings (section "Library") |
| `Tests/EditMode` | FFT vs a naive DFT; white: Gaussian (excess kurtosis ≈ 0), flat; pink: -3.01 ± 0.15 dB/octave, Gaussian, nothing outside 20 Hz-20 kHz, no loop seam; ramp = cos² exactly; level; same seed = same samples; clipping throws; library: unique ids and files, public files present, credits filled, a library WAV imports as PCM and levels to -26 dBFS |

## Use
```csharp
// a chamber's design constant (or built from preset values, ADR-0002)
static readonly StimulusRecipe Burst = StimulusRecipe.PinkNoise(0.240, seed: 3, rampS: 0.020); // -26 dBFS RMS, 48 kHz

AudioClip _clip;
void Start()
{
    _clip = StimulusClips.CreateAtOutputRate(Burst);        // once, at load (not per trial, not on the audio thread)
    source.clip = _clip;
    log.Write("stimulus", Burst.WithSampleRate(_clip.frequency).Note());
}
void OnDestroy() => StimulusClips.Release(_clip);

// token bank: 10 frozen tokens, the trial's token is logged
var tokens = Enumerable.Range(1, 10).Select(s => StimulusClips.Create(Burst.WithSeed(s))).ToArray();
```
Placement and playback stay in `GPL.SpatialSources` (`PlacedSource`, `PulseTrain`, `BackgroundBed`); this module only makes clips.

## Defaults (any of them can be changed per recipe)
| Item | Default | Why |
|---|---|---|
| Distribution | Gaussian | the textbook definition of white noise and what papers mean (Zahorik 2002 and McCracken 2026 say so); pink is Gaussian anyway |
| Pink method | frequency-domain shaping, 20 Hz-20 kHz | exact -3 dB/octave; approximations (Voss-McCartney, Kellet filters) have small ripples |
| Ramps | cos² (raised cosine), 10 ms | smooth at both ends, least spectral splatter; the psychoacoustic standard |
| Level | -26 dBFS RMS of the steady part, before the ramps | exact by construction, what papers report; leaves headroom for Gaussian peaks (about -12 dBFS) and the spatializer |
| Sample rate / format | 48 kHz, 32-bit float, mono | Quest output rate; mono is what the Meta XR spatializer takes; float never clips inside |
| Seed | fixed (frozen noise) | reproducible; a chamber that wants variety uses a token bank of seeds 1..N and logs the token |

## Three choices, explained simply
**1. Distribution: uniform or Gaussian?** A noise clip is a list of 48,000 random numbers per second. *Uniform* is one fair die:
every value from -1 to +1 is equally likely (ffmpeg's `anoisesrc color=white`, excess kurtosis -1.2). *Gaussian* is the total of many
dice: middle values are common, extremes rare (bell curve). Both sound the same ("shhh"): "white" describes the **spectrum** (every
pitch equally strong), and both are flat. The ear splits sound into frequency bands, and inside a band any noise becomes bell-shaped,
so listeners cannot tell them apart. The difference is in the peaks: uniform never exceeds 1.7 × its RMS; Gaussian sometimes reaches
4-5 ×, so it needs headroom. Use Gaussian.

**2. Ramp shape.** A ramp turns the volume up at the start and down at the end. An instant on/off is a jump, and a jump contains every
frequency: a click the participant could use as a cue. *Linear* turns the knob at constant speed (sharp corners at both ends).
*cos²* starts slowly, speeds up and slows into the top: no corners, least click. ffmpeg calls cos² `hsin` or `qsin2` (the same curve,
`(1 - cos πt)/2`; checked in ffmpeg's `afade.c`). Ramp length matters as much as shape: 5-20 ms is typical; below about 2 ms clicks return.

**3. Level: dBFS RMS or LUFS?** Both answer "how loud is the file", with different rulers. *dBFS RMS* is the plain average energy,
every frequency counted equally (physical, exact). *LUFS* (EBU R128 / ITU BS.1770) first applies an ear-shaped K-weighting that
counts highs a little more and deep bass less, so speech, music and noise *sound* equally loud. For noise the two differ.
Calculated from the K-weighting filter (not measured): white noise reads **+3.1 LU** above its dBFS RMS, pink **+0.7 LU**, a 1 kHz
tone **0.0**. So white noise at -26 dBFS RMS ≈ -22.9 LUFS, about the -23 LUFS the HW2 recordings were normalized to. Use dBFS RMS
for synthetic stimuli; use LUFS to match recordings to each other. Neither gives the level at the ear: that is dB SPL, measured.

## How the noise is made
- **White:** independent standard-normal samples (Box-Muller on the GPL.Core `Rng`, which gives the same sequence on every platform).
- **Pink / band-limited:** every frequency bin of the clip's own length gets an independent complex Gaussian, scaled by f^(-1/2)
  (pink) or 1 (white) inside the band and 0 outside; one inverse DFT gives a real Gaussian signal with exactly that spectrum.
  The result is periodic in the clip length, so a looped clip has no seam.
- **Tone:** sine with integer phase arithmetic for whole-hertz frequencies (no drift over long clips); a whole number of cycles loops
  seamlessly (a 200 ms file at 997 Hz holds 199.4 cycles and clicks at the seam; at 1000 Hz it holds 200).
- **Order:** generate → scale the whole buffer to the RMS level → ramps (first and last sample 0) → error if any |sample| > 1.
- **Same recipe = same samples** on the same runtime. Across CPUs `Math.Sin/Log` may differ in the last bit, so log the recipe
  (`Note()`), not a hash of the samples.

## Generated or file?
If a file is uncompressed PCM and holds the same waveform, the listener gets the same samples: no perceptual difference, and the
spatializer treats both alike. The real differences:
1. **Lossy encoding (the main trap).** Unity's default import is compressed (Vorbis). Measured with ffmpeg's encoders (Unity uses
   FMOD's Vorbis: same direction, not the same numbers): a 50 ms Gaussian burst kept 27 / 6 / 4 dB waveform SNR at Vorbis q10 / q5 / q2
   and lost up to 1.3 dB of level and the top of the band; Vorbis q2 collapsed a 250 µs interaural delay in a stereo file to 0 and
   changed clip length by up to 2.7 ms. Generated clips are float PCM in memory, so none of this applies. **Import every stimulus file
   as PCM, Load Type Decompress On Load, no sample-rate override**; long background beds may stream compressed (they are not stimuli).
2. **Loop seams:** see "Tone" above; generated pink noise is periodic.
3. **Frozen vs fresh noise.** A file is one frozen token. Token effects are real in binaural tasks (Freyman et al. 2010; Kohlrausch
   2001); no distance or localization study compared the two directly, and none of the papers below says which it used. Safe design:
   a bank of 10-20 seeded tokens, randomized across trials, token logged.
4. **Timing.** Latency and jitter come from the audio buffer, driver and engine, not from file vs generated. Some HMD + engine setups
   lagged up to about 157 ms with ±11.8 ms jitter (Eckhoff et al. 2024). A clip in memory can be started with
   `AudioSource.PlayScheduled(AudioSettings.dspTime + …)`; for audiovisual-asynchrony studies measure the real onset asynchrony
   (photodiode + microphone) before collecting data.

Practical rule: tones and noise are generated (this module); anything compared against a published result that used a recording
(horn, voice) uses that recording, converted to PCM.

## Levels and gains
- **Software at unity, level set by the headset volume.** Level at the ear = file level × software gains × headset volume ×
  transducer. With every software gain at unity the digital signal is exactly the generated or normalized file, and the only
  variable is the headset volume step, which the chamber logs and the paper reports. Stacked gains (AudioSource volume, mixer
  attenuation, AudioListener volume, Meta Gain Boost, reverb send) make levels irreproducible and risk clipping or lost bit depth.
- Unity: AudioSource volume 1, AudioListener volume 1, mixer group 0 dB, Meta XR Audio Source **Gain Boost 0 dB** (Meta documents 0
  as unity), reverb send at default (the Reflection effect's reverb level is an experimental parameter: keep it fixed, report it).
  `GPL.SpatialSources.UnityGain` applies these.
- **Still measure:** the spatializer changes level with direction (interaural level differences) and reverb adds energy, so measure
  at the real positions in each acoustic condition. Absolute level needs dB SPL at the ear with a coupler / ear simulator and a
  sound-level meter (say whether A-weighted). A phone app with an external calibrated microphone was within about ±1 dB in
  Kardous & Shaw 2016; a phone alone is not a meter. Unity cannot set the Quest system volume: record the step every session.
- -23 LUFS (EBU R128) is a broadcast convention, not a value from the VR literature: any consistent target works. It is useful for
  recordings because it equalizes perceived loudness better than RMS; very different sounds (noise vs speech) still need a pilot.

## Library
Recordings any chamber may use (a module, so no link between chambers); only recordings that may be redistributed. Files are kept **unchanged** as downloaded; a chamber sets
the level at load with `StimulusClips.Leveled(clip, dbfs, out gain)` and logs `entry.Note()` and the gain. Load a clip in a chamber's
Editor step with `AssetDatabase.LoadAssetAtPath<AudioClip>(StimulusLibrary.AssetPath(StimulusLibrary.Find(id)))`.

| Folder | What | Licence | In git |
|---|---|---|---|
| `Library/OpenAIR/` | 7 anechoic recordings from the OpenAIR library (Univ. of York): 4 bicycle horns (0.5-1.5 s), a starter-pistol shot (impulsive, 1.1 s), an operatic voice (4.9 s), a natural baroque trumpet fanfare (14.7 s). Mono, 44.1 / 48 / 96 kHz, 16-bit, 3.7 MB | CC BY-SA (snapshot record CC BY-SA 4.0); credit "www.openairlib.net" + Murphy & Shelley 2010; a changed copy stays CC BY-SA | yes (LFS) |

Taken from the OpenAIR snapshot (doi:10.5281/zenodo.1186905, 575 MB, 2026-11-09): its zip has no central directory (the last file is
cut off), so the entries were read by their local headers and every chosen file's CRC was checked. The snapshot holds only the CC BY-SA
part of OpenAIR: **no speech** (the female voice of the lab's Q032 is not in it; it is probably one of the CC BY-NC files on
openairlib.net, whose site had an expired certificate on 2026-11-09). Not taken: instrument exercises and pieces (viols, bassoons,
trumpets, cornets, a choir pavane; 0.5-17 MB each), ask if a chamber needs them. Not available legally as files: audiocheck.net noise
(generate it), the McCracken Freesound quack (clip not identified), the Q036 engine and Q042 TTS clips, the Hofmann speech corpus.

## Recordings a chamber brings
Chamber-only recordings live in the chamber's own `Audio/` folder (rule 1: chambers never use each other's files); library and
chamber files alike need an entry in `docs/AssetCredits.md` (ADR-0009). Preparation with ffmpeg (tested 6.1):

1. **Inspect** `ffprobe -hide_banner in.mp3`. Dual-mono test: `ffmpeg -i in.mp3 -af "pan=mono|c0=c0-c1,volumedetect" -f null -`
   (max volume at or below about -60 dB = dual mono). Prefer a **dry mono** source: reverb baked into a clip contaminates a reverb
   manipulation.
2. **Cut and convert** to mono 48 kHz 24-bit WAV:
   `ffmpeg -i in.mp3 -ss 00:05:00 -to 00:06:30 -map_metadata -1 -af "pan=mono|c0=0.5*c0+0.5*c1" -ar 48000 -c:a pcm_s24le clip.wav`.
   For true stereo, compare the mix with each channel (`volumedetect`); if the mix is more than about 3 dB below a channel, phase
   cancellation is thinning it: use one channel. MP3 sources: trim the encoder delay/padding from the `iTunSMPB` tag
   (`atrim=start_sample=<delay>:end_sample=<delay+length>,asetpts=PTS-STARTPTS`) and check the duration.
   Keep loops short (60-90 s of 16- or 24-bit mono is 6-13 MB; a 15-minute clip is 125 MB).
3. **Cut first, then normalize each stimulus** (two-pass `loudnorm`, -23 LUFS, true peak -1.5 dBTP, `linear=true`, keep `-ar 48000`
   because `loudnorm` resamples). Pass 1: `-af loudnorm=I=-23:TP=-1.5:LRA=7:print_format=json -f null -`; pass 2 with the measured
   `measured_I/TP/LRA/thresh` and `offset`. LUFS is unreliable on very short clips.
4. **Verify** `-af ebur128=peak=true` (about -23 LUFS, true peak ≤ -1.5 dBTP) and a spectrogram
   (`-lavfi showspectrumpic=s=1200x600:legend=1`) for baked reverb, MP3 low-pass, hum or clicks.
5. **Import** PCM (stimulus: Decompress On Load; long background: Streaming, level measured from the file with
   `GPL.Acoustics.WavLevel`).

## Stimuli from the literature (`StandardRecipes`)
Everything here was read from the papers; "assumed" = not reported. Level is the module default (-26 dBFS RMS); set the SPL the paper
reports at the ear.

| Recipe | Paper (topic) | As reported |
|---|---|---|
| `LewaldGuski2003Tone` | Lewald & Guski 2003 (AV spatial + temporal disparity) | 1 kHz tone, 20 ms, 10 ms rise/fall (shape assumed cos²), 60 dB SPL, five pulses at 1/s |
| `SlutskyRecanzone2001Tone`, `…Noise` | Slutsky & Recanzone 2001 (ventriloquism in time and space) | 200 ms tone (1 kHz) or noise, 4 ms linear ramps, 65 dB SPL (noise distribution assumed Gaussian) |
| `Zahorik2002Noise` | Zahorik 2002 (distance: intensity vs DRR) | Gaussian noise burst, 50 ms, 2.5 ms cos² ramps |
| `Ahrens2019Pink` | Ahrens et al. 2019 (localization with visual information in VR) | pink burst, 240 ms, 20 ms tapered-cosine ramps, 48 kHz, 65 dB SPL ± 3 dB rove |
| `Finnegan2016Pink` | Finnegan et al. 2016 (AV distance compression) | pink burst, 500 ms (ramp assumed) |
| `BernalBerdun2024Noise` | Bernal-Berdún et al. 2024 / 2025 (AV disparity with head rotation) | white noise 20 Hz-20 kHz, 2 s (ramp assumed) |
| `MershonKing1975Noise` | Mershon & King 1975 (distance: intensity and reverberation) | white noise, 5 s, 60 / 80 dB(A) (ramp assumed) |

Not generated: clicks with ITD (Alais & Burr 2004: 1.5 ms clicks), amplitude-modulated noise (Valzolgher et al. 2020: 4 Hz AM white
noise, depth not reported), Berger et al. 2018's broadband "beep" (approximate with band-limited noise 42-15,210 Hz, 190.5 ms, 15 ms
ramps), speech, horn, engine and other recordings.

**The lab's own papers** (Q032-Q036) used recordings, not synthetic stimuli: a 4 s anechoic **horn** (Pätynen et al. 2008, Aalto;
Q033, Q034, Q035), an anechoic **female voice** (OpenAIR; Q032) and a **motorcycle engine** (source not reported; Q036). Clip
length, the exact horn excerpt and the level are not reported: ask the authors for the files before replicating.

## Where recordings may come from (as found 2026-11-09; not legal advice)
The repository is public: anything committed is redistributed. Record every file in `docs/AssetCredits.md` before committing it.

| Source | Licence / terms | In this repo? |
|---|---|---|
| OpenAIR anechoic database (Univ. of York; openairlib.net/anechoicdb, Zenodo 1186905) | per file CC BY-SA or CC BY-NC | yes, with credit; a trimmed or normalized file is an adaptation (BY-SA stays BY-SA). 7 files in `Library/OpenAIR` |
| Freesound | per clip CC0, CC BY or CC BY-NC | yes, with credit for BY / BY-NC |
| LibriVox (public-domain audiobooks, several languages) | public domain | yes |
| Aalto anechoic orchestra recordings (Pätynen et al. 2008) | "The samples can be used free for academic research"; MP3 on the page, PCM on request; no redistribution licence | no, until Aalto agrees in writing (not in the library: PO 2026-11-09, not needed for the research) |
| audiocheck.net test files | personal and educational use only | no (generate instead) |
| YouTube videos and podcasts | copyrighted; YouTube's terms do not allow downloading | no |

## What to report in Methods
Generator and distribution, band, sample rate and format, duration (with or without ramps), ramp shape and length, level in dBFS RMS
**and** the measured dB SPL at the ear (weighting), seed policy (frozen token, bank size), the playback chain (PCM, unity gains,
headset volume step, spatializer and HRTF), and for recordings the source, licence and processing.

## Limits
- Render time on the Quest is not measured. Pink noise needs one inverse DFT of the clip length: a power of two (2^18 for `PinkLoop`) is the fast path, other lengths go through Bluestein (Editor: `PinkLoop` 61 ms, a 4 s pink clip about 0.4 s). The test
  `StandardRecipes_AllRender_WithoutClipping` prints Editor times.
- LUFS values above are calculated from the K-weighting filter on ideal spectra, not measured on files or the headset.
- `Leveled` uses the RMS of the whole recording: pauses count, so a sparse sound (a horn call, a shot) comes out louder while sounding
  than a steady noise at the same value. Match such sounds by ear in a pilot or level a trimmed excerpt.
- No LUFS meter or WAV export in the module yet (add when a chamber needs them).

## References
- Ahrens, A., et al. (2019). Sound source localization with varying amount of visual information in virtual reality. *PLOS ONE*. doi:10.1371/journal.pone.0214603
- Alais, D., & Burr, D. (2004). The ventriloquist effect results from near-optimal bimodal integration. *Current Biology*. doi:10.1016/j.cub.2004.01.029
- Berger, C. C., et al. (2018). Generic HRTFs may be good enough in virtual reality. *Frontiers in Neuroscience, 12*. doi:10.3389/fnins.2018.00021
- Bernal-Berdún, E., et al. (2024). Modeling the impact of head-body rotations on audio-visual spatial perception for VR. *IEEE TVCG*. doi:10.1109/tvcg.2024.3372112
- Eckhoff, D., et al. (2024). Temporal precision and accuracy of audio-visual stimuli in mixed reality systems. *PLOS ONE, 19*(1). doi:10.1371/journal.pone.0295817
- Finnegan, D. J., et al. (2016). Compensating for distance compression in audiovisual virtual environments using incongruence. *CHI 2016*. doi:10.1145/2858036.2858065
- Freyman, R. L., et al. (2010). Lateralization of noise-burst trains based on onset and ongoing interaural delays. *JASA, 128*(1). doi:10.1121/1.3436560
- Huisman, T., Ahrens, A., & MacDonald, E. (2021). Ambisonics sound source localization with varying amount of visual information in virtual reality. *Frontiers in Virtual Reality, 2*. doi:10.3389/frvir.2021.722321
- Kardous, C. A., & Shaw, P. B. (2016). Evaluation of smartphone sound measurement applications using external microphones. *JASA, 140*(4). doi:10.1121/1.4964639
- Kohlrausch, A. (2001). The influence of interaural stimulus uncertainty on binaural signal detection. *JASA, 109*(1). doi:10.1121/1.1320472
- Lewald, J., & Guski, R. (2003). Cross-modal perceptual integration of spatially and temporally disparate auditory and visual stimuli. *Cognitive Brain Research*. doi:10.1016/s0926-6410(03)00074-0
- McCracken, M. K., et al. (2026). A sound approach: mitigating distance compression in VR with spatially incongruent real-world sounds. ACM. doi:10.1145/3797256
- Mershon, D. H., & King, L. E. (1975). Intensity and reverberation as factors in the auditory perception of egocentric distance. *Perception & Psychophysics, 18*(6). doi:10.3758/bf03204113
- Murphy, D. T., & Shelley, S. (2010). OpenAIR: an interactive auralization web resource and database. *129th AES Convention*, paper 8226.
- Pätynen, J., Pulkki, V., & Lokki, T. (2008). Anechoic recording system for symphony orchestra. *Acta Acustica united with Acustica, 94*(6). doi:10.3813/aaa.918104
- Slutsky, D. A., & Recanzone, G. H. (2001). Temporal and spatial dependency of the ventriloquism effect. *NeuroReport*. doi:10.1097/00001756-200101220-00009
- Valzolgher, C., et al. (2020). Reaching to sounds in virtual reality. *Neuropsychologia*. doi:10.1016/j.neuropsychologia.2020.107665
- Zahorik, P. (2002). Assessing auditory distance perception using virtual acoustics. *JASA*. doi:10.1121/1.1458027
- ITU-R BS.1770 (loudness, K-weighting); EBU R128. Meta XR Audio SDK for Unity documentation (spatialize, room acoustics, setup).
