# ADR-0002: Parameters, presets and Debug carry-over (2026-11-05)

Status: **accepted** (Q17 answered 2026-11-05).

**Context.** In HW2 the Debug bench settings did not carry over to real trials (on purpose). In practice this made parameter tuning slow: values had to be changed in code or the Inspector and rebuilt.

**Decision**
- Each chamber declares a parameter schema and ships `Config/default.params` (key=value text, in git).
- Presets are saved on the device (`persistentDataPath/<ChamberId>/presets/`). One preset is active.
- Debug edits a working copy (controls generated from the schema). Save as preset / Overwrite / Make active / Revert / **Run experiment trials** (real protocol, working copy, `_DEBUG` log).
- Real runs use the active preset. The chamber menu shows it next to the Study ID ("Preset: Pilot-A"). Real sessions always use the active preset, never unsaved Debug values. `Config/default.params` is the preset "Default", active until another preset is made active.
- Each session stores the full parameter snapshot, preset name and hash. A resumed session uses its own snapshot, not the current active preset.
- CSV columns `preset`, `params_hash`; Debug changes log `param_change`.
- Debug-only display switches (markers, error feedback) are not parameters.

**Consequences.** Parameters must be read from the `ParameterSet` at run time, not from Inspector fields. Presets on the headset must be copied off along with the data.

**Amendment (2026-11-05, PO).** Two presets in practice: **Default** (read-only, `Config/default.params`) and **Custom**. The working copy is always the active preset: making a preset active also loads it (confirm if unsaved edits are lost); Revert goes back to the active preset; **Save as Custom** creates Custom or overwrites it (confirm) and makes it active. Debug runs use the working copy, real sessions the active preset as saved. The store stays general (more presets possible later).
