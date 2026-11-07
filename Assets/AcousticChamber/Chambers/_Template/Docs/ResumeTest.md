# Template (C00) — Kill-the-app resume test (PB-07c)

Proves every resume policy on the headset. The same cases run as an EditMode test without a headset
(`GPL.Protocol` `ProtocolTests.KillAtAnyStep_ResumeCompletesEveryBlock`: a kill at every step, every policy); this checklist
covers what only the app can show: real quits, the session file on disk, the chamber menu, the launch routing and the CSV.

## Setup (once per policy)
1. Hall > Template > **Debug** > Parameters: set `resume_from`; Save as preset (e.g. `Resume-next`), Make active. Back to the
   chamber menu: the line under the buttons reads "Running on custom preset: Resume-next". Defaults otherwise (2 practice trials, 2 blocks × 4 trials).
2. Use a new Study ID per case (S0101, S0102, ...), so the cases do not mix.

## The kill
Start the session, hand over, choose a language, recenter with the Meta button, go through the slides, pass the dot, do the practice, block 1, then **2 trials of block 2**.
During trial 3 of block 2 (dot or "Press A" showing): **quit the app**, Quest: Meta button > Quit; Editor over Link: stop Play.
Start the app again.

## Expected after the restart
For every resumable policy:
- The app opens at the **Template chamber menu**, not the Hall (launch routing), status "Opened at launch: …".
- The line under the buttons reads "Unfinished: S0101" (with the other unfinished IDs).
- Enter 101, Start: dialog "S0101 has an unfinished session" > **Resume** (the volume is read, no pad).
- No language screen; the handover (Continue), the Meta-button recenter and the dot check run again; no slides (already done).
- After the run: thanks, the menu no longer lists the ID; the Hall pill count drops.

| `resume_from` | Continues with | CSV rows after `session_resume` |
|---|---|---|
| `next_trial` | trial 3 of block 2 | `stage_begin` handover, recenter and ready (again after resume), `stage_begin experiment` (resumed), `block_begin` block 2 "continued at trial 3", trials 3–4 with `attempt` 1 |
| `block_start` | block 2 from trial 1 | `block_restart` block 2 `attempt` 2, trials 1–4 `attempt` 2 |
| `stage_start` | block 1 of the experiment | `block_restart` block 1 `attempt` 2, then block 2 `attempt` 2 (`block_restart`), 8 trials |
| `session_start` | handover (language skipped) | handover, recenter, slides and ready (not marked "again"), practice `block_restart` `attempt` 2, both blocks `attempt` 2 |
| `not_resumable` | — | the app opens at the **Hall** (nothing to resume); the ID is not listed; Start with it begins a **new** session (new CSV and session file). The old files stay (Debug > Data files) |

Also check once (any policy): quit **during the practice** → the practice starts again as `attempt` 2 (practice always uses
`stage_start`).

## Checking the files
Debug > Data files lists each session (Study ID, status, trial rows). For the CSV itself: Quest
`/sdcard/Android/data/com.nycu.AcousticChamber26/files/Template/` (USB), Editor
`%USERPROFILE%\AppData\LocalLow\NYCU\AcousticChamber26\Template\`. In the CSV:
- The rows before the kill are all there (nothing deleted); the interrupted trial 3 has **no** `trial` row from before the kill.
- The `.session` file: `status=Completed`, `resume_from` = the policy, `attempt.3.1` = the last attempt of block 2
  (stage 3 = experiment when the practice has trials).
- DataFormat step 3 (highest complete attempt per block) gives exactly 4 trials per block.

## Result
Record per policy: pass / fail and a note, in the SprintBoard row of PB-07.
