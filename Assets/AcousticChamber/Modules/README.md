# GPL modules

Generic, portable building blocks for VR perception studies (Unity 6, Meta Quest). Each folder is one module with its own README,
assembly definitions and tests. **To reuse a module in another project, copy its folder plus `Core/`** (every module depends on
`GPL.Core`; any other dependency is listed in the module's README).

| Module | Assembly (pure C#) | What it does | Depends on |
|---|---|---|---|
| [Core](Core/README.md) | `GPL.Core` | clock, atomic files, CSV, invariant formatting, Study ID, deterministic RNG, key=value text, hashes, data file names | — |
| [Params](Params/README.md) | `GPL.Params.Core` | parameter schema, validated parameter sets, presets with an active preset | Core |
| [Logging](Logging/README.md) | `GPL.Logging.Core` | CSV event log: common + chamber columns, flushed per row, safe append on resume | Core |
| [Session](Session/README.md) | `GPL.Session.Core` | session record, cursor, attempts, resume policies, session store, data manager (list/delete file pairs) | Core |
| [UI](UI/README.md) | `GPL.UI.Core` + `GPL.UI` | overlay canvas placement, ray pointer, themed buttons/text (TextMeshPro, English + 中文), number pad (`ITextEntry`), sheets and dialogs, pages, scroll lists, groups and rows, switches, localization | Core, Meta XR Core, uGUI/TMP |
| [Input](Input/README.md) | `GPL.Input.Core` + `GPL.Input` | binding table (action -> controller button), action reader over OVRInput, global Stop action (Y) | Core, Meta XR Core |
| [DebugBench](DebugBench/README.md) | `GPL.DebugBench.Core` + `GPL.DebugBench` | a chamber's Debug pages: working copy of a preset (step, revert, save, save as), Debug page (Run, Parameters, Presets, chamber sections, Data), Parameters page | Core, Params, UI |

Planned (Architecture section 4): Protocol, Homing, Acoustics, HearingLoss, SpatialSources, Responses.

Conventions
- Folder layout: `Core/` (pure, `noEngineReferences: true`), `Runtime/` (Unity), `Editor/`, `Tests/EditMode/`, `README.md`.
- Namespaces `GPL.<Module>`; assemblies `GPL.<Module>.Core`, `GPL.<Module>`, `GPL.<Module>.Editor`, `GPL.<Module>.Tests` (Core module: `GPL.Core`).
- No module references the App, a chamber, or another module except `GPL.Core` (unless its README says so).
- Tests: `Tools/Verify-Headless.ps1` (from the AcousticChamber26 repo) or Unity Test Runner > EditMode.
