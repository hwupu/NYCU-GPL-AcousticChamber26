# ADR-0003: Sessions, resume, Stop dialog, bindings, text entry (2026-11-05)

Status: **accepted**.

**Sessions and resume**
- Sessions are keyed by chamber + Study ID; several unfinished sessions may exist.
- On launch, if the last used chamber has unfinished sessions, the app opens that **chamber's menu** (the experimenter is the user there), otherwise the Hall.
- In the menu: Study ID + Start; an unfinished session for that ID offers Resume / Start new / Back. Start new closes the old session (`session_superseded`), never deletes it.
- After Resume the headset is handed over and the protocol continues **silently** at the point given by the stage's `ResumeFrom` policy (`NextTrial`, `BlockStart`, `StageStart`, `SessionStart`, `NotResumable`); stage resume hooks may re-apply the condition or run homing.
- Partial blocks stay in the CSV (`attempt`); the analysis uses the highest complete attempt.

**Stop dialog** (global action, default **Y** on the left controller), modal, session frozen: Keep going / Pause and back to the chamber menu (resumable, `session_paused`) / Drop out (`participant_dropout`, not resumable). Debug: Keep going / Back to the chamber menu.

**Bindings.** Global actions are reserved and identical across chambers by default; a chamber can remap one only by declaring it in its `ChamberDefinition`, and the menu shows the binding. Chamber actions are declared in the same table and read through `GPL.Input` (no direct `OVRInput` in chamber code). The Meta button stays with the system.

**Text entry.** `ITextEntry` with `NumberPadEntry`, `KeypadEntry`, `NativeKeyboardEntry` (Unity `TouchScreenKeyboard`), selectable per chamber. The HW2 system-keyboard failure gets its own ticket (PB-14). Meta's in-app Virtual Keyboard is not in the installed Core SDK 207.
