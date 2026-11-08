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
| `Runtime/UiButton` | the design-system button (a uGUI `Button`): style = Prominent / Gray / Borderless / DestructiveText / Destructive, settle transitions on unscaled time (hover in fast, out slower, press instant at 97 % scale, release settles), disabled 40 %, `InGroup` hover colors, `Selected` (active toggle / chosen button: `select` fill, PB-08e); `UiKit.ChoiceButton` builds one of n equal buttons in a row, `UiKit.Select(buttons, i => on)` sets `Selected` on a set |
| `Runtime/UiKit` | builders: `Window` (the canvas's base layer, pushed back by sheets), `Group` + `Row` (raised group, 88 u rows, optional secondary line, value and ›), `InfoRow` (a row that is not a button, room for its own controls), `SetRowSub`, `RowText`, `SmallButton` (− / + steppers), `SwitchRow`, `Rect`, `Box` (any corner radius from one procedural 9-slice sprite), `Round`, `Background`, `Text`, `LocText`, `Button`, `LocButton`, `IconButton` (‹ `BackGlyph`, × `CloseGlyph`), `DebugTag`, `HitRect`; `MotionSuppressed` (set while a stimulus plays: every transition instant). New objects take the parent's layer |
| `Runtime/UiRoot` | one per scene: sets the theme (old theme assets: current defaults + a warning), registers the module strings |
| `Runtime/PanelMover` | `OverlayPanel.Movable = true` adds a pill-shaped handle below the panel, outside it (a real 3D object and `IRayTarget`, visionOS style; the overlay canvas would clip it): it reacts in a zone much larger than the pill, grows and brightens on hover (settle curve); trigger held + swing the ray turns the panel around the head (yaw, pitch), hand away from / toward the head moves the panel farther / nearer (0.8-5 m). `OverlayPanel.Place` lifts a panel that would reach the floor. Used by the chamber menu (not during a run) and the experience panels. `UiPointer.ActiveRay` / `HasActiveRay` give chamber code the active controller's ray |
| `Runtime/OverlayPanel` | places a canvas 2 m ahead (`yawOffsetDeg` to the side, negative = left), 8° below eye level, facing the eye; scale follows distance; waits for valid head height; re-places 2 frames after a system recenter; hides layers 30/31 from the eye cameras |
| `Runtime/UiPointer` | ray from the controller (last trigger owns the ray), white laser; hit regions grown to 60 x 60 u (`UiKit.HitRect`; the element's own rect wins ties, `Pick`); trigger down presses a `UiButton` (pressed look until release) and clicks it; inside a `ScrollList` the click waits for the release (the press may become a drag) and the right thumbstick scrolls the list under the ray; rows scrolled out of view are not hit; `UiPointer.Modal` limits it to the top sheet |
| `Runtime/RayTarget` | `IRayTarget` (3D objects the pointer can hover, press and drag; `RayTargets.Register`): the nearer of a UI hit and a target wins; a held target owns the trigger until release (no UI clicks meanwhile); ignored while a modal sheet is open (PB-10) |
| `Runtime/ITextEntry`, `NumberPadEntry` | swappable text entry; the number pad (3 x 4 borderless keys in a container with a 4 u inset, Clear, ←) shows the formatted value live (`StudyId.FromDigits`: `1` -> `S0001`) |
| `Runtime/NativeKeyboardEntry` | `ITextEntry` over the platform keyboard (Quest system overlay; needs "Requires System Keyboard"); `Diagnosis()` flags "reports Visible but never shown" (PB-14, headset test open: docs/references/Keyboard.md). Not used by any screen; the number pad stays the default |
| `Runtime/UiLayer` | one layer of a canvas (window or sheet): appears in place (scale `sheetScale` → 1 + fade) and is pushed back by the sheets on top of it (`pushScale` per level, dims to `dim`); settle curve, unscaled time; instant outside Play mode. No blur |
| `Core/ScrollModel.cs` | scroll state, Unity-free: offset / max, press → drag after 12 u (shorter presses stay clicks), content follows the pointer, momentum (exponential decay 0.35 s, clamped ends), thumbstick 1200 u/s (dead zone 0.2) |
| `Runtime/ScrollList` | borderless scroll list: clipped viewport (`RectMask2D`), top-anchored `Content` + `ContentHeight`, edge fades (56 u, only where more content waits, color = the background behind), indicator (8 u, `text3`, fades 0.6 s after moving); driven by `UiPointer` |
| `Runtime/PageStack`, `Page` | pages in one container (the window): `Push(title, debugTag)`, `Pop`, `PopToRoot`, `Back`; header with ‹ (back one page; on the root page only if `RootBack` is set), title, optional DEBUG tag; pages change in place (fade + `pageScale` 0.98); pages below stay alive (inactive) and raise `Returned` when they come back; keep the top `Page.HeaderHeight` free; `AddHeaderAction` (Revert, Delete all at the header's right) |
| `Runtime/Column` | a scrolling page body below the header at the content width: `Label` (group label, wraps), `Group`, `Buttons` (a group of side-by-side buttons), `Gap`, `Finish` |
| `Runtime/UiSwitch` | switch 100 x 60 u (off: gray track, text2 knob; on: prominent track, dark knob; the knob settles); `UiKit.SwitchRow` makes the whole row its target |
| `Runtime/Sheet` | a sheet: surface box on top of a canvas, pushes the window and lower sheets back; sheets stack; only the top one takes the ray (`UiPointer.Modal`); `Close` brings the layers forward, fades out, destroys; `Abandoned` if destroyed without `Close` |
| `Runtime/ModalDialog` | a decision sheet: title and message left-aligned, buttons right-aligned and sized to their labels (main action last = right); dialogs stack (`Current` = top) |
| `Runtime/StopDialog` | the Stop dialog: real session Drop out (red text) / Keep going (prominent); Drop out asks once more on a second sheet on top ("Drop out S0012?" Cancel / Drop out in red); Debug run Back to the chamber menu / Keep going; returns a `StopChoice`. `Freeze` (time scale 0 + audio pause, nested, restores the old values) while open |
| `Runtime/Kit/` | rebuilt kit, proof of concept (docs/design): `UiLay` (layout-group helpers, label nudge, background child), `UiPanel` (transparent root: show, push back, placement, grabber visibility), `UiSurface`, `UiNavigationBar`, `UiLay.Section/Row/ChoiceGroup/Button/SwitchRow/TextField/Stepper`, `UiDialog`; `UiKitPoc` = the C01 menu (Magic > App > UiKit PoC Panel) |
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
var window = UiKit.Window(canvas.transform);   // content goes into the window; sheets go onto the canvas
var pages = PageStack.On(window);
var menu = pages.Push("C04 · HumPerHW2", debugTag: true).Rect;   // later: pages.Push("Debug"), ‹ pops
UiKit.LocButton(window, "Next", UiStrings.Next, UiStyle.Prominent, new Vector2(0, -250), new Vector2(300, 0), OnNext);
UiKit.IconButton(window, "Back", UiKit.BackGlyph, new Vector2(-412, 270), OnBack);
var pad = NumberPadEntry.Create(window, new Vector2(-250, -50), StudyId.MaxDigits, StudyId.FromDigits, "S____");
var list = ScrollList.Create(menu, "Files", new Vector2(0, -60), new Vector2(800, 500));   // rows go under list.Content
var group = UiKit.Group(list.Content, "Group", Vector2.zero, 760, files.Count);
ScrollList.TopAnchored(group, 0);
list.ContentHeight = group.sizeDelta.y;
var sheet = Sheet.Open(canvas.transform, "Save as", Vector2.zero, new Vector2(820, 500));   // build into sheet.Rect; sheet.Close()
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
