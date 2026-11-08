# ADR-0002: Parameters, presets and Debug carry-over (2026-11-05)

Status: **accepted** (Q17 answered 2026-11-05). Single source for the preset rules; the Config pages (called Debug in the first version) are described in the [GPL.DebugBench README](../../Assets/AcousticChamber/Modules/DebugBench/README.md).

**Context.** In HW2 the Debug bench settings did not carry over to real trials (on purpose). In practice this made parameter tuning slow: values had to be changed in code or the Inspector and rebuilt.

**Decision**
- Each chamber declares a parameter schema and ships `Config/default.params` (key=value text, in git) = the preset **Default** (read-only).
- Two presets in practice: **Default** and **Custom**, saved on the device (`persistentDataPath/<ChamberId>/presets/`). One preset is active; Default is active until another is made active. The store stays general (more presets possible later).
- Config (Debug mode) edits the **Playground**, a working copy of the active preset (controls generated from the schema). Making a preset active also loads it into the Playground (confirm if unsaved edits are lost); **Revert** goes back to the active preset; **Save as Custom** creates Custom or overwrites it (confirm) and makes it active; **Run experiment trials** runs the real protocol with the Playground values (`_DEBUG` log).
- Real sessions always use the active preset as saved, never unsaved Playground values. The chamber menu shows a non-default active preset under its buttons ("Preset: Custom"; nothing for Default).
- Each session stores the full parameter snapshot, preset name and hash. A resumed session uses its own snapshot, not the current active preset.
- CSV columns `preset`, `params_hash`; Debug changes log `param_change`.
- Debug-only display switches (markers, error feedback) and test playback are not parameters.

**Consequences.** Parameters must be read from the `ParameterSet` at run time, not from Inspector fields. Presets on the headset must be copied off along with the data.

Amended 2026-11-05 (PB-19b: Default + Custom only, Save as Custom), 2026-11-07 (PB-20b: label "Playground", preset line "Preset: Custom" under the menu buttons).
