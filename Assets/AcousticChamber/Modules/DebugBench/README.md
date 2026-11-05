# GPL.DebugBench

A chamber's Debug pages (ADR-0002, DesignSystem section 7): tune experimental parameters on the headset in a **working copy**,
save them as presets, and run the real protocol with the working copy (logged `_DEBUG`). Real sessions never use the working
copy: they use the active preset.

**Copy:** this folder + `Core/`, `Params/`, `Session/`, `UI/`. **Dependencies:** GPL.Core, GPL.Params, GPL.Session (Core: `DataFiles`), GPL.UI (TextMeshPro, uGUI).

## Parts
| Part | What |
|---|---|
| `Core/WorkingCopy.cs` | Unity-free model: edits the active preset (`Source`; `CustomName` = "Custom"), `Step(key, ±1)` (numbers by their step on the step grid, inside min..max; Bool toggles; Choice wraps; Text not editable on the headset), `EditedKeys`, `Label` ("Pilot-A · 2 edited"), `Revert` (back to the active preset), `Activate(name)` (make active + load), `SaveAs()` (writes Custom: creates or overwrites; makes it active), `CustomExists`, `DeletePreset` (Default becomes active if it was; the working copy reloads if it was based on it), `Format` (step decimals + unit, On / Off). Events `Changed`, `ParamChanged(key, old, new)` (log `param_change`) |
| `Runtime/DebugBench.cs` | pages on the chamber's `PageStack`: **Debug** (Run experiment trials / Practice trial, Working copy ›, Presets ›, `ChamberSections`, Data files ›, status line) and **Parameters** (rows per `ParamDef.Group`: label, description or "Source: old value" when edited, value, − / + or a switch; Revert in the header; Save as Custom: creates Custom, or overwrites it after a confirmation; Custom becomes active). No DEBUG tag on these pages |
| `Runtime/DebugBench.Presets.cs` | **Presets** page: Default first, then saved presets; ✓ marks the active one; sub line marks the working copy. Tap → sheet with ×: Make active (also loads it into the working copy; confirms when that discards unsaved edits), Delete (not Default, red text; confirms; deleting the active one makes Default active) |
| `Runtime/DebugBench.Data.cs` | **Data files** page (needs `Files`, a `GPL.Session.DataFiles` of the chamber folder): one one-line row per session (the file base name, vertically centered, shrinking to fit), newest first, red Delete per row (confirms; warns when the session is unfinished), Delete all in the header (one confirmation naming the count). Presets (`presets/`) and other files are never touched. The Debug page row shows the count |

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
    Files = new DataFiles(Path.Combine(Application.persistentDataPath, chamberId), chamberId),
};
bench.RunRequested += () => StartDebugRun(work.Values);
// chamber menu: Debug button
bench.OpenDebugPage();
```

## Status
PB-19a: Debug and Parameters pages. PB-19b: Presets page. PB-19c: Data files page.
Two presets in practice: Default (read-only) and Custom. The store stays general: other `.params` files in the folder are listed too.

Provenance: new (HW2's Debug bench had Inspector-only parameters and did not carry over to real trials).
