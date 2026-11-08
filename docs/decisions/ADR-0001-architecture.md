# ADR-0001: Layered, modular architecture with independent chambers (2026-11-05)

Status: **accepted** (PO answers round 1; the Q&A is in the git history of `docs/Questions.md`).

**Context.** The project hosts independent chambers that differ in stimuli, assets, parameters, bindings, logging and resume rules, and are added and removed over time; features should be reusable in other projects. HW2 (`../115A-535655-HumPer-HW2`) proved the pattern (pure Core + thin runtime, Magic setup steps, per-row CSV, progress file, onboarding, head gate, RT60 calibration) but was one design with hard-coded columns, a fixed resume rule, and Debug settings that did not reach real trials.

**Decision** (details: [../Architecture.md](../Architecture.md)):
1. Everything we write lives under `Assets/AcousticChamber/`.
2. Layers, dependencies downward only: `Modules/` (`GPL.<Module>`) <- `App/` (Hall, catalog, routing) <- `Chambers/<Id>/`.
3. Each module: own folder, asmdefs (pure `Core`, `Runtime`, `Editor`, `Tests`), README; depends only on `GPL.Core`, the GPL modules named in its README (no cycles) and the packages named there. Modules stay plain folders.
4. HW2 is refactored: generic parts into modules, the rest into a `HumPerHW2` chamber.
5. One scene per chamber with its own Meta rig, plus a Hall scene; chambers registered by `ChamberDefinition` assets in a `ChamberCatalog`.
6. Logs: common + chamber-declared columns, explicit factor columns, `attempt` column, never delete rows, a key=value session file with the parameter snapshot next to its CSV.
7. Homing methods are interchangeable (`IHomingMethod`); posture (seated, standing, room-scale) is per chamber with a handover stage. Recentering uses the Meta system recenter (the participant holds the Meta button), never a rig rotation; the rig stays at identity so tracking poses and logged angles share one frame (0° = the participant's forward).
8. Hearing loss on the final stereo output, per ear.

**Consequences.** More structure up front; each chamber needs Design/DataFormat/AnalysisPlan before code. Chamber scenes duplicate the rig (accepted for independence).

Amended 2026-11-06 (PB-05c: system recenter, rig at identity), 2026-11-07 (PB-20b: recenter by the Meta button; modules may depend on other GPL modules named in their README).
