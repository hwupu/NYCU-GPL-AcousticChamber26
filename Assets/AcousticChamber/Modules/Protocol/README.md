# GPL.Protocol

The stage tree of a study session (session > stage > block > trial), walked step by step with the session cursor kept in step,
so every resume policy of `GPL.Session` works without chamber code. Plus the stock participant screens (language, handover,
messages, cue text). Generalized from HW2's `ExperimentFlow` loop (PB-07b).

**Copy:** this folder + `Core/`, `Session/`, `Logging/`; for the screens also `UI/`, `Input/`, `Homing/`.
**Dependencies:** `GPL.Protocol.Core`: GPL.Core, GPL.Session.Core, GPL.Logging.Core (pure C#). `GPL.Protocol`: GPL.UI,
GPL.Input, GPL.Homing, Meta XR Core, TextMeshPro.

## Plan (`ProtocolPlan`, Core)
Built by the chamber from the session's parameter snapshot, so a resumed session gets the same plan.
```
var plan = new ProtocolPlan()
    .Stage("language", StageFlags.Once)                 // plain stage: the chamber runs its body
    .Stage("handover", StageFlags.RepeatOnResume)
    .Blocks("practice", 1, practiceTrials, ResumeFrom.StageStart)   // 0 trials: left out
    .Blocks("experiment", blocks, trialsPerBlock, policy)
    .Stage("thanks", StageFlags.CompletesSession);
record.PlanHash = plan.Hash();                           // a resume with another hash is refused
```
| Flag | Meaning |
|---|---|
| `Once` | skipped once it has ended (also on a `SessionStart` resume); kept as `custom.stage_done.<name>=1` |
| `RepeatOnResume` | after a resume it runs again before the resume point, without moving the cursor (plain stages only) |
| `CompletesSession` | entering it sets the status `Completed` (a kill afterwards is not resumable) |

## Walk (`ProtocolWalker`, Core)
```
var w = new ProtocolWalker(plan, record, isResume, r => store.Save(r, DateTime.Now));
while (w.Next())
{
    var s = w.Current;                 // StageBegin, BlockBegin, Trial, BlockEnd, StageEnd
    ProtocolLog.Write(log, s);         // structure rows + stage/block/trial/attempt columns
    ...run the step's content; a Trial step ends with the chamber's trial row...
}
```
`Next()` first completes the previous step: a trial counts only then, after its row was written, so a trial interrupted by a
kill runs again (and never appears twice in one attempt as long as the row is its last action). The cursor is saved after
every change. On a resume the walker resolves the record's policy (`NextTrial`, `BlockStart`, `StageStart`, `SessionStart`),
replays the `RepeatOnResume` stages, and begins blocks again as new attempts unless `NextTrial` continues one.

Steps carry `Replay` (a replayed stage), `Resumed` (the stage / block where the session continues) and `Continued` (a block
continued in the same attempt).

## Log rows (`ProtocolLog`, Core)
`stage_begin` / `stage_end` (note `again after resume` for a replay, `resumed` at the resume point), `block_begin` (note
`block 2 of 4, 8 trials`; `continued at trial n` after a `NextTrial` resume), `block_restart` (instead of `block_begin` for
attempt 2 and later), `block_end`. Trial steps write no row; they set the `trial` column (1-based). Plain stages leave
`block`, `trial` and `attempt` empty.

## Stock screens (`ProtocolScreens`, Runtime)
Built at run time on the chamber's overlay canvas (ADR-0004). Participant text in English and 中文 (`ProtocolStrings`; a
chamber may override a key by registering its own table later).
- `Language(chosen)`: "English" / "中文"; sets `Loc`.
- `Handover(rig, done)`: "Please hand the headset to the participant." + Continue (experimenter), then "Look straight ahead
  and press A." (participant); A runs `SystemRecenterMethod`, the result goes to `done` (log it as `homing`).
- `Message(text, pressA, buttonKey)`: waits for A or the button.
- `Cue(text, y)`: bare text without a panel while the world is visible (gate hint, "Press A"); `Cue(null)` hides it.
- `PressedA()`: A this frame, ignored while the Stop dialog is open (`InputOpen`).

## Tests
`Tests/EditMode/ProtocolTests.cs`: plan shape and hash, the full walk, each resume policy, a resume during the language, the
`NotResumable` policy, and **a kill at every step for every policy** (resume from the saved file, finish, and check that the
highest attempt of every block holds each trial exactly once).
