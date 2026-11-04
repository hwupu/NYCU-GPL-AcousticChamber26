# ADR-0001: Layered, modular architecture with independent chambers (2026-11-05)

Status: **accepted** (PO answers round 1, [../Questions.md](../Questions.md)).

**Context.** The project hosts independent chambers (studies, explorations, demos) that differ in stimuli, assets, parameters, input bindings, logging and resume rules, and are added and removed over time. Features should be reusable in other projects. HW2 (`../115A-535655-HumPer-HW2`) proved the pattern (pure Core + thin runtime, Magic setup steps, per-row CSV, progress file, onboarding, head gate, RT60 calibration) but was one design with hard-coded columns, a fixed resume rule, and Debug settings that did not reach real trials.

**Decision** (details in [../Architecture.md](../Architecture.md)):
1. Everything we write lives under `Assets/AcousticChamber/` (third-party and Asset Store content stay outside).
2. Three layers, dependencies downward only: `Modules/` (generic, `GPL.<Module>`) <- `App/` (Hall, catalog, routing) <- `Chambers/<Id>/`.
3. Each module: own folder, asmdefs (pure `Core`, `Runtime`, `Editor`, `Tests`), README; depends only on `GPL.Core` and named packages. Modules stay plain folders.
4. HW2 is refactored: generic parts into modules, HW2-specific parts into a `HumPerHW2` chamber that rebuilds the study.
5. One scene per chamber with its own Meta rig, plus a Hall scene; chambers registered by `ChamberDefinition` assets in a `ChamberCatalog`.
6. Logs: common columns + chamber-declared columns, explicit factor columns, `attempt` column, never delete rows, a session file (key=value) with the parameter snapshot, next to its CSV.
7. Homing methods are interchangeable (`IHomingMethod`); posture (seated, standing, room-scale) is per chamber with a handover stage.
8. Hearing loss on the final stereo output, per ear.

**Consequences.** More structure up front; each chamber needs Design/DataFormat/AnalysisPlan before code. Chamber scenes duplicate the rig (accepted for independence).
