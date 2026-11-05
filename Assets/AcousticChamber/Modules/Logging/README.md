# GPL.Logging

CSV event log for experiments: one row per event, long format, flushed after every row (data survives a crash or a headset
power-off), mirrored to the console. Generalized from HW2's `ExperimentLog`.

**Copy:** this folder + `Core/`. **Dependencies:** GPL.Core.

## Columns
Common (every study, in this order):
`event, timestamp_local, elapsed_s, session_id, chamber, chamber_version, study_id, mode, preset, params_hash, stage, block, condition, trial, attempt`
then the study's own columns (declared in `LogSchema`), then `note` (always last).

- `elapsed_s` counts from when the log object was opened (after a resume: from the resume).
- `trial` is 1-based within the block; `attempt` is the block's attempt (a restarted block has attempt 2, ...).
- Writing an undeclared column throws (a unit test catches typos). `event`, `timestamp_local` and `elapsed_s` are written by the log.

## Usage
```csharp
var schema = new LogSchema("visual_az_deg", "audio_final_az_deg", "signed_error_deg");
var ctx = new LogContext { SessionId = id, Chamber = "HumPerHW2", ChamberVersion = "1", StudyId = "S0001",
                           Mode = LogModes.Experiment, Preset = "Default", ParamsHash = p.Hash() };
using var log = EventLog.Open(csvPath, SystemClock.Instance, schema, ctx, mirror: line => Debug.Log("[GPL] " + line));
log.Write(LogEvents.SessionStart, "app 0.1.0, Quest 3, volume step 8");
ctx.Stage = "experiment"; ctx.Block = "1"; ctx.Condition = "quiet"; ctx.Attempt = 1; ctx.Trial = 1;
log.Write(LogEvents.Trial, new LogValues().Set("visual_az_deg", 7.5f).Set("audio_final_az_deg", 9.2f).Set("signed_error_deg", 1.7f));
```
`EventLog.Open` appends to an existing file (resume) without a second header, and refuses if the file's header differs from the schema.

Common event names: `LogEvents` (session_start, session_resume, participant_dropout, block_begin, block_restart, trial, homing, recenter, ...).
