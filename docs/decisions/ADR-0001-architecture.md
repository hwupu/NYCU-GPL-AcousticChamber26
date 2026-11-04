# ADR-0001: Layered, modular architecture with chambers (2026-11-05)

Status: **proposed** (waiting for PO answers in [../Questions.md](../Questions.md)).

**Context.** The project hosts several experiment designs ("chambers") that differ in stimuli, assets, conditions, logging and resume rules, and will be added and removed over time. Features should be reusable in other projects. HW2 (`../115A-535655-HumPer-HW2`) proved the single-scene pattern (pure Core + thin runtime, Magic setup steps, per-row CSV, progress file, onboarding, head gate, RT60 calibration), but it was one design in one scene with hard-coded columns and a fixed resume rule.

**Decision (proposed)** — details in [../Architecture.md](../Architecture.md):
1. Three layers with one-way dependencies: portable modules (`Assets/GPL`) <- App shell (`Assets/App`: Hall, catalog, session routing) <- chambers (`Assets/Chambers/<Id>`).
2. Each module has its own folder, asmdefs (pure `Core` + `Runtime` + `Editor` + `Tests`) and README; it depends only on `GPL.Core` and named external packages.
3. One scene per chamber with its own Meta rig, plus a Hall scene; single-scene loading.
4. Chambers are registered by `ChamberDefinition` assets in a `ChamberCatalog`; removing a chamber is deleting its folder and syncing.
5. Session record (JSON, atomic) with a cursor (stage, block, trial, attempt) and a per-stage `ResumeFrom` policy (`NextTrial`, `BlockStart`, `StageStart`, `SessionStart`, `NotResumable`) plus resume hooks; an active-session pointer lets the app route straight into the chamber.
6. Logs: common columns + chamber-declared columns (explicit factor columns for RM-ANOVA), an `attempt` column, never delete rows; a JSON snapshot per session.
7. Homing as interchangeable `IHomingMethod`s.
8. Hearing loss applied per ear on the AudioListener, after spatialization.

**Consequences.** More up-front structure than HW2; each chamber needs Design/DataFormat/AnalysisPlan before code. Modules can be copied out with `GPL.Core`. Chamber scenes duplicate the rig (accepted for independence).
