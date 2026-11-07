# ADR-0006: Experience chambers (2026-11-07)

Status: **accepted** (PO, PB-10 planning: "only experience the effect ... we probably will not use the standard numpad chamber menu, or even make logs. build a custom chamber menu to this experience chamber.")

**Context.** Every chamber so far is a study: the standard chamber menu (Study ID, Start, Debug, Back to Hall), presets, a session file, a CSV log and resume rules (ADR-0001..0003). The HearingLoss chamber is a demonstration: a visitor switches a simulated hearing loss on and off while a sound plays, and nothing is measured.

**Decision**
- A chamber may be an **experience chamber**: it has its own panel instead of `ChamberMenu`, no Study ID, no session or `.session` file, no CSV log, no presets file and no Debug bench. Its constants live in its `Docs/Design.md` (rule 7 applies to studies).
- It is still a normal chamber for the App: folder `Chambers/<Id>`, a `ChamberDefinition` (label, title, scene), listed in the Hall by Sync Chambers, entered and left with `AppNav`. It does not derive from `ChamberBehaviour`.
- The **Y button** stays the way out (rule 8): it opens a small dialog **Keep going / Back to Hall** (time and audio frozen while open), built with `GPL.UI` `ModalDialog`. The panel also has **Back to Hall**.
- Input still goes through `GPL.Input` (the Stop action) and the `GPL.UI` pointer; the panel language is the PO's choice per chamber (HearingLoss: English only).
- Docs: `Brief.md`, `Design.md`, `SceneSetup.md`. `DataFormat.md` and `AnalysisPlan.md` are not needed (no data); the Brief says so.

**Consequences**
- Turning an experience into a study later means adding a `ChamberBehaviour` + `ChamberMenu` and the data docs (data first, rule 5).
- Validate Setup of an experience chamber checks its own scene; the catalog needs nothing new.
- Console messages still use `[Chamber:<Id>]`; the Console is the only trace of a visit.
