# Auditory stimuli catalog for Unity re-implementation

*Built 2026-11-09. Scope: localization, distance, audiovisual (AV) disparity. Every stimulus detail below was read from the paper's own text (PDF in this library, or Scite full text) unless marked **(not reported)**. DOIs were checked with Scite `verify_papers` (20 + 7 verified, no retractions, no predatory-journal flags). "Cited by" = Scite citing publications on 2026-11-09.*

**Metaphor.** A WAV file is a *photograph* of a sound; a synthesized stimulus is a *recipe* you cook fresh each time. Same recipe + same seed = the same dish. Most "file vs synthetic" differences come from how the photo was *saved* (compression, loop seams), not from the photo-vs-recipe idea itself (Section 4).

## 0. What the lab uses (one-paragraph summary)

Lab papers (Lin, Babu, R. Venkatakrishnan, Ro. Venkatakrishnan) study **auditory distance via reverberation time (RT)** and its **calibration**, plus room size and head-tracking. Three of the five audio papers (Q033, Q034, Q035) use the **same 4-second anechoic horn clip** (Pätynen et al.), rendered with **Steam Audio** or pre-reverberated with **Waves IR1** convolution; the other two use a female voice (Q032) and an engine noise (Q036). Nothing in the lab set uses pure tones, AV *temporal* asynchrony, or synthetic noise bursts. That is the gap Section 2 fills.

---

## 1. LIST A, our lab

| ID | Paper | Topic | Auditory stimulus (as reported) | Source / license | Rendering in the paper |
|---|---|---|---|---|---|
| Q032 | Huang et al. 2021, ACM SAP, doi:10.1145/3474451.3476236 | **Distance** (reverb vs distance compression); AV 2AFC "which looks nearer" | Anechoic **female-voice clip** (length **not reported**); reverb added offline with Waves IR1 convolution in Adobe Audition. RT = 0 (anechoic), 0.7, 1.1, 1.5, 1.9, 2.3 s, plus a 3.2 s extreme trial; increments of 0.4 s over the room's optimal RT of 0.7 s. Object distances 1, 3, 5, 7, 9 m. | **OpenAIR anechoic database** (Univ. of York AudioLab; Murphy & Shelley 2010). Per-file CC BY-SA or CC BY-NC (check the file's page). Download: openairlib.net/anechoicdb, or the Zenodo snapshot record 1186905. | Unity; Oculus Rift CV1 + earphones |
| Q034 | Lin, Wang, Wu et al. 2022, IEEE VR, doi:10.1109/vr51125.2022.00042 | **Distance**, calibration and 1-/6-month carryover (auditory only) | **4 s anechoic horn** recording; Waves IR1 reverb, 10 RTs from 0.2 to 2.2 s in 0.2 s steps; intensity held constant. Calibration: 3/5/7/9/11 m ↔ RT 0.6/1.0/1.4/1.8/2.2 s. Pre/post-test: 2/4/6/8/10 m ↔ RT 0.4/0.8/1.2/1.6/2.0 s. Scene "ideal" RT is 0.7 s; RTs deliberately exaggerated. | Pätynen, Pulkki & Lokki 2008 (Aalto). See Section 5. Exact horn track / excerpt **not reported**. | **Unreal 4.23.1**; Vive Pro built-in noise-cancelling earphones |
| Q033 | Lin, Venkatakrishnan et al. 2024, IEEE VR, doi:10.1109/vr58804.2024.00089 | **Distance** calibration vs environment visibility (dark vs light room); absolute action-based measure | Same **4 s anechoic horn**; **Steam Audio default settings** (spatialization, propagation, reflection, occlusion, transmission, reverb in real time); invisible spherical speakers at targets. Pre/post-test 3/5/7/9/11 m, calibration 2/4/6/8/10 m. Level: "audible but not uncomfortably loud" (**no dB value**). | As Q034 | **Unreal 4.27**; Vive Pro + noise-cancelling headphones; 90 Hz |
| Q035 | Wu, Duffrin, Ro. Venkatakrishnan et al. 2024, IEEE ISMAR, doi:10.1109/ismar62088.2024.00057 | **Room size** from sound vs vision; perception-action calibration | Anechoic **horn**, repeated; Steam Audio with fixed attenuation/air absorption. Walls "brick": scattering 0.05, transmission 0.015, absorption 0.03/0.04/0.07 at 400/2500/15000 Hz. Room sizes 6, 10, 14, 18, 22 m. Personal HRTF built from each participant's ear anthropometry. | As Q034 | **Unity 2019.4.4**; Audio-Technica ATH-M20x |
| Q036 | Tolchinsky, R. Venkatakrishnan et al. 2023, IEEE ISMAR, doi:10.1109/ismar59233.2023.00124 | **Localization / tracking** of moving targets (vision, audio, or both) | **Motorcycle engine noise** (clip source and duration **not reported**), spatialized by ITD + SteamVR default HRTF, distance attenuation, Doppler via the physics engine; audio and visual always congruent. | **Not reported**: ask the authors | HMD at 90 Hz, SteamVR audio |
| Q042 | Ro. Venkatakrishnan et al. 2024, IEEE TVCG, doi:10.1109/tvcg.2023.3293405 | *Not perception*: auditory **distractor** for cybersickness | 60 **text-to-speech clips** (30 trivia/news snippets × female/male voice), 5–12 s each, via the HMD headphones. | TTS service "Woord" (Zyla Labs); no stimulus license stated | HMD headphones |

