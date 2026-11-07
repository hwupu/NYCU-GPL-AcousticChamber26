# GPL.Responses

Participant responses and what they record. For now the method-of-adjustment response in azimuth from HW2: hold the left trigger
to move a sound left, the right trigger to move it right, confirm with A (PB-08c).

**Copy:** this folder + `Core/` and `Input/`. **Dependencies:** GPL.Core, GPL.Input (actions `trigger_left`, `trigger_right`,
`confirm`; declare them in the chamber's binding table). It does not move anything itself: the chamber moves its sound in a
callback (e.g. GPL.SpatialSources `PlacedSource.SetAzimuth`), so the two modules stay independent.

## Parts
| Part | What |
|---|---|
| `Core/AzimuthAdjust.cs` | pure state: one side held moves at `RateDegPerS`, both or none hold still, clamped to ±`LimitDeg`, confirm accepted after `ConfirmIgnoreS`; counts reversals; time from the deltas it is given (paused frames add nothing). `AdjustSettings.HW2` = 10°/s, ±90°, 0.3 s |
| `Core/AzimuthAdjust.cs` `AdjustResult` | start, final, target, duration, reversals, at the limit; `SignedErrorDeg` = final − target; `Note()` = `start_deg=60 final_deg=12.5 target_deg=7.5 duration_s=4.812 reversals=1 at_limit=0` |
| `Runtime/TriggerAdjust.cs` | the coroutine over `AzimuthAdjust` reading GPL.Input; scaled time; while the Stop dialog is open (time scale 0, or a `paused` function) and on the frame it closes, nothing moves and no confirm is taken |

The chamber writes the measures into its own columns (HumPerHW2: `audio_final_az_deg`, `signed_error_deg`, `duration_s`);
derived measures (stop-short, absolute error) are described in the chamber's DataFormat, not logged.

Not built yet (Architecture section 4): pointing ray, place-a-marker, button choice; elevation and distance responses.

## Use
```csharp
var adjust = new TriggerAdjust(Actions.Reader);
AdjustResult result = default;
yield return adjust.Run(audioStartAz, visualAz, AdjustSettings.HW2, az => stimulus.SetAzimuth(az), r => result = r);
```

Provenance: HW2 `ExperimentFlow.RunTrial` (trigger loop, 0.3 s confirm guard, duration) and `Maths.PolarMath.StepAzimuth`.
