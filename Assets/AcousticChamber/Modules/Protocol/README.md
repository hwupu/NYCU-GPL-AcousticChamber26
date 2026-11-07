# GPL.Protocol

The stage tree of a study session (session > stage > block > trial), walked step by step with the session cursor kept in step,
so every resume policy of `GPL.Session` works without chamber code. Plus the stock participant screens (language, handover,
messages, cue text). Generalized from HW2's `ExperimentFlow` loop (PB-07b).

**Copy:** this folder + `Core/`, `Params/`, `Session/`, `Logging/`; for the screens also `UI/`, `Input/`, `Homing/`.
**Dependencies:** `GPL.Protocol.Core`: GPL.Core, GPL.Params.Core, GPL.Session.Core, GPL.Logging.Core (pure C#).
`GPL.Protocol`: GPL.UI, GPL.Input, GPL.Homing, Meta XR Core, TextMeshPro. `GPL.Protocol.Editor` (Editor only): GPL.Protocol.

## Plan (`ProtocolPlan`, Core)
Built by the chamber from the session's parameter snapshot, so a resumed session gets the same plan.
```
var plan = new ProtocolPlan()
    .StageIf(on("handover"), "handover", StageFlags.RepeatOnResume)   // plain stage: the chamber runs its body
    .StageIf(on("language"), "language", StageFlags.Once)             // StageIf: switched by a preset boolean
    .StageIf(on("recenter"), "recenter", StageFlags.RepeatOnResume)
    .StageIf(on("instructions"), "instructions")
    .StageIf(on("ready"), "ready", StageFlags.RepeatOnResume)
    .Blocks("practice", 1, practiceTrials, ResumeFrom.StageStart)   // 0 trials: left out
    .Blocks("experiment", blocks, trialsPerBlock, policy)
    .Stage("thanks", StageFlags.CompletesSession);
record.PlanHash = plan.Hash();                           // a resume with another hash is refused
```
`ProtocolPlan.SingleTrial(stage)`: one block with one trial (the Debug bench's Practice trial, PB-21b).

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
- `Handover()`: title "Protocol", a numbered list "1. Set the IPD. 2. Hand the headset to the participant." + Continue
  (experimenter). No recenter (PB-20b).
- `MetaRecenter(listener, done, look, language, shown)`: the recenter slide in the slide layout: title (default "Recenter the
  view"), illustration from `look` (a `SlideSet.Slide`; without an image the text "Look straight ahead. Hold the Meta button
  until the view recenters." fills the gray area), Back reading "← Language" when `language` is given (runs it, then the slide
  comes back), Next always active (no A button). The result goes to `done` (log it as
  `homing`): the user recenter the `RecenterListener` reported, or outcome `cancelled` when Next came without one. Slide
  buttons (360 u, … when too long) name where they lead: "← <previous slide>" and "<next slide> →" (short title, else title;
  "← Back" / "Next →" when unknown); a final Continue has no arrow. Instruction slide 1 goes back to the recenter slide.
- `DotCheck(rig, dot, gate, done)`: the final check before the trials: "Look at the dot to begin" and the head gate; the result
  goes to `done`. Log the final headset volume after it (`volume_check`, see the App's `HeadsetVolume`).
- `Slides(set, start, resumed, shown)` (PB-20): instruction slides from a `SlideSet` asset (per slide: id, title English /
  中文, image English / 中文; 中文 falls back to English). Layout and image size: DesignSystem section 9 (images: import with
  Non-Power of 2 = None, no mipmaps). Back is hidden on the first slide unless a first Back is given; Continue on the
  last. Built once, changes in place. `shown(deck, slide, how)` runs for every slide on screen: store the index with
  `SlideDeck.Store` and log `slide` with `deck.Note(slide.id, how)`; on a resume open on `SlideDeck.Restore`. An empty or
  missing set shows nothing. Navigation is the pure `SlideDeck` (Core). The general overload takes `SlidePage`s (title,
  image, caption text for the gray area while there is no image, `Ready` = Next active) plus an optional first Back
  (`firstBackKey`, `firstBack`) and the last Next label (`lastKey`).
- `Message(text, buttonKey, until)`: the Message layout; waits for the button or until `until()` is true. No screen waits for A.
- `Choice(text, leftKey, rightKey, chosen)` (PB-08d-3): the Message layout with two buttons (left gray, right prominent); reports 0 / 1.
- `Cue(text, y)`: bare text without a panel while the world is visible (gate hint, "Press A"); `Cue(null)` hides it.
- `PressedA()`: A this frame, ignored while the Stop dialog is open (`InputOpen`).

## Onboarding (`OnboardingStages`, Core; `Onboarding`, Runtime; PB-21)
The stock onboarding stages of Architecture section 8 in one place, so a chamber does not wire them itself.
```
schema: defs.AddRange(OnboardingStages.SwitchDefs());        // onboarding.handover ... onboarding.ready (all on)
plan:   OnboardingStages.AddTo(new ProtocolPlan(), values)    // the switched-on stages with their resume flags
            .Blocks("practice", ...).Blocks("experiment", ...).Stage(OnboardingStages.Thanks, StageFlags.CompletesSession);
run:    var ob = new Onboarding(screens, record, log, values) { Save, Rig, Cue, Listener, Instructions, RecenterSlide,
                                                                ReadVolume, VolumeTarget };
        yield return ob.TryRun(step) ?? chamberStage(step);    // null = not an onboarding stage
```
`Onboarding` writes the stage rows (`handover`, `language`, `homing`, `slide`, `volume_check` with column
`OnboardingStages.VolumeColumn` = `volume_step` when the log has it), stores the language and the slide index in the record,
links Back on the recenter slide to the language choice and Back on instruction slide 1 to the recenter slide.
`InRecenterStage` tells a recenter listener that the Meta recenter belongs to the recenter stage (no extra `recenter` row).
A schema without a switch counts it as on. The App's `ProtocolChamber` uses all of this; a chamber deriving from it gets
the onboarding for free, plus the Debug bench's Practice trial (its `RunTrial` once in `SingleTrial("practice")`, no
onboarding; `PracticeStageName` renames the stage). A chamber adds Debug sections by overriding
`ChamberBehaviour.DebugSections(col, bench)` (Template: "Fixation dot" Show / Hide).

## Slide editor helpers (`SlideSetup`, Editor; PB-21c)
For a chamber's Magic step that sets up its instruction slides; idempotent, existing files keep their .meta and GUID.
```
SlideSetup.EnsureFolder(folder);                              // with missing parents
var tex = SlideSetup.Placeholder(folder + "/Placeholder-1.png", fill);   // gray 2000 x 936, frame at the 96 px safe margin
var set = SlideSetup.EnsureSet(folder + "/Instructions.asset", () => new[] { new SlideSet.Slide { id = "...", imageEnglish = tex } });
SlideSetup.ImportForSlides(path);                             // Non-Power of 2 = None, no mipmaps, max size >= 2048
SlideSetup.PngSize(path);                                     // (w, h) from the header, (0, 0) if not a PNG
```
`Placeholder` redraws a file of another size, so call it only on placeholder files, never on a real illustration.
`EnsureSet` calls its slide list only when it creates the set. `Width`, `Height`, `SafeMarginPx` hold the slide image size.

## Tests
`Tests/EditMode/SlideSetupTests.cs`: PNG header size (also missing, short, non-PNG files), the placeholder frame position.
`Tests/EditMode/OnboardingTests.cs`: stage order and flags, switched-off stages, a schema without switches.
`Tests/EditMode/ProtocolTests.cs`: plan shape and hash, the full walk, each resume policy, a resume during the language, the
`NotResumable` policy, and **a kill at every step for every policy** (resume from the saved file, finish, and check that the
highest attempt of every block holds each trial exactly once). `Tests/EditMode/SlideDeckTests.cs`: Back / Next, the end of
the deck, an empty deck, the stored slide (clamped, per stage, through the session file), the row note.
