# Product Backlog

Product Owner: researcher. Status: Done / Open / Blocked (waiting for PO) / Closed (won't do or accepted).

| ID | Ticket | Acceptance | Status |
|---|---|---|---|
| PB-01 | Project hygiene | Git repo, `.gitignore`, Git LFS (`.gitattributes`), CLAUDE.md, AGENTS.md symlink | Done (AGENTS.md is a real symlink in git; local checkout needs Windows Developer Mode) |
| PB-02 | Architecture | `docs/Architecture.md`, ADR-0001 approved by the PO, questions Q1-Q12 answered | Blocked (PO answers) |
| PB-03 | GPL.Core / Logging / Session (pure) | Port HW2 log + progress, generalized (schema, cursor, resume policy); EditMode tests pass headless | Open |
| PB-04 | GPL.UI + GPL.Homing runtime | Overlay canvas, pointer, number pad, panel placer, dialog, slides; head gate, recenter listener, software recenter, align-to-target | Open |
| PB-05 | Main Hall + catalog + routing | Hall scene, chamber list, participant ID, unfinished sessions, auto-route to the active session | Open |
| PB-06 | `_Template` chamber | Copyable minimal chamber; resume variants proven by kill-the-app test | Open |
| PB-07 | GPL.Acoustics | Port HW2 room dialog, mixer setup, ReverbTuner, RT60 meter/calibration, audio build guard | Open |
| PB-08 | GPL.HearingLoss | ANSIS344 + test vectors; per-ear listener filter bank; presets; unilateral/bilateral | Open |
| PB-09 | GPL.DebugBench + Explore mode | Modules register controls; condition audition | Open |
| PB-10 | First chamber | Brief, Design, DataFormat, AnalysisPlan approved, then built (Q1) | Open |
