# GPL.UI

Readable, clickable GUI for Meta Quest: overlay canvases placed in front of the participant, a controller ray pointer,
themed buttons and text (TextMeshPro, English + 中文), a number pad, modal dialogs, localization.
Generalized from HW2's `UiPointer`, `GuiPanelPlacer`, `ParticipantIdField`, `LocalizedText`, `Localization` and the GUI
helpers of `HumPerSetup`.

**Copy:** this folder + `Core/` (GPL.Core). **Dependencies:** Meta XR Core SDK (`Oculus.VR`: OVRCameraRig, OVRInput,
OVROverlayCanvas, OVRControllerHelper), uGUI + TextMeshPro (`com.unity.ugui` 2.x), URP (laser material).

## Parts
| Part | What |
|---|---|
| `Core/Localization.cs` | `UiLanguage` (English, Chinese), `StringTable`, `Loc` (current language, `Changed`, lookup; later tables override; missing key -> `[key]`; labels exactly "English" / "中文") |
| `Core/DigitEntry.cs` | number-pad model: digits only, max length |
| `Runtime/UiTheme` | design tokens asset (colors, fonts, type scale, control sizes); defaults = AcousticChamber26 design system |
| `Runtime/UiKit` | builders: `Rect`, `Box`, `Background`, `Text`, `LocText`, `Button` (style = Primary / Neutral / Warning / Danger), `LocButton`, `Badge`. New objects take the parent's layer. Rounded corners from a procedural 9-slice sprite (no texture assets) |
| `Runtime/UiRoot` | one per scene: sets the theme, registers the module strings |
| `Runtime/OverlayPanel` | places a canvas 2 m ahead, 8° below eye level, facing the eye; scale follows distance; waits for valid head height; re-places 2 frames after a system recenter; hides layers 30/31 from the eye cameras |
| `Runtime/UiPointer` | ray from the controller (trigger = click, last trigger owns the ray), white laser, accent on a target, hover outline; `UiPointer.Modal` limits it to a dialog |
| `Runtime/ITextEntry`, `NumberPadEntry` | swappable text entry; the number pad (3 x 4, Clear, ←) shows the formatted value live (`StudyId.FromDigits`: `1` -> `S0001`) |
| `Runtime/ModalDialog` | dim + box with a colored top bar, title, message, buttons; modal for the pointer; `Abandoned` if destroyed without `Close` |
| `Runtime/StopDialog` | the Stop dialog: real session Keep going / Drop out (asks once more, "Drop out S0012?"); Debug run Keep going / Back to the chamber menu; returns a `StopChoice`. `Freeze` (time scale 0 + audio pause, nested, restores the old values) while open |
| `Runtime/UiShowcase` | headset test panel of the module (no study logic) |
| `Editor/UiSetup` | `EnsureFontAsset` (dynamic SDF from a .ttf/.otf), `EnsureTheme`, `EnsureUnlitMaterial`, `CreateOverlayCanvas`, `EnsurePointer`, `EnsureUiRoot`, `EnsureGroup`, `EnsureLayerName` |

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
UiKit.LocButton(canvas.transform, "Next", UiStrings.Next, UiStyle.Primary, new Vector2(0, -250), new Vector2(300, 80), OnNext);
var pad = NumberPadEntry.Create(canvas.transform, new Vector2(-250, -50), StudyId.MaxDigits, StudyId.FromDigits, "S____");
ModalDialog.Show(canvas.transform, "Stop the session?", "...", UiKit.Theme.danger,
                 new DialogButton("Keep going", UiStyle.Primary), new DialogButton("Drop out", UiStyle.Danger, DropOut));
```

## Quest notes
- Text and panels are drawn through `OVROverlayCanvas` (sharp, compositor layer). The source canvas lives on layer 30,
  which the eye cameras must not render; content added at run time must be on the same layer (UiKit does this).
- Meta's `OVRInputModule` does not work with the Input System only, hence the own pointer.
- Laser material: a URP Unlit **material asset** (single-pass stereo). `Sprites/Default` renders in one eye.
- Glyphs missing from the font show as boxes. Noto Sans TC has no "⌫" (U+232B); the backspace key uses "←".
