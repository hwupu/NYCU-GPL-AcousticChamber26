# GPL.Acoustics

Room acoustics with the Meta XR Audio shoebox: a target reverberation time through a **measured** coefficient table, the RT60
the plugin actually renders **measured after ISO 3382-2 in octave bands** with a traceable report, software clip levels, and the
Editor pieces that make the room audible at all (the Reflection mixer, the audio build-settings guard). Ported and generalized
from HW2 (PB-08b); measurement protocol PB-26 (ADR-0010).

**Copy:** this folder + `Core/`. **Dependencies:** GPL.Core (incl. `IOutputFilter`); Meta XR Audio SDK 85 (`Meta.XR.Audio`:
`MetaXRAudioRoomAcousticProperties`, `MetaXRAudioNativeInterface`, `MetaXRAudioSource`, `MetaXRAudioSourceExperimentalFeatures`);
URP (room materials). Project settings: Spatializer Plugin "Meta XR Audio".

## Three things that must all hold, or there is no reverb
1. Every spatial source is routed to a mixer group with the **"Meta XR Audio Reflection"** effect (`ReflectionMixer.Ensure`, `Route`;
   `Unrouted()` for Validate Setup). Without it sources with Enable Acoustics stay dry.
2. Room modeling and late reverb are switched on (`ReverbTuner` does it in `Awake`; `RoomModelingStatus` for the log).
3. The build uses the committed audio settings (`AudioBuildSettingsGuard`, runs automatically on every build). Meta's Project Setup
   Tool rewrites the live AudioManager (DSP buffer 256, the machine's sample rate); an HW2 APK built that way had no reverb.

## Room size
- A chamber **with a visible room**: the shoebox has the size of that room (`ShoeboxRoom` on the room root, `ReverbTuner.roomSize`).
- A chamber **without walls** (open space): the shoebox has the default `Rt60Protocol.OpenSpace*` = **12 × 12 × 3.5 m**, locked to the
  listener; `ReverbTuner.roomSize` stays empty and every report says `room_kind = open_space_default` (PO 2026-11-15).
- The shoebox takes one reflection coefficient on all six surfaces and all four plugin bands; clutter 0. Dry = coefficient 0 on all
  surfaces, pushed every frame (switching room modeling off did not make the room dry; 2026-11-15).

## Calibration: when is an RT60 "calibrated"?
`ReverbTuner` maps a target RT60 to a coefficient with a table (`Rt60Calibration`). The room counts as **calibrated** only when an
imported table (`Rt60CalibrationAsset` in a `Resources/RT60Calibration` folder) was measured with **this protocol**, on **this platform**
(`Android` = Quest, `Windows` = Editor / Link) and for **exactly this room size** (`Rt60Calibration.CalibratedFor`). Otherwise the HW2
Windows table (broadband T30, 8.72 × 15 × 3.5 m) is scaled by V/S and every value is an estimate: `ReverbTuner.Calibrated = false`,
`CalibrationStatus` says why, and chambers show `ReverbTuner.UncalibratedWarning` under their status.

### Why on the Quest, not in Play mode
The room is simulated in software, but the software is not the same in the Editor and on the headset:
- **Another plugin build.** Play mode renders with the Windows build of the Meta XR Audio native plugin, the Quest with the Android
  build. Same SDK, separate native libraries; nothing guarantees that they render the same decay.
- **Other audio settings.** The Quest runs at 24 kHz with a 512 × 4 DSP buffer (reports of 2026-11-16); the Editor usually at 48 kHz with
  other buffers. The reverb engine's timing and filters may depend on them.
- **Participants hear the Quest.** A cited RT60 has to describe what reaches the participant's ears, so it is measured from that output.

That is why tables are kept per platform (`Rt60Calibration.CalibratedFor`): a sweep in Play mode is imported as a `Windows` table and
the Quest still says "not calibrated". The old HW2 table (Windows) predicted 0.67 s at coefficient 0.8 where the Quest measured 0.78 s,
but that table was a broadband single number V/S-scaled from another room, so it neither proves nor rules out a platform difference;
only the same sweep on both platforms would. Play mode is fine for development (buttons, report contents, sweep range, listening),
and for an optional one-off comparison of both platforms that a paper may cite. The headless verify cannot measure at all (no audio
device in batch mode).

Workflow for a room size: on the headset open **C04 Reverberation** (PB-27), set the room size and a profile, press **Calibrate** (the
sweep below; the full grid takes about 6 min) -> pull the report pair from
`<persistentDataPath>/Calibration/` (Quest: `/sdcard/Android/data/<package>/files/Calibration/`) -> in the Editor
`Magic > Acoustics > Import RT60 Calibration...` -> it writes `Assets/AcousticChamber/Calibration/Resources/RT60Calibration/RT60_<platform>_<W>x<D>x<H>.asset`
and copies the CSV + TXT to `docs/calibration/` -> commit both. Then **Measure RT60** (C04 with the profile, or in each chamber)
to verify the targets.

**Alternative without a rebuild: headset tables** (PB-27, PO 2026-11-17). The workflow above stays the main protocol. After a C04
sweep, **Use on this headset** copies the CSV to `<persistentDataPath>/Calibration/Tables/RT60_<platform>_<W>x<D>x<H>.csv` (one per
size; `Runtime/Rt60HeadsetTables.cs`). `ReverbTuner.ResolveCalibration` reads them with `FromSweepCsv`, the parser of the import, so
the table is the same; an **imported table of the same platform and size always wins** (`Core/Rt60TableChoice.cs`). With only a headset
table the room is calibrated (`Calibrated` true, `TableSource` = `Headset`, `HeadsetTable`), chambers show
`ReverbTuner.HeadsetTableNote` ("RT60 calibrated on this headset only: import it before the study.", text2) instead of the warning
(`PanelNote`), reports list it under PROBLEMS and `Note()` adds `table=headset`. Headset tables are not in git; import the CSV before a study.

**Profiles** (`Core/Rt60Profiles.cs`, PO 2026-11-16): the targets a C04 measurement checks and the sweep range of a calibration.
Experience 0.7, 2.2 s and HW2 0.3, 0.7, 2.2 s sweep the full grid; Short 0.3-1.0 s and Long 1.5-4.0 s sweep only the grid points
whose expected RT60 (table in use, V/S-scaled) lies within 0.7 x the shortest .. 1.3 x the longest target, plus one neighbour each
side (at least 4 points). A partial sweep imports like a full one; outside its range the coefficient is clamped to the table's ends.

## RT60 measurement protocol (ISO 3382-2:2008, engineering method)
What `Rt60Runner` does, in order. Every value below is also written into the report header, so a report is self-describing.

| Step | What | Why / standard |
|---|---|---|
| Method | **integrated impulse response method**: one impulse, Schroeder backward integration | ISO 3382-2 allows it next to interrupted noise; Schroeder's integral equals the average of infinitely many noise decays. A digital render has no background noise and repeats exactly, so the impulse is the cleaner choice (pink noise is used in real rooms for its per-octave signal-to-noise ratio) |
| Excitation | single-sample impulse, amplitude 0.5, 100 ms into a 200 ms clip (the spatializer fades a new voice in) | |
| Source | the runner's **own probe** `AudioSource`: spatialized, Meta acoustics on, routed to the chamber's Reflection mixer group, **HRTF intensity 0** (`MetaXRAudioSourceExperimentalFeatures`), flat distance curve, Doppler 0, volume 1 | HRTF EQ off = substitute for an omnidirectional microphone; Meta keeps the interaural time and level differences, so the ears still differ in level |
| Positions | azimuths **0, −90, +90, 180°** (0 = ahead, negative = left) at **2 m**, ear height, relative to the head yaw at the start; **one receiver position** (the listener, at the room centre because the shoebox is locked to the listener) | ISO asks for several source **and** microphone positions because real rooms vary in space. Here the receiver cannot move (the room moves with it) and Meta's late reverb is a model; only the early reflections depend on the source position. The spread over the 4 source positions is reported, so the position (in)dependence of the renderer is documented, not assumed |
| Repeats | **3** per position | the render is deterministic: repeats show repeatability |
| Silence | the chamber's other sounds are paused (`Rt60Plan.SilenceOthers`); settle `max(1 s, 1.2 × expected RT60)` before the impulse | |
| Recording | final output at the AudioListener (`Rt60Meter`, `OnAudioFilterRead`), **left and right separately**, `max(3 s, 1.6 × expected + 0.5 s)` | long enough for −60 dB and a noise-floor tail |
| Output filters | every `IOutputFilter` on the listener (e.g. the hearing-loss filter) is **bypassed** while recording and restored; any other audio filter on the listener **refuses** the measurement | the result must be the room, not a hearing simulation; the filter chain found is in the report |
| Bands | octaves **125, 250, 500, 1000, 2000, 4000 Hz** (ISO 3382-2 engineering) plus broadband; exact base-ten mid-band frequencies (IEC 61260-1) | |
| Filters | 6th-order Butterworth band-pass per octave (bilinear, edges pre-warped, unity gain at mid-band; −3 dB at the edges, ≥ 17.5 dB one octave and ≥ 42.5 dB two octaves away, checked by tests), **time-reversed filtering** | time reversal puts the filter's own ringing before the impulse, so short decays in the low bands are not lengthened |
| Evaluation | onset = first sample within 20 dB of the peak; noise floor = mean energy of the last 10 %; truncation where the 10 ms smoothed energy reaches floor + 5 dB; floor subtracted; Schroeder integral; least-squares lines: **EDT** 0 to −10 dB, **T20** −5 to −25 dB, **T30** −5 to −35 dB, each × 60 dB / slope; correlation r of each fit; curvature **C = 100 (T30/T20 − 1) %** | ISO 3382-2 evaluation ranges; C > 10 % flags a non-straight decay |
| Validity | T20 needs a peak-to-noise ratio ≥ 35 dB, T30 ≥ 45 dB (the floor at least 10 dB under the lowest evaluation level) | invalid values are left empty with the reason |
| Quality flags | a valid value is **flagged** (kept, reason in `note`) when: curvature **\|C\| > 10 %** (curved decay); a T20 / T30 fit has **r > −0.98** (non-linear); or T20 / T30 is **below the octave filter limit** T < 4 / B (B = band width; B·T > 4 for time-reversed filtering, > 16 for forward filtering), i.e. the filter's ring-down, not the room. Per condition the TXT counts the recordings whose T_mid bands are flagged (`flagged` line and PROBLEMS); a condition with **reflection 0** (Dry) is reported as "no reverberation is rendered" instead of a time | ISO 3382-2 asks to note curved decays; added after the first Quest reports (2026-11-16): the quiet room (coefficient 0.5) had C −10 to −30 % and Dry showed T_mid 0.024 s, both without a warning |
| Ear mean | mean of the left and right decay times (empty unless both ears are valid) | |
| Single number | **T_mid = mean of T30 at 500 Hz and 1 kHz** (ear mean); the RT60 targets (0.7 s, 2.2 s, ...) refer to T_mid (PO 2026-11-15); the other bands are reported only | |
| Summary | per condition: T_mid (T30 and T20) mean ± SD [min, max] over positions × repeats, deviation from the target, flagged count, mean per band | |

Kinds of runs (`Rt60Plan`):
- **measurement**: the chamber's condition(s), 4 positions × 3 repeats (Measure RT60 buttons in C01, C02, C04, C05).
- **sweep** (`Rt60Plan.Sweep`): the 21 coefficients of `Rt60Protocol.SweepReflections` (0.20, 0.30, 0.40, then HW2's 0.50 ... 0.99)
  or a profile's subset of them, 0° only, 3 repeats -> calibration table (Calibrate in C04 only, PB-27). Points below `CoreSweepFloor` (0.50) are **extension points** for short
  times: the import keeps them only while each lies below the next point and has a valid T_mid, drops the rest and lists them in the
  Console; from 0.50 up the times must rise or the import fails. Tables imported before 2026-11-16 start at 0.50 (shortest about 0.31 s).
  The sweep is at 0° only, the measurement averages 4 positions; 0° gives the longest times, so a 4-position T_mid lands about 1-3 %
  under the target (Quest reports 2026-11-16: −1.1 to −2.2 % at 0.7 and 2.2 s).
- **block_start**: one position, one repeat, at the start of a condition block (HumPerHW2 option); the session log row links the report id.

### Report (traceability)
One pair per **completed** run in `<persistentDataPath>/Calibration/`, named `RT60_<chamber>_<yyyyMMdd-HHmmss>`. A cancelled, refused
or failed run writes **no files** (PB-27, PO 2026-11-16: "Delete all incomplete"); its report text, marked INCOMPLETE, goes to the
Console only. Not participant data (no Study ID).
- **TXT**: kind, chamber, platform, room size and kind, the method lines above, date (local, UTC), device, OS, Unity, app, Meta XR Audio
  native version, spatializer, sample rate, DSP buffer, speaker mode, room-modeling status, calibration status and table, walls and
  clutter, mixer group, output filters, probe, positions, repeats, timing and the reverb note per condition, chamber extras;
  PROBLEMS (e.g. not calibrated, nothing recorded); SUMMARY.
- **CSV** (one row per condition × position × repeat × ear (L, R, mean) × band): `report_id, kind, chamber, platform, room_m, room_kind,
  condition, target_rt60_s, reflection, position, azimuth_deg, distance_m, repeat, ear, band_hz, edt_s, t20_s, t30_s, r_edt, r_t20,
  r_t30, curvature_pct, peak_to_noise_db, edc_depth_db, valid_t20, valid_t30, note`. `band_hz` = `broadband` or the nominal octave;
  empty cells = not evaluable.

## Parts
| Part | What |
|---|---|
| `Core/RoomMath.cs` | box volume / surface, V/S, Eyring coefficient ↔ RT60 (amplitude or energy reading), Sabine absorption. Calculated, not what the plugin renders |
| `Core/Rt60Calibration.cs` | coefficient → RT60 table, log interpolation, V/S scaling to another room size, clamps to the table; metadata (platform, protocol, room); `CalibratedFor(platform, w, d, h)`; `FromSweepCsv(csv, source)`; `MetaXrAudioShoebox` = the HW2 measurement (Windows plugin, broadband T30, 8.72 × 15 × 3.5 m, 0.30-5.74 s, **not** calibrated in the ISO sense) |
| `Core/OctaveFilter.cs` | `OctaveBands` (ISO 3382-2 engineering set, exact base-ten frequencies, edges), `OctaveFilter` (6th-order Butterworth band-pass, `Apply`, `ApplyTimeReversed`, `ResponseDb`) |
| `Core/DecayAnalysis.cs` | `BandDecay` (EDT, T20, T30, r, C, peak-to-noise, EDC depth, validity, flag, note); `DecayAnalysis.Analyze(signal, fs, band)`, `Flag`, `FilterLimitSeconds` |
| `Core/Rt60Protocol.cs` | the protocol constants and texts (positions, repeats, sweep coefficients, open-space size, settle / record times), `Tmid`; `ImpulseResponseAnalysis.Analyze(channel, fs)` (broadband + octaves), ear `Mean` |
| `Core/Rt60Report.cs` | `Rt60Report` (metadata, rows, problems, `Csv()`, `Text()`, `TmidStats`, `BandStats`, `TmidFlaggedCount`, `QualityProblems`, `FileStem`), `Rt60Stats` |
| `Core/Rt60Profiles.cs` | `Rt60Profile` (targets, `FullSweep`, `SweepFor(expectedRt60)`, `TargetName`), `Rt60Profiles` (Experience, HW2, Short, Long), `Rt60Eta` (time left from the plan, corrected by the recordings done; `Text`, `Duration`) |
| `Core/Rt60TableChoice.cs` | `Rt60TableSource` (None, Imported, Headset), `Pick(imported, headset, platform, w, d, h)` (imported wins), `HeadsetFileName`, `SourceName` |
| `Core/Rt60Estimator.cs` | HW2's broadband Schroeder T20 / T30 (kept for the HW2 table's provenance; new measurements use `DecayAnalysis`) |
| `Core/WavLevel.cs` | RMS dBFS of a WAV file on disk (PCM 16 / 24 / 32, float 32, EXTENSIBLE), same value as `ClipLevels.RmsDbfs`; lets long clips stream (GetData does not work on Streaming clips) |
| `Core/AudioLevelMath.cs` | RMS dBFS, power sum of uncorrelated sources, gain → dB; `Silent` = -200 |
| `Core/RoomChoice.cs` | `RoomChoice` Dry / Room (0.7 s) / Hall (2.2 s) and their RT60 (experience chambers); `ReverbTuner.Apply(choice)` / `SetDry()` |
| `Runtime/ShoeboxRoom.cs` | room size (one size for the visual room and the shoebox), V/S |
| `Runtime/ReverbTuner.cs` | `SetRt60(s)` (coefficient from the table, pushed to the plugin every frame, the shoebox component disabled), `SetDry()`, `SetReflection(r)`, `UsePreset()`, `ExpectedRt60()`, `PresetRt60()`, `ResolveCalibration()`, `Calibrated`, `CalibratedTable`, `CalibrationStatus`, `OpenSpace`, `HasRoom`, `SetRoomSize(w, d, h)` (open space only; C04), `ExpectedRt60For(r)`, `UncalibratedWarning` ("Need to calibrate RT60 before running trials.") / `UncalibratedNote` (reports), `TableSource`, `HeadsetTable`, `HeadsetTableNote`, `PanelNote(out warning)`, `Note()` for the log, `Describe()` for a Debug status |
| `Runtime/Rt60Runner.cs` | `Rt60Runner.Begin(plan, progress, done)` runs an `Rt60Plan` (conditions, positions, repeats, mixer group, silence / restore callbacks, extra metadata) and writes the report of a completed run; `Wait()`, `Cancel()`; destroying it restores everything; progress for a dialog: `Step`, `Fraction`, `SecondsLeft`; `Rt60Plan.EstimatedSeconds()`, `Sweep(..., reflections)`; `Rt60RunResult.Summary()` for a status line, `ResultLines()` for a dialog. The dialog itself is App `Rt60RunDialog` (GPL.UI) |
| `Runtime/Rt60Meter.cs` | on the AudioListener (`Ensure()`): `Record(probe, settle, record, done)` plays the impulse and returns the left and right channels |
| `Runtime/Rt60HeadsetTables.cs` | `Use(sweepCsvPath)` (validate + copy to `Calibration/Tables`), `LoadAll()`, `Directory` |
| `Runtime/Rt60CalibrationAsset.cs` | imported table (ScriptableObject in `Resources/RT60Calibration`), `LoadAll()`; `Rt60Platform.Current` |
| `Runtime/ClipLevels.cs` | clip RMS dBFS (needs Decompress On Load), source level = RMS + gain boost + volume |
| `Editor/Rt60CalibrationImport.cs` | `Magic > Acoustics > Import RT60 Calibration...`: sweep CSV -> table asset + report copies in `docs/calibration/` |
| `Editor/ReflectionMixer.cs` | create a mixer with the effect on Master (Unity's internal mixer API, by reflection), route sources, list unrouted spatial sources |
| `Editor/RoomBuilder.cs` | `ApplyBakedLighting(settingsPath)` (no directional light, flat ambient, baked GI), `Build(parent, options)` (white box room, area lights, probes, shoebox of the same size; idempotent), `Shoebox(parent, options)` (the shoebox alone, for a chamber with its own room model; idempotent), `Bake()` |
| `Editor/ClipImport.cs` | `DecompressOnLoad(clip)`, `SetLoadType(clip, type)` |
| `Editor/AudioBuildSettingsGuard.cs` | `IPreprocessBuildWithReport`, last: sample rate 0 (system), DSP buffer 1024, requested 0 = `ProjectSettings/AudioManager.asset` |

## Use
```csharp
// Editor step (chamber's Magic menu)
var env = /* Environment root */;
RoomBuilder.ApplyBakedLighting("Assets/AcousticChamber/Chambers/<Id>/Settings/<Id>.lighting");
var room = RoomBuilder.Build(env, RoomBuilder.Options.Default("Assets/AcousticChamber/Chambers/<Id>/Materials"));
var group = ReflectionMixer.Ensure("Assets/AcousticChamber/Chambers/<Id>/Audio/<Id>Mixer.mixer");
ReflectionMixer.Route(new[] { stimulusSource, bg1, bg2, bg3 }, group);
// open space instead: shoebox.width/depth/height = Rt60Protocol.OpenSpaceWidth/Depth/Height, reverb.roomSize = null

// Run time
reverb.SetRt60(0.7f);                                  // condition
log.Write("block_begin", reverb.Note());               // includes calibrated=0/1
if (!reverb.Calibrated) status.text += "\n" + ReverbTuner.UncalibratedWarning;

// Measure (a Config / panel button)
var plan = new Rt60Plan { Chamber = "<Id>", Reverb = reverb, Output = stimulusSource.outputAudioMixerGroup,
                          SilenceOthers = s => background.Pause(s), Restore = () => reverb.SetRt60(current) };
plan.Conditions.Add(Rt60Condition.Rt60("noisy", reverb, 0.7f));
var runner = Rt60Runner.Begin(plan, line => status.text = line, r => status.text = r.Summary());
yield return runner.Wait();                            // or runner.Cancel()

// in the AcousticChamber app, with the run dialog (progress, Cancel, result): AcousticChamber.App.Rt60RunDialog.Run(canvas, plan)

// Calibrate this room on this device (C04 Reverberation does this; a profile may narrow the sweep)
var sweep = Rt60Plan.Sweep("<Id>", reverb, group, Rt60Profiles.Short.SweepFor(reverb.ExpectedRt60For));
Rt60Runner.Begin(sweep, line => status.text = line, r => status.text = r.Summary());
```

## Limits
- Until a sweep is imported for a room on the Quest, every RT60 is an **estimate** (HW2 Windows table, broadband, V/S-scaled).
- Short times are not clean exponentials: on the Quest, coefficients below about 0.7 give curved decays (C −10 to −60 %: the early
  part decays slower than the late part), so their T_mid depends on the evaluation range; the report flags them. Coefficient 0.5 gives
  about 0.31 s at 0° in both measured rooms; the extension points reach lower.
- One receiver position (see the protocol); HRTF intensity 0 removes the HRTF EQ but not ITD / ILD.
- The low octaves (125, 250 Hz) of a single decay scatter more (few modes in a narrow band, short fit range); the report gives the
  spread over positions and repeats. EditMode tests check the analysis for bias on synthetic decays (mean of 12 noise decays within
  5 % in every band for T30).
- The clause references of ISO 3382-2:2008 / IEC 61260-1 are summarized from the standards, not quoted; check them against the texts
  before citing.
- Levels are software levels, not sound pressure (headset volume step is logged by the chamber, not read here).

Provenance: HW2 `Rt60Calibration`, `Rt60Estimator`, `Maths` (`RoomAcousticsMath`, `AudioLevelMath`), `ReverbTuner`, `Rt60Meter`,
`ClipLevels`, `RoomInfo`, `AudioBuildSettingsGuard`, `HumPerSetup` Steps 1, 2, 4 (lighting, room, mixer). HW2-specific condition
mapping (`ConditionAcoustics`) and the background stay in the HumPerHW2 chamber. ISO protocol, octave analysis, runner, report and
calibration import: PB-26 (2026-11-15).
