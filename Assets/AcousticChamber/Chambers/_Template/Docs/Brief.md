# Template (C00) — Brief

**Status:** approved by the PO (PB-07, 2026-11-06).

The minimal working chamber, to copy when starting a new one (PB-13 automates the copy). It has no stimuli; it shows every
part a real chamber uses:
- the chamber menu (Study ID, Preset line, Start, Debug, unfinished sessions, Resume / Start new / Back),
- a protocol of stages, blocks and trials (`GPL.Protocol`),
- homing (system recenter at handover, head gate before every trial),
- a response (press A) with a reaction time,
- the CSV log and the session file, the Stop dialog (Y), and resume.

Its second job is **testing**: every resume policy is proven with it by killing the app mid-session (PB-07c), selected by the
preset parameter `resume_from`.

Chamber ID `Template`, label `C00`, folder `Chambers/_Template`. Docs: [Design](Design.md), [DataFormat](DataFormat.md),
[AnalysisPlan](AnalysisPlan.md).
