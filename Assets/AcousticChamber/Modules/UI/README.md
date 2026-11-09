# GPL.UI

Readable, clickable GUI for Meta Quest: overlay canvases in front of the participant, a controller ray pointer, the design-system
components (layout-group based, designed in the Storybook preview `docs/design/`, ADR-0007), text (TextMeshPro, English + 中文),
icons, a number pad, dialogs and sheets, localization. Generalized from HW2's `UiPointer`, `GuiPanelPlacer`, `ParticipantIdField`,
`LocalizedText`, `Localization` and the GUI helpers of `HumPerSetup`.

**Copy:** this folder + `Core/` (GPL.Core). **Dependencies:** Meta XR Core SDK (`Oculus.VR`: OVRCameraRig, OVRInput,
OVROverlayCanvas, OVRControllerHelper), uGUI + TextMeshPro (`com.unity.ugui` 2.x), URP (laser material).

## Parts
| Part | What |
|---|---|
| `Core/Localization.cs` | `UiLanguage` (English, Chinese), `StringTable`, `Loc` (current language, `Changed`, lookup; later tables override; missing key -> `[key]`; labels exactly "English" / "中文") |
| `Core/DigitEntry.cs` | number-pad model: digits only, max length |
| `Core/Motion.cs` | `CubicBezier` timing curve, `Motion.Settle` = cubic-bezier(.16, 1, .3, 1) (fast start, soft landing, no overshoot), `Motion.Progress` |
| `Runtime/UiTheme`, `UiIconSet` | design tokens asset (version 3: surfaces and text, per-style state colors, type scale, geometry, paddings, radii, motion, `reduceMotion`) and the icon set (name -> sprite; white PNGs exported by `pnpm icons` from `docs/design/src/icons`); `Edited` restyles every button live; `version` + `ResetTokens` migrate old assets |
| `Runtime/UiKit/` | **one file per Storybook component, same name** (component list and mapping: docs/design/README.md). Factories are the static partial class `UiKit` (`UiKit.Button`, `UiKit.Row`, ...), stateful ones classes of the same name. `UiKit.cs` / `UiKit.Layout.cs` hold the basic builders: `Rect`, `Fill`, `Box`, `Round` (any corner radius from one procedural 9-slice sprite), `Text`, `BareIcon`, `Select` (shows the chosen button of a set), `Clear`, `HitRect`, `InLeavingLayer`; `Theme`; `MotionSuppressed` (set while a stimulus plays: every transition instant). New objects take the parent's layer |
| `UiKit/UiButton`, `UiIcon`, `UiSwitch` | the button (a uGUI `Button`): style Prominent / Gray / Borderless / DestructiveText / Destructive, settle transitions on unscaled time, `InGroup`, `Selected`, icons follow the label color (`UiIcon.FollowsLabel`); the switch (100 x 60 u) |
| `UiKit/UiSection`, `UiRow`, `UiChoiceGroup`, `UiTextField`, `UiStepper`, `UiList` | **the components, as layouts** (sizes come from paddings, minimum heights and flexible widths, never from hand-computed positions): `Button` (80 u, icon-side padding 75 %, label nudge 2 u), `Section` (raised group), `Row` (88 u, title, subtitle, value, icons), `SwitchRow`, `ChoiceGroup` (segmented, single selection; `columns` > 0 = a grid on one track; `LabeledRow` puts a label in front), `TextField`, `Stepper`, `List` (scrolling stack with edge fades), `Label`, `Fill` (the background is a child image the layout ignores: a sliced `Image` on a layout node adds its 130 u slice size to the node's preferred size) |
| `UiKit/UiPanel` | the transparent root of a panel: `Shown` (fade + scale from `sheetScale`), `Depth` (pushed back: 94 % per level and a dim), `Placement` (8° below eye level or level), `Present` / `Close` / `Stack` / `Top` (panels of a canvas stack; a `Modal` panel takes the ray), `HasGrabber`, `GrabberVisible`. Its child `Surface` is the rounded rectangle |
| `UiKit/UiSurface` (+ `UiPage`), `UiNavigationBar` | the surface holds a stack of pages (`Push`, `Pop`, `PopToRoot`, `Back`, `RootBack`; pages change in place, fade + `pageScale` 0.98, the page below stays alive). A page has the slots navigation bar (116 u: ‹ / ×, title, DEBUG tag, header actions), body (48 u at the sides, 40 u at the bottom without status or toolbar), status line and toolbar |
| `UiKit/UiDialog`, `UiSheet`, `Runtime/ModalDialog`, `StopDialog` | a decision (820 u, title, message, right-aligned actions, main action last) and a sheet (navigation bar with × and a body); both are modal panels that push the panels behind back. `ModalDialog.Show(canvas, title, message, buttons)` is the call chambers use; `StopDialog` is the Stop dialog (Drop out asks twice on a second dialog on top; `Freeze` while open) |
| `UiKit/UiCallout` | a world-space annotation of a 3D object (pill, icon, label, tail); follows its target, faces the head, keeps its angular size; `Show(delay)` fades in after the delay, `Dismiss()` fades out; no periodic animation |
| `Runtime/UiColumn` | not a Storybook component: the scrolling body of Debug-style pages (Config, Parameters, Presets, Log files): `Label`, `Group`, `Buttons`, `Gap` |
| `Runtime/UiRoot` | one per scene: sets the theme (old theme assets: current defaults + a warning), registers the module strings |
| `UiKit/UiGrabber` | `OverlayPanel.Movable = true` adds a pill grabber below the panel (a real 3D object and `IRayTarget`; the overlay canvas would clip it): 120 x 8 u, 16 u below the surface, 220 x 16 u on hover. Trigger held + swing the ray turns the panel around the head (yaw, pitch) at a fixed distance (no distance move: the canvas scale follows the distance, so it looked the same). `OverlayPanel.Place` lifts a panel that would reach the floor. `UiPointer.ActiveRay` / `HasActiveRay` give chamber code the active controller's ray |
| `Runtime/OverlayPanel` | places a canvas 2 m ahead (`yawOffsetDeg` to the side, negative = left), 8° below eye level, facing the eye; scale follows distance; waits for valid head height; re-places 2 frames after a system recenter; hides layers 30/31 from the eye cameras |
| `Runtime/UiPointer` | ray from the controller (last trigger owns the ray), white laser; hit regions grown to 60 x 60 u; trigger down presses a `UiButton` and clicks it; inside a `ScrollList` the click waits for the release and the right thumbstick scrolls the list under the ray; content under a canvas group that does not block the ray (leaving, pushed back, hidden) is not hit; `UiPointer.Modal` limits it to the top modal panel |
| `Runtime/RayTarget` | `IRayTarget` (3D objects the pointer can hover, press and drag; `RayTargets.Register`): the nearer of a UI hit and a target wins; a held target owns the trigger until release |
| `Runtime/ITextEntry`, `UiKit/UiNumberPad` | swappable text entry; the number pad (`CreateLayout`: a text field above 3 x 4 borderless keys in a raised container, Clear, ←) shows the formatted value live (`StudyId.FromDigits`: `1` -> `S0001`) |
| `Runtime/NativeKeyboardEntry` | `ITextEntry` over the platform keyboard (Quest system overlay; needs "Requires System Keyboard"). Not used by any screen; the number pad stays the default |
| `Core/ScrollModel.cs`, `Runtime/ScrollList` | scroll state, Unity-free (drag after 12 u, momentum, thumbstick), and the clipped list with edge fades and an indicator; `UiKit.List` sizes its range from the layout |
| `Runtime/UiKitPoc` | the C01 menu from the components with hide / push back / placement buttons (Magic > App > UI Test Panel) |
| `Editor/UiSetup`, `UiIconSetup` | `EnsureFontAsset`, `EnsureTheme`, `EnsureIconSet` (and the importer for the icon PNGs), `EnsureUnlitMaterial`, `CreateOverlayCanvas`, `EnsurePointer`, `EnsureUiRoot`, `EnsureGroup`, `EnsureLayerName` |

## Use
Editor step (once per scene; see `AcousticChamber.App.Editor.AppSetup` for an example):
```csharp
var theme = UiSetup.EnsureTheme("Assets/.../UiTheme.asset", regular, bold);
var ui = UiSetup.EnsureGroup("UI");
UiSetup.EnsureUiRoot(ui, theme);
var canvas = UiSetup.CreateOverlayCanvas(ui, "Menu", new Vector2(1000, 700));
UiSetup.EnsurePointer(ui, UiSetup.EnsureUnlitMaterial("Assets/.../Laser.mat", Color.white));
```
Run time (screens are built by code into the canvas):
```csharp
var panel = UiPanel.Create(canvas.transform, "Menu", new Vector2(1000, 700));
var surface = panel.Surface;
surface.RootBack = BackToHall;
UiNavigationBar.Create(surface, "C04 · HumPerHW2", UiNavigationBar.Leading.Back, debugTag: true);
var status = surface.AddStatus("");                       // status line (or surface.AddToolbar())
var section = UiKit.Section(surface.Body, "Path", "PATH");
UiKit.Row(section, "Circle", "Circle", "Around the head", iconLeading: "arrow-clockwise", onClick: OnCircle);
UiKit.ChoiceGroup(surface.Body, "Ear", new[] { "Both", "Left", "Right" }, 0, i => SetEar(i), out var buttons);   // UiKit.Select(buttons, ...) shows the state
var page = surface.Push("Debug");                         // a page with its own bar; ‹ pops
ModalDialog.Show(canvas.transform, "Delete this file?", "...",
                 new DialogButton("Cancel", UiStyle.Borderless), new DialogButton("Delete", UiStyle.Destructive, Delete));
UiKit.MotionSuppressed = true;                            // while a stimulus plays: no UI motion
```
Tests measure the laid-out rects (`UiKitLayoutTests`, `UiKitInputsTests`, `UiKitPagesTests`, App `LayoutTests`); design numbers: `docs/design`.

## Quest notes
- Text and panels are drawn through `OVROverlayCanvas` (sharp, compositor layer). The source canvas lives on layer 30,
  which the eye cameras must not render; content added at run time must be on the same layer (UiKit does this).
- Meta's `OVRInputModule` does not work with the Input System only, hence the own pointer.
- Laser material: a URP Unlit **material asset** (single-pass stereo). `Sprites/Default` renders in one eye.
- The grabber and the callout are real 3D objects (URP Unlit material), not part of an overlay canvas, which clips them.
- UI motion runs on unscaled time (works while the Stop dialog freezes `Time.timeScale`).
- Icons are white PNGs tinted at run time (the Vector Graphics package does not compile on Unity 6000.6).