**Lab gaps for your replication:** Level (dB SPL) is not stated in any lab paper; the Q032 clip length, the Q036 sound file, and the exact horn excerpt are not reported. Ask Rohith or Wan-Yi Lin for the actual files; they exist on a lab machine.

---

## 2. LIST B, others (most influential, ranked by Scite citing publications)

Playback column matters for Unity: **real loudspeakers** means you must substitute binaural rendering, which changes the cue set.

| Cited by | Paper | Topic | Auditory stimulus (as reported) | Playback | Source / data |
|---|---|---|---|---|---|
| 2002 | Alais & Burr 2004, *Current Biology*, doi:10.1016/j.cub.2004.01.029 | **AV localization** (ventriloquism, cue weighting) | **Clicks, 1.5 ms**; apparent position set by ITD. Paired with visual Gaussian blobs of 4°–64° (15 ms). Click shape/level **not reported**. | Two loudspeakers at screen edge, ITD-controlled | No stimulus files |
| 354 | Slutsky & Recanzone 2001, *NeuroReport*, doi:10.1097/00001756-200101220-00009 | **AV disparity** (time and space) | **200 ms** tone or noise, **4 ms linear on/off ramp, 65 dB SPL**; also 1 kHz tone (no difference from noise). SOA −250…+250 ms in 50 ms steps; speakers 146 cm away. | Loudspeakers | None |
| 331 | Kolarik et al. 2016 (review), *Atten Percept Psychophys*, doi:10.3758/s13414-015-1015-1 | **Distance cues** background | Review: intensity, direct-to-reverberant ratio, spectrum, binaural cues | n/a | CC BY |
| 274 | Zahorik 2002, *JASA*, doi:10.1121/1.1458027 | **Distance** (intensity vs DRR weighting) | **Gaussian noise burst, 50 ms, 2.5 ms cos² up/down**; and a **male /da/ syllable** (anechoic, 1 m from mic). Convolved with binaural room impulse responses (MLS, 50 kHz) measured at 0.3–13.79 m. | Headphones | None |
| 247 | Mershon & King 1975, *Percept Psychophys*, doi:10.3758/bf03204113 | **Distance** (intensity and reverberation), classic | **5 s white-noise bursts**; A-weighted 60 or 80 dB (exp. 1), 55/65 near and 45/55 far (exp. 2); ramp **not reported**. | Real speakers in rooms | None |
| 222 | Lewald & Guski 2003, *Cogn Brain Res*, doi:10.1016/s0926-6410(03)00074-0 | **AV spatial + temporal disparity** | **1 kHz pure tone, 20 ms, 10 ms rise/fall, 60 dB SPL**; five pulses at 1/s; disparity ±250 ms in 50 ms steps | 11 loudspeakers on a 1.5 m arc, anechoic room | None |
| 106 | Ahrens et al. 2019, *PLOS ONE*, doi:10.1371/journal.pone.0214603 | **Localization** with varying visual info in VR | **Pink-noise burst, 240 ms, 20 ms tapered-cosine ramps, 48 kHz, 65 dB SPL ± 3 dB rove** (MATLAB) | 64-loudspeaker array in an anechoic chamber (real speakers, not headphones) | **Zenodo** doi:10.5281/zenodo.1293059 (perceptual data), 10.5281/zenodo.1185335 (impulse responses); CC BY |
| 70 | Berger et al. 2018, *Front Neurosci*, doi:10.3389/fnins.2018.00021 | **Localization** with generic HRTF + AV training in VR | "Beep-like" sound: **0.042–15.21 kHz, 190.5 ms, 15 ms rise/fall, 19.6 ms silent gap**, repeating, 55 dB SPL; plus an impact sound (150 ms, 0.042–18 kHz). Generic KEMAR HRTF, Unity 5.3.6. | In-ear headphones | None |
| 56 | Finnegan et al. 2016, ACM CHI, doi:10.1145/2858036.2858065 | **AV distance** (incongruence compresses less) | **Pink-noise burst, 500 ms**; distance cue = intensity. SoundScape Renderer (GPL) plugin in Unity; Oculus DK2. | Headphones | None |
| 41 | Valzolgher et al. 2020, *Neuropsychologia*, doi:10.1016/j.neuropsychologia.2020.107665 | **Localization** adaptation, reaching to sounds in VR | **Amplitude-modulated (4 Hz) white-noise bursts, ~60 dB SPL**, sound ~3 s; modulation depth **not reported** | Real speakers aligned with virtual ones | None |
| 17 | Kytö et al. 2015, IEEE ISMAR, doi:10.1109/ismar.2015.18 | **AV localization** (ventriloquism) in AR | **Male voice** saying "This is a point of interest." in a loop; Pure Data audio; generic HRTF | Headphones | None |
| 12 | Bernal-Berdún et al. 2024, *IEEE TVCG*, doi:10.1109/tvcg.2024.3372112; follow-up 2025 ISMAR, doi:10.1109/ismar67309.2025.00053 | **AV disparity** with head/body rotation (2AFC left/right of the visual ball) | **White noise 20–20,000 Hz, 2 s**; 0°, ±5°…±30° offsets; Steam Audio generic HRTF with 5° horizontal resolution (detection thresholds matched that resolution) | Vive Pro headphones | See paper supplement |

