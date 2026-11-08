# GPL.DebugBench

A chamber's Config pages (ADR-0002, DesignSystem section 7): tune parameters on the headset in the **Playground** (a working copy of the active preset), save them as the preset Custom, and run the real protocol with the Playground values (logged `_DEBUG`). Real sessions never use the Playground: they use the active preset. Preset rules: ADR-0002.

**Copy:** this folder + `Core/`, `Params/`, `Session/`, `UI/`. **Dependencies:** GPL.Core, GPL.Params, GPL.Session (Core: `DataFiles`), GPL.UI (TextMeshPro, uGUI).

## Parts
| Part | What |
|---|---|
| `Core/WorkingCopy.cs` | Unity-free model: edits the active preset (`Source`; `CustomName` = "Custom"), `Step(key, ±1)` (numbers by their step inside min..max; Bool toggles; Choice wraps; Text not editable on the headset), `EditedKeys`, `Label` ("Custom · 2 edited"), `Revert`, `Activate(name)` (make active + load), `SaveAs()` (creates or overwrites Custom; makes it active), `CustomExists`, `DeletePreset` (Default becomes active if it was; reloads if the copy was based on it), `Format`. Events `Changed`, `ParamChanged(key, old, new)` (log `param_change`) |
| `Runtime/DebugBench.cs` | pages on the chamber's `UiSurface`: **Config** (Run experiment trials / Practice trial, Playground ›, Presets ›, `ChamberSections`, Log files ›, status line) and **Parameters** (opened by the Playground row; rows per `ParamDef.Group`: label, description or "Source: old value" when edited, value, − / + or a switch; Revert in the header; Save as Custom creates Custom, or overwrites it after a confirmation, and makes it active). No DEBUG tag on these pages |
| `Runtime/DebugBench.Presets.cs` | **Presets** page: Default first, then saved presets; ✓ marks the active one; a sub line marks the working copy. Tap → sheet with ×: Make active (loads it into the working copy; confirms when that discards unsaved edits), Delete (not Default; red text; confirms; deleting the active one makes Default active) |
| `Runtime/DebugBench.Data.cs` | **Log files** page (needs `Files`, a `GPL.Session.DataFiles` of the chamber folder): one one-line row per session (file base name), newest first, red Delete per row (confirms; warns when unfinished), Delete all in the header (one confirmation naming the count). `presets/` and other files are never touched. The Config row shows the count |

The bench runs nothing itself. The chamber handles `RunRequested` / `PracticeRequested` (with `Work.Values`; `PracticeAvailable = false` makes Practice trial inactive), logs `Work.ParamChanged`, and adds its own sections (test playback, display switches, room ...) in `ChamberSections` (a `UiColumn`). Test playback and Debug-only display switches are **not** parameters and are never saved in presets.

## Use
```csharp
var store = new PresetStore(Path.Combine(Application.persistentDataPath, chamberId, "presets"), schema, defaultParams.text);
var work = new WorkingCopy(store);
work.ParamChanged += (key, old, value) => log.Write(LogEvents.ParamChange, ...);
var bench = new DebugBench(surface, canvas.transform, work)
{
    ChamberSections = col =>
    {
        col.Label("Test playback (not saved)");
        var g = col.Group("Playback");
        UiKit.SwitchRow(g, "Play stimulus", "Play stimulus", "2 s on, 1 s off", false, on => player.Loop(on));
    },
    Files = new DataFiles(Path.Combine(Application.persistentDataPath, chamberId), chamberId),
};
bench.RunRequested += () => StartDebugRun(work.Values);
// chamber menu: Config button
bench.OpenDebugPage();
```

Two presets in practice: Default (read-only) and Custom; the store stays general (other `.params` files in the folder are listed too).

Provenance: new (HW2's Debug bench had Inspector-only parameters that did not carry over to real trials).
