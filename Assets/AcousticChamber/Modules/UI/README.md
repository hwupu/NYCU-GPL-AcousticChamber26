# GPL.UI

Readable, clickable GUI for Meta Quest: overlay canvases placed in front of the participant, a controller ray pointer,
design-system components (DesignSystem v2, ADR-0005: buttons with timed states, capsules, hit regions), text (TextMeshPro,
English + 中文), a number pad, modal dialogs, localization.
Generalized from HW2's `UiPointer`, `GuiPanelPlacer`, `ParticipantIdField`, `LocalizedText`, `Localization` and the GUI
helpers of `HumPerSetup`.

**Copy:** this folder + `Core/` (GPL.Core). **Dependencies:** Meta XR Core SDK (`Oculus.VR`: OVRCameraRig, OVRInput,
OVROverlayCanvas, OVRControllerHelper), uGUI + TextMeshPro (`com.unity.ugui` 2.x), URP (laser material).

## Parts
| Part | What |
|---|---|
| `Core/Localization.cs` | `UiLanguage` (English, Chinese), `StringTable`, `Loc` (current language, `Changed`, lookup; later tables override; missing key -> `[key]`; labels exactly "English" / "中文") |
| `Core/DigitEntry.cs` | number-pad model: digits only, max length |
| `Core/Motion.cs` | `CubicBezier` timing curve, `Motion.Settle` = cubic-bezier(.16, 1, .3, 1) (fast start, soft landing, no overshoot), `Motion.Progress` |
| `Runtime/UiTheme` | design tokens asset (design system v2): surfaces and text, per-style state colors (normal / hover / pressed / label; pressed is the brightest), type scale, geometry (capsule buttons, 60 u hit minimum, 4 u inset, radii), motion (150 / 350 / 250 / 450 ms), `reduceMotion`; `Edited` restyles every button live; `version` + `ResetTokens` migrate old assets |
| `Runtime/UiButton` | the design-system button (a uGUI `Button`): style = Prominent / Gray / Borderless / DestructiveText / Destructive, settle transitions on unscaled time (hover in fast, out slower, press instant at 97 % scale, release settles), disabled 40 %, `InGroup` hover colors |
| `Runtime/UiKit` | builders: `Rect`, `Box` (any corner radius from one procedural 9-slice sprite), `Round`, `Background`, `Text`, `LocText`, `Button`, `LocButton`, `IconButton` (‹ `BackGlyph`, × `CloseGlyph`), `DebugTag`, `HitRect`; `MotionSuppressed` (set while a stimulus plays: every transition instant). New objects take the parent's layer |
| `Runtime/UiRoot` | one per scene: sets the theme (old theme assets: current defaults + a warning), registers the module strings |
| `Runtime/OverlayPanel` | places a canvas 2 m ahead, 8° below eye level, facing the eye; scale follows distance; waits for valid head height; re-places 2 frames after a system recenter; hides layers 30/31 from the eye cameras |
| `Runtime/UiPointer` | ray from the controller (last trigger owns the ray), white laser; hit regions grown to 60 x 60 u (`UiKit.HitRect`; the element's own rect wins ties, `Pick`); trigger down presses a `UiButton` (pressed look until release) and clicks it; `UiPointer.Modal` limits it to a dialog |
| `Runtime/ITextEntry`, `NumberPadEntry` | swappable text entry; the number pad (3 x 4 borderless keys in a container with a 4 u inset, Clear, ←) shows the formatted value live (`StudyId.FromDigits`: `1` -> `S0001`) |
| `Runtime/ModalDialog` | dim + surface box, title and message left-aligned, buttons right-aligned and sized to their labels (main action last = right); modal for the pointer; `Abandoned` if destroyed without `Close` |
| `Runtime/StopDialog` | the Stop dialog: real session Drop out (red text) / Keep going (prominent); Drop out asks once more ("Drop out S0012?" Cancel / Drop out in red); Debug run Back to the chamber menu / Keep going; returns a `StopChoice`. `Freeze` (time scale 0 + audio pause, nested, restores the old values) while open |
| `Runtime/UiShowcase` | headset test panel of the module (no study logic): every button style, ‹ back, DEBUG tag, number pad, Stop dialog, a motion switch |
| `Editor/UiSetup` | `EnsureFontAsset` (dynamic SDF from a .ttf/.otf), `EnsureTheme` (creates or migrates the theme asset), `EnsureUnlitMaterial`, `CreateOverlayCanvas`, `EnsurePointer`, `EnsureUiRoot`, `EnsureGroup`, `EnsureLayerName` |

## Use
Editor step (once per scene; see `AcousticChamber.App.Editor.AppSetup` for an example):
```csharp
var regular = UiSetup.EnsureFontAsset("Assets/.../NotoSansTC-Regular.otf", "Assets/.../NotoSansTC-Regular SDF.asset");
var bold    = UiSetup.EnsureFontAsset("Assets/.../NotoSansTC-Bold.otf",    "Assets/.../NotoSansTC-Bold SDF.asset");
var theme   = UiSetup.EnsureTheme("Assets/.../UiTheme.asset", regular, bold);
var ui = UiSetup.EnsureGroup("UI");
UiSetup.EnsureUiRoot(ui, theme);
var canvas = UiSetup.CreateOverlayCanvas(ui, "Menu", new Vector2(1000, 700));
UiSetup.EnsurePointer(ui, UiSetup.EnsureUnlitMaterial("Assets/.../Laser.mat", Color.white));
```
Run time (screens are built by code into the canvas):
```csharp
UiKit.Background(canvas.transform);
UiKit.LocButton(canvas.transform, "Next", UiStrings.Next, UiStyle.Prominent, new Vector2(0, -250), new Vector2(300, 0), OnNext);
UiKit.IconButton(canvas.transform, "Back", UiKit.BackGlyph, new Vector2(-412, 270), OnBack);
var pad = NumberPadEntry.Create(canvas.transform, new Vector2(-250, -50), StudyId.MaxDigits, StudyId.FromDigits, "S____");
ModalDialog.Show(canvas.transform, "Delete this file pair?", "...",
                 new DialogButton("Cancel", UiStyle.Borderless), new DialogButton("Delete", UiStyle.Destructive, Delete));
UiKit.MotionSuppressed = true;   // while a stimulus plays: no UI motion
```

## Quest notes
- Text and panels are drawn through `OVROverlayCanvas` (sharp, compositor layer). The source canvas lives on layer 30,
  which the eye cameras must not render; content added at run time must be on the same layer (UiKit does this).
- Meta's `OVRInputModule` does not work with the Input System only, hence the own pointer.
- Laser material: a URP Unlit **material asset** (single-pass stereo). `Sprites/Default` renders in one eye.
- Glyphs missing from the font show as boxes. Noto Sans TC has no "⌫", "✕", "❮"; use "←", "×" (`CloseGlyph`), "‹" (`BackGlyph`).
- All UI motion runs on unscaled time, so it keeps working while the Stop dialog freezes `Time.timeScale`.
- Existing `UiTheme.asset` files from v1 are migrated by `UiSetup.EnsureTheme` (run the setup step once).
