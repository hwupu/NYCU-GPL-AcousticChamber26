# GPL.Acoustics

Room acoustics with the Meta XR Audio shoebox: a target reverberation time through a **measured** coefficient table, the RT60
the plugin actually renders measured from the final output, software clip levels, and the Editor pieces that make the room audible
at all (the Reflection mixer, the audio build-settings guard). Ported and generalized from HW2 (PB-08b).

**Copy:** this folder + `Core/`. **Dependencies:** GPL.Core; Meta XR Audio SDK 85 (`Meta.XR.Audio`: `MetaXRAudioRoomAcousticProperties`,
`MetaXRAudioNativeInterface`, `MetaXRAudioSource`); URP (room materials). Project settings: Spatializer Plugin "Meta XR Audio".

## Three things that must all hold, or there is no reverb
1. Every spatial source is routed to a mixer group with the **"Meta XR Audio Reflection"** effect (`ReflectionMixer.Ensure`, `Route`;
   `Unrouted()` for Validate Setup). Without it sources with Enable Acoustics stay dry.
2. Room modeling and late reverb are switched on (`ReverbTuner` does it in `Awake`; `RoomModelingStatus` for the log).
3. The build uses the committed audio settings (`AudioBuildSettingsGuard`, runs automatically on every build). Meta's Project Setup
   Tool rewrites the live AudioManager (DSP buffer 256, the machine's sample rate); an HW2 APK built that way had no reverb.

## Parts
| Part | What |
|---|---|
| `Core/RoomMath.cs` | box volume / surface, V/S, Eyring coefficient ↔ RT60 (amplitude or energy reading), Sabine absorption. Calculated, not what the plugin renders |
| `Core/Rt60Calibration.cs` | coefficient → RT60 table, log interpolation, V/S scaling to another room size, clamps to the table; `MetaXrAudioShoebox` = the HW2 measurement (Windows plugin, 8.72 × 15 × 3.5 m, 0.30-5.74 s); custom tables via the constructor |
| `Core/Rt60Estimator.cs` | Schroeder backward integration, noise-floor removal, T20 / T30 line fit; `Rt60Result.Note()` = `valid=1 t20=0.712 t30=0.698 depth_db=48` |
| `Core/AudioLevelMath.cs` | RMS dBFS, power sum of uncorrelated sources, gain → dB; `Silent` = -200 |
| `Runtime/ShoeboxRoom.cs` | room size (one size for the visual room and the shoebox), V/S |
| `Runtime/ReverbTuner.cs` | `SetRt60(s)` (uniform coefficient from the table, pushed to the plugin every frame, the shoebox component disabled), `SetReflection(r)`, `UsePreset()`, `ExpectedRt60()`, `PresetRt60()`, `Note()` for the log, `Describe()` for a Debug status |
| `Runtime/Rt60Meter.cs` | on the AudioListener: `Measure(probe, expectedRt60, silenceOthers, done)` plays an impulse through the probe source and records the final mix (`OnAudioFilterRead`), about 5 s |
| `Runtime/ClipLevels.cs` | clip RMS dBFS (needs Decompress On Load), source level = RMS + gain boost + volume |
| `Editor/ReflectionMixer.cs` | create a mixer with the effect on Master (Unity's internal mixer API, by reflection), route sources, list unrouted spatial sources |
| `Editor/RoomBuilder.cs` | `ApplyBakedLighting(settingsPath)` (no directional light, flat ambient, baked GI), `Build(parent, options)` (white box room, area lights, probes, shoebox of the same size; idempotent), `Bake()` |
| `Editor/ClipImport.cs` | `DecompressOnLoad(clip)` |
| `Editor/AudioBuildSettingsGuard.cs` | `IPreprocessBuildWithReport`, last: sample rate 0 (system), DSP buffer 1024, requested 0 = `ProjectSettings/AudioManager.asset` |

## Use
```csharp
// Editor step (chamber's Magic menu)
var env = /* Environment root */;
RoomBuilder.ApplyBakedLighting("Assets/AcousticChamber/Chambers/<Id>/Settings/<Id>.lighting");
var room = RoomBuilder.Build(env, RoomBuilder.Options.Default("Assets/AcousticChamber/Chambers/<Id>/Materials"));
var group = ReflectionMixer.Ensure("Assets/AcousticChamber/Chambers/<Id>/Audio/<Id>Mixer.mixer");
ReflectionMixer.Route(new[] { stimulusSource, bg1, bg2, bg3 }, group);

// Run time
reverb.SetRt60(0.7f);                                  // condition
log.Write("block_begin", reverb.Note());
yield return meter.Measure(stimulusSource, 0.7f, silence => background.Pause(silence),
                           r => log.Write("rt60_measured", "target=0.7 " + r.Note()));
```

## Limits
- The table was measured with the **Windows** plugin; re-check with `Rt60Meter` on the headset (HW2 open point).
- Below about 0.3 s the decay is not a clean exponential (direct sound and first reflections dominate): 0.3 s is the lowest target.
- One coefficient on all surfaces and all four bands; clutter 0. The measurement sums both ears and includes the direct sound.
- Levels are software levels, not sound pressure (headset volume step is logged by the chamber).

Provenance: HW2 `Rt60Calibration`, `Rt60Estimator`, `Maths` (`RoomAcousticsMath`, `AudioLevelMath`), `ReverbTuner`, `Rt60Meter`,
`ClipLevels`, `RoomInfo`, `AudioBuildSettingsGuard`, `HumPerSetup` Steps 1, 2, 4 (lighting, room, mixer). HW2-specific condition
mapping (`ConditionAcoustics`) and the background stay in the HumPerHW2 chamber.
