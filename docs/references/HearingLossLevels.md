# Hearing-loss levels: values, sources and filter response

Record for academic write-ups (PO 2026-11-08): every loss level the project applies, where each number comes from, and what the
filters actually do to a pure tone. Code and tests named below hold the same numbers; a test fails if they drift from this page.

## Signal chain
Sound sources → Meta XR Audio spatializer → Unity mixer → **`GPL.HearingLoss` `OutputHearingLoss`** on the AudioListener → headset.
The loss is applied to the final left and right output, one filter bank per ear (`Modules/HearingLoss/README.md`). A unilateral
loss leaves the other ear unfiltered (0 dB, bit-exact pass-through).

## Sources
| Item | Source |
|---|---|
| Band centres | 125, 250, 500, 1000, 1500, 2000, 3000, 4000, 6000, 8000 Hz (HLSim) |
| Mild / Moderate / Severe | NIOSH/CDC *Hearing Loss Simulator* (HLSim) web app v4.0.29.0, fixed presets, read from the app (`HLSIM_GUIDELINES.md` section 6). HLSim's labels: "Mild (20 dB HL at 4000 Hz)", "Moderate (30 dB HL at 4000 Hz)", "Moderate/severe (40 dB HL at 4000 Hz)" |
| −60 / −80 | **Ours**, not HLSim: Severe × 1.5 and × 2 (PO choice 2026-11-08, "NIOSH notch, scaled"); the 4 kHz band equals the label. Test panel only |
| Filter | RBJ *Audio EQ Cookbook* peaking EQ (same formula as Web Audio `BiquadFilterNode` "peaking", which HLSim uses), 10 in series per ear, transposed direct form II, double precision |
| Q | 24 at 125 Hz, 12 at the other nine bands (HLSim's effective values; its intended 24 at 8 kHz never took effect, kept as 12, PO 2026-11) |

This is an unofficial port; it is not endorsed by NIOSH/CDC. The 0 dB, −20, −30 and −40 levels are exactly HLSim's.

## Band gains (dB)
| Label | Source | 125 | 250 | 500 | 1k | 1.5k | 2k | 3k | 4k | 6k | 8k |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 dB | none | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| −20 dB | HLSim Mild | 0 | 0 | 0 | 0 | −5 | −10 | −10 | −20 | −10 | 0 |
| −30 dB | HLSim Moderate | 0 | 0 | −10 | −10 | −15 | −20 | −20 | −30 | −20 | −10 |
| −40 dB | HLSim Severe | 0 | 0 | −10 | −20 | −25 | −30 | −30 | −40 | −30 | −20 |
| −60 dB | Severe × 1.5 | 0 | 0 | −15 | −30 | −38 | −45 | −45 | −60 | −45 | −30 |
| −80 dB | Severe × 2 | 0 | 0 | −20 | −40 | −50 | −60 | −60 | −80 | −60 | −40 |

−37.5 at 1.5 kHz in Severe × 1.5 is rounded away from zero to −38 (the filter takes whole dB). The module limit is −80…+12 dB per band.

## What the label means
- The label is the gain of the **4 kHz band**, a digital gain relative to the unprocessed signal. It is not dB HL at the eardrum:
  what is audible also depends on the headset volume step and the Quest's own output path. HLSim calls the same numbers "dB HL".
- **The cut at a tone is not the band value.** Neighbouring peaking filters overlap, and with this formula a deep cut also widens
  (bandwidth term 1/(A·Q), A = 10^(gain/40)). At deep levels the cut at a tone is much larger than the label:

Change of a pure tone, filter bank response at 48 kHz (analytic, dB):

| Label | 500 Hz | 1 kHz | 2 kHz | 4 kHz | 8 kHz |
|---|---|---|---|---|---|
| 0 dB | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| −20 dB | 0.0 | −0.1 | −10.4 | −20.4 | −0.3 |
| −30 dB | −10.1 | −10.5 | −21.3 | −31.3 | −11.0 |
| −40 dB | −10.4 | −21.5 | −33.9 | −43.7 | −22.9 |
| −60 dB | −16.7 | −37.0 | −60.5 | −73.1 | −42.2 |
| −80 dB | −28.2 | −63.6 | −97.8 | −111.4 | −71.1 |

Calculated twice: an independent script of the cookbook formula (2026-11-08) and `EarFilterBank.ResponseDb` in the test below
(agreement ±0.05 dB). The test panel shows the same "expected" value next to the measured change for each tone.

## Where each level is used
| Where | Levels | Code |
|---|---|---|
| HearingLoss chamber (C02, experience) | 0, −20, −30, −40 | `Chambers/HearingLoss/Core/ExperienceState.cs`, presets in `Modules/HearingLoss/Core/ANSIS344.cs` (`HearingPresets`) |
| Hearing Loss Test Panel (Sandbox, PB-09 check) | 0, −20, −30, −40, −60, −80 | `App/Core/TestPanelLevels.cs`, `App/Scripts/HearingLossTestPanel.cs` |

Tests: `Modules/HearingLoss/Tests/EditMode/HearingLossTests.cs` (presets, filter response against sine tones),
`App/Tests/EditMode/TestPanelLevelsTests.cs` (tables above, 4 kHz band = label, −60/−80 = Severe × 1.5/× 2, response table).

## Changes
| Date | Change |
|---|---|
| 2026-11-07 | Mild / Moderate / Severe ported from HLSim v4.0.29.0 (PB-09) |
| 2026-11-08 | −60 / −80 added to the test panel as Severe × 1.5 / × 2 (PO) |
