# GPL.HearingLoss

Simulated hearing loss on the app's **final left/right output** (after the Meta spatializer, the room and the mixer), one 10-band
peaking filter bank per ear, from fixed presets, manual bands or the ANSI S3.44 prediction. Unofficial port of the NIOSH Hearing Loss
Simulator (HLSim v4.0.29.0) as decoded in `docs/references/HLSIM_GUIDELINES.md`; not endorsed by NIOSH/CDC (PB-09).

**Copy:** this folder (`Core/`, `Runtime/`, `Tests/`). **Dependencies:** none (no other GPL module, no SDK). `Core/` is pure C#.

## Parts
| Part | What |
|---|---|
| `Core/ANSIS344.cs` | `ANSIS344.Apply(PredictionParameters)` → `int[10]` dB HL (−80…+12), float32-faithful; `PredictionParameters` (HLSim defaults: male, 55 y, 25 y at 95 dB(A), fractile 0.5); `HearingLossLevel`; `HearingPresets` Mild / Moderate / Severe (noise notch, −20 / −30 / −40 dB at 4 kHz) |
| `Core/HearingLossState.cs` | the HLSim state machine: `Set(level, prediction)`, `SetManualBand(i, db)` and `ResetManual()` (Manual only; values come back when Manual is selected again), `Changed` |
| `Core/HearingProfile.cs` | immutable `{ Left[10], Right[10] }`; `For(Ear.Both/Left/Right, preset or bands)` (unilateral = other ear normal); `Note()` / `Parse()` = `L=0,0,0,0,-5,-10,-10,-20,-10,0;R=0,…` for presets and logs |
| `Core/PeakingBiquad.cs` | `PeakingBiquad` (Audio EQ Cookbook peaking = Web Audio "peaking", TDF-II, double; `ResponseDb(f, fs)` analytic); `EarFilterBank` (10 in series, `DefaultQ` = {24,12,…,12}, `SetGains`, `SetSampleRate`, `Process(interleaved, channels, channel)`, all-zero gains = bit-exact pass-through) |
| `Core/ToneGenerator.cs` | sine test tone on Left / Right / Both with 10 ms fades, allocation-free |
| `Runtime/OutputHearingLoss.cs` | on the AudioListener (`OutputHearingLoss.Ensure()` adds it there): `SetProfile(profile)`, `Profile`, `PlayTestTone(ear, hz, dbfs, s)` (mixed in **before** the filters), `InputRmsDbfs(ear)` / `OutputRmsDbfs(ear)` of the last block, `Note()` for the log |

## Use
```csharp
var hl = OutputHearingLoss.Ensure();                                   // after the scene's AudioListener exists
hl.SetProfile(HearingProfile.For(Ear.Left, HearingLossLevel.Moderate)); // unilateral, applies at the next audio block
log.Write("hearing_loss", hl.Note());

// Prediction, one ear each
var right = ANSIS344.Apply(new PredictionParameters { Age = 60, Years = 30, ExposureLevel = 100 });
hl.SetProfile(new HearingProfile(HearingPresets.None, right));

// Per-ear check: a 4 kHz tone on the left ear only; read the levels after ~0.5 s
hl.PlayTestTone(Ear.Left, 4000f, -20f, 1f);
// hl.InputRmsDbfs(Ear.Left) ≈ -23 dBFS; hl.OutputRmsDbfs(Ear.Left) ≈ -23 + band gain; right ear silent
```

## Decisions kept from the original (PO, 2026-11; open, may be revisited)
- **1500 Hz Xi = 0.070** copied faithfully (it makes 0.75 / 0.9 fractiles odd at 1.5 kHz, e.g. +12 dB next to −80).
  `Apply(..., clampGainToZero: true)` exists as an option; it is not used by default.
- **Q at 8 kHz = 12** (the effective value; the original intended 24 but the assignment did nothing). `EarFilterBank(fs, q)` takes another table.
- Gain changes are instant (no ramp), as in the original: switching while a sound plays can click.

## Limits (state these in every write-up)
- Models **audibility only**: band attenuation at 10 centre frequencies. No loudness recruitment, no loss of frequency selectivity,
  no temporal effects, no tinnitus.
- **The cuts are wider than "Q = 12" suggests.** With the cookbook / Web Audio peaking formula a cut of g dB has the bandwidth
  term 1/(A·Q) in the denominator (A = 10^(g/40)): at −6 dB the bell stays narrow (1250 Hz between two −6 dB bands: about −0.5 dB),
  but at −20 dB a single 1 kHz band already removes about 6.5 dB at 1250 Hz (calculated, analytic), and −40 dB bands overlap
  strongly. HLSIM_GUIDELINES 9.4 says the midpoint is "nearly unaffected"; that only holds for shallow cuts. The response is exactly
  the original's (same formula), so this is how HLSim sounds too. `EarFilterBank.ResponseDb(f)` gives the real curve.
- Levels are digital (dBFS), not dB HL at the eardrum: the headset volume step decides what is audible, so a "−40 dB" band can still be
  heard at a high volume. The chamber logs the volume step.
- The filters see the final mix: any sound in the app (UI clicks included) goes through them.

## Tests (EditMode, `Tests/EditMode/HearingLossTests.cs`)
The 20 vectors of HLSIM_GUIDELINES 9.1 (exact), defaults, a range grid, ignored inputs, presets, Manual restore, Predicted-null
exception, profile text round trip, sine response at band centres (±0.1 dB vs the set gain) and against the analytic curve, shallow-cut
midpoint, pass-through, channel separation, sample-rate change, tone generator ears and fades.

Provenance: HLSIM_GUIDELINES sections 5 (model, verbatim C#), 6 (state), 7.2–7.3 (filters). Not ported: the two-track player, S/N
slider, spectrum displays and the HLSim UI (a chamber builds its own).
