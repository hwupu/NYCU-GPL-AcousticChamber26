# GPL.Core

Small, dependency-free utilities every other module uses. Pure C# (`noEngineReferences: true`), unit-tested.

**Copy:** this folder. **Dependencies:** none.

| Type | Purpose |
|---|---|
| `IClock`, `SystemClock`, `ManualClock` | injectable local time (tests, replays) |
| `AtomicFile` | write temp then replace; read falls back to an orphaned temp file after a power-off |
| `Csv` | RFC 4180 escape / join / split one line |
| `Fmt` | culture-invariant numbers (`F` = up to 3 decimals for logs, `R` = round-trip for state files), timestamps `yyyy-MM-ddTHH:mm:ss.fff`, bool parsing |
| `Ids` | file-name-safe IDs (`Sanitize`, `SanitizeToken` without `_`) |
| `StudyId` | number pad digits (1-4) -> `S0001` |
| `Rng` | deterministic generator (same sequence for a seed on every platform), unbiased `Next`, `Shuffle`, `NewSeed` |
| `KeyValueText` | `key=value` lines with `#` comments and escapes (presets, session files) |
| `Hashing` | FNV-1a 64; `Short` = 8 hex digits (parameter hashes) |
| `DataFileNames` | `yyyyMMdd_HHmmss_<Chamber>[_<StudyId>][_DEBUG]` + `.csv` / `.session`; parse back |

Notes
- `Rng` replaces `System.Random` (HW2) because a stored seed must rebuild the identical trial plan after a resume, on any runtime.
  A test pins the sequence for seed 42; if it fails, the generator changed and old sessions would get different plans.
- Provenance: `AtomicFile` (HW2 `ProgressStore.Save`), `Csv`/`Ids` (HW2 `ExperimentLog`), the rest new.
