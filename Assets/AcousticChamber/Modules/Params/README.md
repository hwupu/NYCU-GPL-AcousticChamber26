# GPL.Params

Experimental parameters, presets and the active preset (ADR-0002 of AcousticChamber26). Lets the experimenter tune values in a
Config menu, save them as a preset and run real sessions with the active preset, without rebuilding the app.

**Copy:** this folder + `Core/`. **Dependencies:** GPL.Core.

## Concepts
- **`ParamDef`**: one parameter: key (`snake_case`, dots allowed, e.g. `hl.left`), type (`Float`, `Int`, `Bool`, `Choice`, `Text`),
  default, min/max/step, label, unit, group, description. Out-of-range values are **rejected, not clamped**.
- **`ParameterSchema`**: the ordered list of a study's parameters. `Defaults()`, `Parse(text, warnings)` (repairs old presets:
  missing -> default, unknown -> dropped, invalid -> default, every repair reported).
- **`ParameterSet`**: a complete, valid set of values. Typed getters, validated setters, `Serialize()`, `Hash()` (8 hex digits, log it),
  `Clone()`, `DiffKeys()`.
- **`PresetStore`**: `Default` = the study's `default.params` text (shipped with the app, read-only on the device); saved presets are
  `<name>.params` files in a folder; `active.txt` names the active preset; Default is active until another is chosen; deleting the
  active preset makes Default active again. `NextFreeName()` gives `Preset-1`, `Preset-2`, ... for saving from the headset.

## Example
```csharp
var schema = new ParameterSchema(
    ParamDef.Float("rt60_s", 0.7f, 0.3f, 5f, 0.1f, "RT60", "s", "Acoustics").Describe("Target reverberation time"),
    ParamDef.Int("trials_per_block", 6, 1, 50, "Trials per block"),
    ParamDef.Choice("hl.left", "none", new[] { "none", "mild", "moderate", "severe" }, "Hearing loss left"));

var store = new PresetStore(Path.Combine(dataDir, "presets"), schema, defaultParamsTextAsset.text);
var warnings = new List<string>();
ParameterSet p = store.LoadActive(warnings);   // real sessions: always the active preset
float rt60 = p.GetFloat("rt60_s");

var working = p.Clone();                        // Debug edits a working copy
working.Set("rt60_s", 2.2f);
string name = store.NextFreeName();
store.Save(name, working);
store.SetActive(name);
```

## File format (`.params`)
```
# comment
rt60_s=0.7
trials_per_block=6
hl.left=none
```
Provenance: new (HW2 had Inspector fields only).