**Also relevant, less cited:** Liu et al. 2020 (doi:10.3758/s13414-019-01929-8): pink noise, 44.1 kHz, 60 dB at the target, near 1.5 m / far 12 m, sound 1 or 2 m behind the object. McCracken et al. 2026 (doi:10.1145/3797256): Gaussian white noise at 0 dBFS from audiocheck.net, 60 dB SPL at 2 m, duck quack from Freesound at 62 dB SPL; data on OSF (osf.io/sz24e). Hofmann et al. 2024 (doi:10.17743/jaes.2022.0162): anechoic AV speech corpus, "available from the authors upon request". Eckhoff et al. 2024 (doi:10.1371/journal.pone.0295817): **timing, not content**: some HMD + Unity/Unreal setups showed AV onset lags of up to ~157 ms with jitter up to ±11.8 ms. Calibrate before any AV-asynchrony study.

---

## 3. Synthesizable stimuli: ffmpeg parameters (tested, ffmpeg 6.1.1)

Script: `_scripts/make_audio_stimuli.sh OUTDIR`. All 48 kHz mono 32-bit float WAV. **Level convention:** RMS of the steady signal *before* ramps, here −26 dBFS; set real SPL on the headphones with a coupler and sound-level meter.

| Output | Based on | Command core |
|---|---|---|
| `tone_1k_20ms_r10` | Lewald & Guski 2003 | `aevalsrc=0.0709*sin(2*PI*1000*t):s=48000:d=0.020` → `afade=t=in:st=0:d=0.010:curve=qsin2,afade=t=out:st=0.010:d=0.010:curve=qsin2` (ramp shape assumed cos²; 20 ms with 10 ms ramps has no steady part) |
| `tone_1k_200ms_r4` | Slutsky & Recanzone 2001 | same, `d=0.200`, `afade … d=0.004:curve=tri` (linear, as reported) |
| `noise_white_200ms_r4` | Slutsky & Recanzone 2001 (distribution not reported) | Gaussian recipe below, 200 ms, 4 ms linear |
| `noise_gauss_50ms_r2p5` | Zahorik 2002 | Gaussian recipe, 50 ms, `d=0.0025:curve=qsin2` |
| `noise_pink_240ms_r20` | Ahrens et al. 2019 | `anoisesrc=color=pink:sample_rate=48000:duration=0.240:amplitude=0.5:seed=3` → scale to RMS → `afade … d=0.020:curve=qsin2` |
| `noise_white_2s_r10` | Bernal-Berdún et al. (ramp assumed) | Gaussian recipe, 2 s |
| `noise_white_5s_r10` | Mershon & King 1975 (ramp assumed) | Gaussian recipe, 5 s |

