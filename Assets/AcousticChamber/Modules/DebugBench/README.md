# GPL.DebugBench

A chamber's Debug pages (ADR-0002, DesignSystem section 7): tune experimental parameters on the headset in a **working copy**,
save them as presets, and run the real protocol with the working copy (logged `_DEBUG`). Real sessions never use the working
copy: they use the active preset.

**Copy:** this folder + `Core/`, `Params/`, `UI/`. **Dependencies:** GPL.Core, GPL.Params, GPL.UI (TextMeshPro, uGUI).

## Parts
| Part | What |
|---|---|
| `Core/WorkingCopy.cs` | Unity-free model: loads a preset (`Source`), `Step(key, ±1)` (numbers by their step on the step grid, inside min..max; Bool toggles; Choice wraps; Text not editable on the headset), `EditedKeys`, `Label` ("Pilot-A · 2 edited"), `Revert`, `Save` (not Default: it is `Config/default.params`), `SaveAs` (next free "Preset-N", not made active), `Format` (step decimals + unit, On / Off). Events `Changed`, `ParamChanged(key, old, new)` (log `param_change`) |
| `Runtime/DebugBench.cs` | pages on the chamber's `PageStack`: **Debug** (Run experiment trials / Practice trial, Working copy ›, Presets ›, `ChamberSections`, Data files ›, status line) and **Parameters** (rows per `ParamDef.Group`: label, description or "Source: old value" when edited, value, − / + or a switch; Revert in the header; Save to &lt;preset&gt; with confirmation; Save as… sheet with ×). No DEBUG tag on these pages |

The bench runs nothing itself. The chamber handles `RunRequested` / `PracticeRequested` (with `Work.Values`), logs
`Work.ParamChanged`, and adds its own sections (test playback, display switches, room ...) in `ChamberSections`. Test playback
and Debug-only display switches are **not** parameters and are never saved in presets.

## Use
```csharp
var store = new PresetStore(Path.Combine(Application.persistentDataPath, chamberId, "presets"), schema, defaultParams.text);
var work = new WorkingCopy(store);
work.ParamChanged += (key, old, value) => log.Write(LogEvents.ParamChange, ...);
var bench = new DebugBench(pages, canvas.transform, work)
{
    ChamberSections = col =>
    {
        col.Label("Test playback (not saved)");
        var g = col.Group("Playback", 1);
        UiKit.SwitchRow(g, 0, "Play stimulus", "2 s on, 1 s off", false, on => player.Loop(on));
    },
    DataSummary = () => files.Count + " files",
};
bench.RunRequested += () => StartDebugRun(work.Values);
// chamber menu: Debug button
bench.OpenDebugPage();
```

## Status
PB-19a: Debug and Parameters pages. Next: Presets page (✓ active; tap → sheet: Make active, Edit, Delete), Data files page.
Preset names are the next free "Preset-N" (no letter keypad yet).

Provenance: new (HW2's Debug bench had Inspector-only parameters and did not carry over to real trials).
