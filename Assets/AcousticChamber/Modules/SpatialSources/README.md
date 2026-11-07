# GPL.SpatialSources

Spatial sounds placed around the listener: on a circle around an origin (azimuth, radius), moved on that circle, re-placed after a
recenter or fixed in the room; a pulse train; a bed of looping background recordings fixed in the room; a Debug marker that shows
where a sound is. Ported and generalized from HW2 `StimulusController` and `AcousticConditions` (PB-08c).

**Copy:** this folder + `Core/` and `Acoustics/`. **Dependencies:** GPL.Core, GPL.Acoustics (clip levels), Meta XR Audio SDK
(`MetaXRAudioSource`: gain boost, Enable Acoustics). Route every source to the Reflection mixer (GPL.Acoustics `ReflectionMixer`).

## Frame
Azimuth 0 = forward, negative = left, positive = right; position = origin + yaw · (r sin az, 0, r cos az), at the origin's height
(eye height when the origin is the head). Origin and yaw are what the chamber passes, usually the head position at trial start and
the rig forward (the rig stays at identity, GPL.Homing).

## Parts
| Part | What |
|---|---|
| `Core/Polar.cs` | azimuth ↔ (x, z), wrap to (-180, 180], signed difference |
| `Core/PulseSchedule.cs` | pure pulse timing: on / off seconds, advanced with the frame's scaled delta time (a paused clock holds it; a long frame skips missed pulses instead of stacking them) |
| `Runtime/Ring.cs` | position on the circle, facing the origin, yaw of a forward vector, azimuth of a world point |
| `Runtime/UnityGain.cs` | volume 1, flat rolloff, no play on awake (the lab's level rule) |
| `Runtime/PlacedSource.cs` | on the AudioSource: `Place(origin, yaw, radius, az)`, `SetAzimuth(az)` (radius locked), `Replace(origin, yaw)` after a recenter (`Anchoring.Replaced`; `RoomFixed` ignores it), `LevelDbfs` (clip RMS + Meta gain boost), `SetAcoustics(on)` |
| `Runtime/PulseTrain.cs` | plays the source's clip as a pulse train (`Play(onS, offS)`, `Stop()`), scaled time (the Stop dialog holds it) |
| `Runtime/BackgroundBed.cs` | looping sources placed once (`Begin(origin, yaw, radius, azimuths)`) and then held in place every frame; `End`, `Pause(bool)`, `LevelDbfs` (power sum; from `clipLevelsDbfs` measured in the Editor with GPL.Acoustics `WavLevel`, so long clips can stream, or measured in Awake with Decompress On Load), `Note(listener)` = `bg1=-2,0,0;bg2=2,0,0;bg3=0,0,-2 spatial=1` |
| `Runtime/SourceMarker.cs` | Debug marker (an unlit sphere): follows a target or is pinned at a point; save it inactive |

Not built yet (Architecture section 4; added when a chamber needs them): constrained random placement, trajectories,
`MovingSource`, `GrabbableSource`.

## Use
```csharp
// trial: picture and sound on the same circle around the head
var origin = head.position; var yaw = Ring.YawOf(rig.forward);
picture.SetPositionAndRotation(Ring.Position(origin, yaw, 2f, visualAz), Ring.FacingOrigin(Ring.Position(origin, yaw, 2f, visualAz), origin));
stimulus.Place(origin, yaw, 2f, audioStartAz);
pulse.Play(2f, 1f);
// GPL.Responses moves it: adjust.Run(start, visualAz, settings, az => stimulus.SetAzimuth(az), done)
pulse.Stop();

// condition with a background
if (!background.Playing) background.Begin(origin, yaw, 2f, new[] { -90f, 90f, 180f });
log.Write("background", "on " + background.Note(head));
```

Provenance: HW2 `StimulusController` (circle placement, pulse loop, debug sphere), `AcousticConditions` (backgrounds fixed in the
room, power-sum level, position note), `Maths.PolarMath.ToXZ`.