**Three things the tests showed (they would have bitten you later):**
1. **`curve=qsin2` is exactly a cos² (raised-cosine) ramp** (max error 3×10⁻⁸ vs (1−cos πt/T)/2). `curve=tri` is linear. Papers differ, so match each one.
2. **`anoisesrc color=white` is uniformly distributed, not Gaussian** (excess kurtosis −1.20; Gaussian = 0). Zahorik and McCracken specify *Gaussian*. The script sums 12 independent white streams (`amix=inputs=12:normalize=0`, seeds 100·s+1…12), giving excess kurtosis ≈ −0.1 with a flat spectrum. `color=pink` is already near-Gaussian (kurtosis −0.12) and measured **−3.02 dB/octave**.
3. **`seed=N` makes noise frozen and bit-identical across runs** (tested); `seed=-1` gives fresh noise each run. Short pink bursts vary ±1 dB in RMS from sample to sample, so normalize each file if level matters.

**Not synthesizable:** the lab's horn/voice clips, Berger's broadband "beep" (approximate with band-passed noise 42–15,210 Hz), the engine, speech, and quack sounds. Use the files in Section 5.

---

## 4. Second question: audio file vs synthetic (tones, white/pink noise)

**Short answer.** If the file is uncompressed PCM and the waveform is the same, the listener gets the same samples, so there is no perceptual difference. Spatializers (Steam Audio etc.) sit *after* the audio source and treat both identically. Real differences come from five things.

