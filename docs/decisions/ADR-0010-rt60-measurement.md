# ADR-0010: RT60 is measured after ISO 3382-2 in octave bands, with a report per run (2026-11-15)

Status: **accepted** (PO 2026-11-15: "correct our reverb module ... follow ISO with each octave band ... a report (txt or csv) should
be recorded for traceability, it is an academic project. the module should be flexible and can be re-used by other chambers"; the
answers to Q1-Q12 of that day; "cook it")

**Context.** The rooms are rendered by Meta XR Audio's shoebox with one reflection coefficient. HW2 mapped a target RT60 to a
coefficient with a table measured on the Windows plugin in one room (broadband T30, one position), and scaled it by V/S to other
rooms. The Quest's Android plugin was never measured, the experience chambers used rooms of another size, the measurement was not
in octave bands, the hearing-loss filter on the output could be inside a measurement, and nothing was written down per run.
Also found the same day: switching room modeling off did not make the "Dry" room dry.

**Decision**
- **Protocol** (GPL.Acoustics README "RT60 measurement protocol"): ISO 3382-2:2008 engineering method, integrated impulse response
  method (impulse + Schroeder), octaves 125 Hz - 4 kHz (IEC 61260-1 base ten, 6th-order Butterworth, time-reversed filtering),
  EDT / T20 / T30 / curvature / validity by peak-to-noise ratio, left and right ear separately plus their mean.
- **Single number**: T_mid = mean of T30 at 500 Hz and 1 kHz; the RT60 targets of all chambers refer to T_mid (PO answer 4a).
- **Omni substitute**: the runner's own probe source with HRTF intensity 0 (PO answer 7).
- **Positions**: 4 source positions (0, -90, +90, 180°, 2 m, ear height), one receiver (the listener at the centre of a
  listener-locked shoebox), 3 repeats. ISO's several receiver positions cannot be had in this renderer; the spread over source
  positions is reported instead of assumed.
- **Output filters**: anything on the AudioListener that implements `GPL.Core.IOutputFilter` (the hearing-loss filter) is bypassed
  while recording; any other audio filter refuses the measurement (PO: "make sure there has no HearingLoss filter active").
- **Room size**: the shoebox has the size of the visible room; a chamber without walls uses the open-space default 12 x 12 x 3.5 m and
  every report says so (PO answer 1).
- **Calibration**: a coefficient sweep on the device (21 coefficients: 0.20-0.99, extension points below 0.50 dropped at import when unusable; 0°, 3 repeats) is imported in the Editor into a table per
  platform and room size. A room is "calibrated" only with such a table for this platform and size; otherwise the chamber shows
  "RT60 not calibrated for this room on this device: values are estimates." under its status (PO answer 11).
- **Report**: every run (measurement, sweep, block start; also cancelled or refused runs) writes `RT60_<chamber>_<time>.csv` (one row
  per condition x position x repeat x ear x band) and `.txt` (method, device, versions, settings, problems, summary) to
  `persistentDataPath/Calibration`. Imported sweeps are kept in `docs/calibration/` next to their table asset.
- **Buttons**: Measure RT60 and Calibrate in the chamber Config (HumPerHW2) and in the experience panels (C01, C02), designed in the
  Storybook first (`Screens/C01 Moving Source`, `Screens/C02 Hidden Source`).
- **Dry** = coefficient 0 on all surfaces, pushed every frame; room modeling stays on.

**Consequences**
- Until the PO runs a sweep on the Quest and imports it, every chamber shows the not-calibrated warning; the HW2 table remains the
  fallback (estimate).
- HumPerHW2's `rt60_measured_s` changes meaning from broadband T20 to T_mid (T30) at one position (DataFormat.md); HW2 itself will be
  replaced by the final-project protocol (PO 2026-11-15), so no further HW2 changes.
- Experience chambers: shoebox 12 x 12 x 3.5 m (was 20 x 20 x 6 and 24 x 24 x 8); sources at 6 m sit at the walls.
- A measurement takes minutes (C01 / C02 one room about 1.5 min, HumPerHW2 three conditions about 3.5 min, a sweep about 6 min); the
  participant should not be in the headset or should stay still and quiet.
