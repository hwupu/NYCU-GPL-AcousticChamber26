# GPL.Session

Resume an interrupted real session, with a resume policy chosen per stage, and manage a study's data files on the headset.
Generalized from HW2's `SessionProgress` / `ProgressStore` (ADR-0003 of AcousticChamber26).

**Copy:** this folder + `Core/`. **Dependencies:** GPL.Core.

## Files and paths
- Folder: `Application.persistentDataPath/<ChamberId>/` (one folder per chamber; `presets/` inside it holds presets, not data).
- Base name `yyyyMMdd_HHmmss_<ChamberId>[_<StudyId>][_DEBUG]` (`GPL.Core` `DataFileNames`); the base name of a real session is its session ID.
- `<base>.csv`: the event log (`GPL.Logging`), one row per event, flushed per row; a resume appends to it.
- `<base>.session`: the session record below. Real sessions only; Debug runs have a CSV and no session file.
- In AcousticChamber26: Quest `/sdcard/Android/data/com.nycu.AcousticChamber26/files/` (Windows Explorer: `Quest 3 > Internal shared storage > Android > data > com.nycu.AcousticChamber26 > files`; macOS: `adb pull`), Windows Editor `%USERPROFILE%\AppData\LocalLow\NYCU\AcousticChamber26\`.

## Session record (`.session` file, key=value, next to its CSV)
Chamber, version, Study ID, status (`InProgress`, `Completed`, `DroppedOut`, `Superseded`), seed, language, preset name,
parameter hash and **full parameter snapshot** (a resumed session keeps its own parameters), plan hash, cursor, per-block attempt
counters, chamber-specific `Custom` values. Written atomically after every checkpoint (`SessionStore.Save`).

## Cursor and attempts
Protocol tree: session > stage > block > trial (0-based). The protocol calls:
```
record.EnterStage(stageIndex, ResumeFrom.BlockStart);  // policy of this stage
record.BeginBlock();          // returns the attempt number (1, then 2 if the block is redone, ...)
record.TrialFinished();       // after the trial's row is written; then store.Save(record, now)
record.BlockFinished();       // next block
record.StageFinished();       // next stage
```
A trial that was running when the app stopped is not counted; it runs again.

## Resume policies (`ResumeFrom`)
| Policy | Continues at | Attempt |
|---|---|---|
| `NextTrial` | the trial after the last finished one | same attempt |
| `BlockStart` | first trial of the interrupted block | new attempt |
| `StageStart` | first block of the interrupted stage | new attempt per block |
| `SessionStart` | the very beginning (same CSV) | new attempt per block |
| `NotResumable` | cannot resume | — |

```
var r = store.FindUnfinished("S0001");
var point = r.ResolveResume();
r.ApplyResume(point);
if (point.BeginBlock) r.BeginBlock();   // else continue at point.NextTrial
```
Rows of an interrupted attempt stay in the CSV; the analysis uses the highest complete attempt of each block.

## Data manager core (`DataFiles`)
Lists one entry per session file pair (`<base>.csv` + `<base>.session`; Debug runs: CSV only) of ONE chamber folder, with Study ID,
Debug flag, status, size and number of `trial` rows; deletes one entry (both files) or all. Sub-folders (`presets/`) and files of
other chambers or with other names are never touched. The headset UI for it is the Debug bench's Log files page (`GPL.DebugBench`, PB-19c); `Folder` is the listed folder.