1. **Lossy encoding (the main trap).** Unity's default for imported clips is Vorbis/MP3 ("Compressed"); PCM keeps the sound closest to the original and is light on the CPU (Unity manual, "Audio file compression in Unity"). I measured ffmpeg's encoders (Unity uses FMOD's Vorbis, so expect the same direction, not identical numbers; **verify in Unity** with a null test):

   | Stimulus | Codec | Waveform SNR | Level change | Bandwidth kept |
   |---|---|---|---|---|
   | 1 kHz tone, 20 ms | PCM 16-bit | 75 dB | 0 | full |
   | | MP3 128k / Vorbis q5 | 26.0 / 27.3 dB | −0.45 / −0.03 dB | ok |
   | Gaussian burst, 50 ms | PCM 16-bit | 75 dB | 0 | 24 kHz |
   | | MP3 320k / 128k | 8.0 / 6.6 dB | −0.74 / −1.04 dB | 20.3 kHz |
   | | Vorbis q10 / q5 / q2 | 27.3 / 6.1 / 3.8 dB | 0 / −0.97 / −1.33 dB | 24 / 20.9 / 17.7 kHz |
   | Pink burst, 240 ms | MP3 128k / Vorbis q5 | 14.3 / 12.5 dB | −0.53 / −0.26 dB | 20.1 kHz |

   Reading: noise loses a high-frequency slice (so the RMS you calibrated drifts by up to ~1.5 dB for white noise), and the waveform no longer matches. For **stereo** files, an interaural time difference of 250 µs was preserved by every setting except **Vorbis q2, which collapsed it to 0 µs** (ILD stayed within 0.2 dB). Vorbis also changed clip length by −128/+32 samples (≤2.7 ms) in my ffmpeg round trip, which matters when a 20 ms tone is your whole stimulus. Rule: **import every stimulus as PCM** (or float WAV), "Load Type: Decompress On Load", no sample-rate override.
2. **Loop seams.** A 200 ms tone file looped at 997 Hz holds 199.4 cycles, so the seam jumps 0.59 of full scale while the largest normal sample step is 0.13: an audible click. At 1000 Hz it holds exactly 200 cycles and the seam is 0.000. A runtime oscillator with phase accumulation has no seam. Choose integer cycles for looped file tones.
3. **Frozen vs fresh noise.** A file is frozen noise by construction; a runtime generator is fresh unless you seed it. Token-level effects are real in binaural tasks: lateralization of frozen-token noise trains showed strong onset dominance (Freyman et al., doi:10.1121/1.3436560), and running noise raised binaural detection thresholds through stimulus uncertainty (Kohlrausch, doi:10.1121/1.1320472). I found **no distance or localization study that compared the two directly**, and none of the papers in Section 2 states whether it used one fixed token or fresh noise per trial (Ahrens generated the burst in MATLAB; token count is not stated). Safe design: pre-render a **bank of 10–20 seeded tokens**, randomize across trials, and log token ID.
4. **Level and spectrum calibration.** Synthetic gives exact amplitude by construction; for files, normalize RMS yourself. Distance rolloff, volume, and spatializer gain apply the same to both.
5. **Timing.** Onset latency and jitter come from the audio buffer, driver, and engine, not from file vs synthetic (Eckhoff et al. measured up to ~157 ms lag and ±11.8 ms jitter on some HMD + engine combinations). As I understand Unity's audio engine (not re-verified this session), a pre-loaded clip can be scheduled against the audio clock (`AudioSource.PlayScheduled` with `AudioSettings.dspTime`), whereas a runtime generator works in buffer-sized chunks; check the manual for your version. For AV-asynchrony replications, **prefer pre-rendered PCM and measure the real SOA** with a photodiode + microphone.

**Practical choice for you.**
- Short tones and bursts (≤1 s): synthesize offline with ffmpeg → PCM float WAV → import untouched.
- Continuous noise or tones whose frequency changes at runtime: runtime synthesis is justified; seed it.
- Anything compared against a lab result (horn, voice): use the original file, converted to PCM, so you reproduce the lab's waveform.

---

## 5. Where to get the files, and what you may do with them

| Source | Used by | Download | License / terms (as found) |
|---|---|---|---|
| Aalto "Anechoic recordings of symphonic music" (Pätynen, Pulkki & Lokki 2008, doi:10.3813/aaa.918104) | Q033, Q034, Q035 (horn) | research.cs.aalto.fi/acoustics/virtual-acoustics/research/acoustic-measurement-and-analysis/85-anechoic-recordings.html | Offered free for research; **MP3 coded on the page, PCM on request** (email the authors). No explicit open license found; for redistribution rights contact Aalto. |
| OpenAIR anechoic database (Murphy & Shelley 2010, AES Convention paper 8226) | Q032 (female voice) | openairlib.net/anechoicdb (also openair.hosted.york.ac.uk); Zenodo snapshot record 1186905 | Per-file **CC BY-SA or CC BY-NC**; check the specific file page, and credit "OpenAIR, Univ. of York". |
| Steam Audio SDK + Unity plugin | Q033, Q035, Q036, Bernal-Berdún | github.com/ValveSoftware/steam-audio | **Apache-2.0** (whole SDK and plugins since v4.5.2, Feb 2024) |
| SoundScape Renderer | Finnegan | spatialaudio.net (footnote in the paper) | GPL (as stated in the paper) |
| audiocheck.net noise files | McCracken | audiocheck.net (test tones) | "Personal and educational use only; no commercial use without permission" |
| Ahrens et al. data | Ahrens | Zenodo 10.5281/zenodo.1293059 | CC BY |
| Freesound clips | McCracken (quack) | freesound.org | Per-clip CC license; check each |
| Hofmann et al. speech corpus | Hofmann | From the authors on request | Not public |

---

## 6. Open questions worth an email

1. Which horn file and excerpt (piece, instrument, start time)? What peak/RMS level and what SPL at the ear?
2. Q032: clip length and playback gain. Q036: source of the engine sound.
3. Bernal-Berdún, Mershon & King, Slutsky: ramp shape and level are not stated; I assumed 10 ms cos² for the first two (marked "assumed" in the script).
4. Alais & Burr and Valzolgher: click envelope and AM depth.
5. Steam Audio default vs custom HRTF: Q035 mentions personalised HRTFs; Q033, Q036 and Bernal-Berdún used the default or generic HRTF. Pick one and record it.

## 7. Files

- `_scripts/make_audio_stimuli.sh`: generates Section 3 (verified: levels −26.0 ± 0.1 dBFS, ramps, seeds).
- `_scripts/measure_audio_codec.py`: reproduces the Section 4 table (needs numpy, scipy, ffmpeg with libmp3lame and libvorbis).

## References (MLA)

Ahrens, Axel, et al. "Sound Source Localization with Varying Amount of Visual Information in Virtual Reality." *PLOS ONE*, 2019, doi:10.1371/journal.pone.0214603.
Alais, David, and David Burr. "The Ventriloquist Effect Results from Near-Optimal Bimodal Integration." *Current Biology*, 2004, doi:10.1016/j.cub.2004.01.029.
Berger, Christopher C., et al. "Generic HRTFs May Be Good Enough in Virtual Reality." *Frontiers in Neuroscience*, vol. 12, 2018, doi:10.3389/fnins.2018.00021.
Bernal-Berdún, Edurne, et al. "Audiovisual Disparities in VR: Impact on Spatial Perception." *IEEE ISMAR*, 2025, doi:10.1109/ismar67309.2025.00053.
Bernal-Berdún, Edurne, et al. "Modeling the Impact of Head-Body Rotations on Audio-Visual Spatial Perception for Virtual Reality Applications." *IEEE TVCG*, 2024, doi:10.1109/tvcg.2024.3372112.
Eckhoff, Daniel, et al. "Temporal Precision and Accuracy of Audio-Visual Stimuli in Mixed Reality Systems." *PLOS ONE*, vol. 19, no. 1, 2024, e0295817, doi:10.1371/journal.pone.0295817.
Finnegan, Daniel J., et al. "Compensating for Distance Compression in Audiovisual Virtual Environments Using Incongruence." *Proceedings of CHI 2016*, ACM, 2016, doi:10.1145/2858036.2858065.
Freyman, Richard L., et al. "Lateralization of Noise-Burst Trains Based on Onset and Ongoing Interaural Delays." *JASA*, vol. 128, no. 1, 2010, pp. 320–31, doi:10.1121/1.3436560.
Hofmann, Annika, et al. "Audiovisual Congruence and Localization Performance in Virtual Reality: 3D Loudspeaker Model vs. Human Avatar." *JAES*, vol. 72, no. 10, 2024, pp. 679–90, doi:10.17743/jaes.2022.0162.
Huang, Yi-Hsuan, et al. "Using Audio Reverberation to Compensate Distance Compression in Virtual Reality." *ACM SAP '21*, 2021, doi:10.1145/3474451.3476236.
Kohlrausch, Armin. "The Influence of Interaural Stimulus Uncertainty on Binaural Signal Detection." *JASA*, vol. 109, no. 1, 2001, pp. 331–45, doi:10.1121/1.1320472.
Kolarik, Andrew J., et al. "Auditory Distance Perception in Humans: A Review of Cues, Development, Neuronal Bases, and Effects of Sensory Loss." *Attention, Perception, & Psychophysics*, 2016, doi:10.3758/s13414-015-1015-1.
Kytö, Mikko, et al. "The Ventriloquist Effect in Augmented Reality." *IEEE ISMAR*, 2015, doi:10.1109/ismar.2015.18.
Lewald, Jörg, and Rainer Guski. "Cross-Modal Perceptual Integration of Spatially and Temporally Disparate Auditory and Visual Stimuli." *Cognitive Brain Research*, 2003, doi:10.1016/s0926-6410(03)00074-0.
Lin, Wan-Yi, et al. "An Empirical Evaluation of the Calibration of Auditory Distance Perception under Different Levels of Virtual Environment Visibilities." *IEEE VR*, 2024, doi:10.1109/vr58804.2024.00089.
Lin, Wan-Yi, et al. "Empirical Evaluation of Calibration and Long-term Carryover Effects of Reverberation on Egocentric Auditory Depth Perception in VR." *IEEE VR*, 2022, doi:10.1109/vr51125.2022.00042.
Liu, Hanyu, et al. "Spatially Incongruent Sounds Affect Visual Localization in Virtual Environments." *Attention, Perception, & Psychophysics*, 2020, doi:10.3758/s13414-019-01929-8.
McCracken, Maggie K., et al. "A Sound Approach: Mitigating Distance Compression in Virtual Reality with Spatially Incongruent Real-World Sounds." ACM, 2026, doi:10.1145/3797256.
Mershon, Donald H., and L. Edward King. "Intensity and Reverberation as Factors in the Auditory Perception of Egocentric Distance." *Perception & Psychophysics*, vol. 18, no. 6, 1975, doi:10.3758/bf03204113.
Murphy, Damian T., and Simon Shelley. "OpenAIR: An Interactive Auralization Web Resource and Database." *129th AES Convention*, 2010, paper 8226.
Pätynen, Jukka, et al. "Anechoic Recording System for Symphony Orchestra." *Acta Acustica united with Acustica*, vol. 94, no. 6, 2008, pp. 856–65, doi:10.3813/aaa.918104.
Slutsky, Dorothy A., and Gregg H. Recanzone. "Temporal and Spatial Dependency of the Ventriloquism Effect." *NeuroReport*, 2001, doi:10.1097/00001756-200101220-00009.
Tolchinsky, Mark, et al. "Empirical Evaluation of the Effects of Visuo-Auditory Perceptual Information on Head Oriented Tracking of Dynamic Objects in VR." *IEEE ISMAR*, 2023, doi:10.1109/ismar59233.2023.00124.
Unity Technologies. "Audio File Compression in Unity." *Unity Manual*, docs.unity.com/en-us/engine/6000.5/manual/audio/files/compression.md.
Valzolgher, Chiara, et al. "Reaching to Sounds in Virtual Reality: A Multisensory-Motor Approach to Promote Adaptation to Altered Auditory Cues." *Neuropsychologia*, 2020, doi:10.1016/j.neuropsychologia.2020.107665.
Venkatakrishnan, Roshan, et al. "The Effects of Auditory, Visual, and Cognitive Distractions on Cybersickness in Virtual Reality." *IEEE TVCG*, vol. 30, no. 8, 2024, pp. 5350–69, doi:10.1109/tvcg.2023.3293405.
Wu, Dai-Rong, et al. "Room Size Perception in Virtual Reality by Means of Sound and Vision: The Role of Perception-Action Calibration." *IEEE ISMAR*, 2024, doi:10.1109/ismar62088.2024.00057.
Zahorik, Pavel. "Assessing Auditory Distance Perception Using Virtual Acoustics." *JASA*, 2002, doi:10.1121/1.1458027.