- Clause numbers of the standards are summarized, not quoted; the PO checks them against the texts before citing.

**Amendment 2026-11-16** (first Quest reports, `docs/calibration/`, `docs/measures/`; PO: "flag and lower sweep points")
- Sweeps and imports done for 12 x 12 x 3.5 m (C01 / C02) and 8.72 x 15 x 3.5 m (HumPerHW2), Android. Measured T_mid (4 positions x 3
  repeats): 0.7 s targets 0.688-0.690 s, 2.2 s targets 2.152-2.175 s (-1.1 to -2.2 %); Dry has no measurable reverberation.
- Quality flags in the report: curvature |C| > 10 %, a fit with r > -0.98, or a time below the octave-filter limit 4 / B; a condition with
  reflection 0 is reported as no reverberation. The values stay in the CSV; the TXT counts flagged recordings under PROBLEMS.
- Sweep extension points 0.20, 0.30, 0.40 below the HW2 range; the import drops those that are unusable and lists them.
- HumPerHW2 "quiet" (0.3 s) is below the imported table (shortest 0.31 s) and its decay is curved (T_mid 0.278 s, -7.5 %). Not re-swept:
  0.3 s is not used by the final-project protocol (PO 2026-11-16).
- Calibration stays on the device (README "Why on the Quest, not in Play mode").

**Amendment 2026-11-16 (PB-27)** (PO: "New chamber on C04 on Reverberation ... remove the Calibrate button from C01, C02, and C05 ...
a dialog should appear ... csv file can be deleted if cancelled"; planning answers of that day)
- **Calibration has its own chamber**: C04 Reverberation sets the room size (presets of the chambers' rooms or W / D / H steppers), a
  target RT60 profile (targets to check + sweep range: Experience, HW2 = full grid; Short, Long = the grid points expected within 0.7 x
  the shortest .. 1.3 x the longest target, plus a neighbour each side; `Rt60Profiles`) and has Calibrate as its main action.
  HumPerHW2 is relabelled **C05** (ID unchanged, so data and reports are unaffected).
- **Buttons**: Calibrate is removed from C01, C02 and C05; Measure RT60 stays there.
- **Run dialog**: Measure and Calibrate run in a modal dialog (App `Rt60RunDialog`: step, progress bar, time left, borderless Cancel)
  instead of status lines. A calibration ends with "Calibration completed" only; a measurement shows its T_mid per condition; Close.
- **Incomplete runs leave no files** (PO: "Delete all incomplete"): a cancelled, refused or failed run writes no CSV / TXT; the report
  text still goes to the Console. Replaces "also cancelled or refused runs" above. HumPerHW2's `rt60_measured` row then has an empty
  `report=`.
- **Warning text**: "Need to calibrate RT60 before running trials." (`ReverbTuner.UncalibratedWarning`); a warning only, it blocks
  nothing. Reports keep the descriptive note (`UncalibratedNote`).

**Amendment 2026-11-17 (PB-27, headset tables)** (PO: "if i did calibration on quest, can it be applied to other chamber automatically
without re-built? I want an alternative. preserve the process of calibrate -> copy -> import -> build as main protocol"; answers:
button in the dialog, imported wins, separate state; "C04 is used to generate the csv, so no need to show Calibrated")
- **Main protocol unchanged**: calibrate -> copy -> import -> build; only imported tables are committed and count for a study.
- **Alternative**: the "Calibration completed" dialog has **Use on this headset** (Close stays). It copies the sweep CSV to
  `persistentDataPath/Calibration/Tables/RT60_<platform>_<W>x<D>x<H>.csv` (one per size, newest replaces); every `ReverbTuner` reads them
  with the import's parser, so the table equals its imported version. An **imported table of the same platform and size always wins**
  (`Rt60TableChoice`).
- **Separate state**: with only a headset table a chamber shows "RT60 calibrated on this headset only: import it before the study."
  (text2) instead of the warning; reports list it under PROBLEMS; `ReverbTuner.Note()` adds `table=headset` (e.g. HumPerHW2 log notes).
- **C04** shows no table status at all (neither "Calibrated…" nor the warning) and its panel is centered ahead.
