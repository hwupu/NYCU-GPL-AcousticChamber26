# Quest system keyboard (PB-14)

**Status:** investigated without a headset (2026-11-07). Code and test panel ready; **the headset test is open**.
Until it passes, the number pad stays the only text entry (CLAUDE.md rule 9, INSTRUCTIONS.md section 3).

## HW2 symptom
`TouchScreenKeyboard.Open` worked in one HW2 build. In later builds it returned a keyboard whose status was "Visible", but the
headset never showed it and the app never lost input focus. The manifest feature, focus awareness and target SDK were all correct.

## What the project shows (read from the files)
| Item | HW2 | AcousticChamber26 |
|---|---|---|
| Unity | 6000.6.6f1 | 6000.6.6f1 |
| Application entry point (`androidApplicationEntry`) | 2 = **GameActivity** | 2 = **GameActivity** |
| OVR project config "Requires System Keyboard" | on | **off** |
| Manifest `oculus.software.overlay_keyboard` | present | absent (added by Meta's manifest preprocessor when the setting is on: `OVRManifestPreprocessor`) |
| XR backend | OpenXR + Meta XR Core | OpenXR + Meta XR Core 207 |

## Leads (from the web; not verified on our headset)
- Unity issue tracker: with the GameActivity entry point, `TouchScreenKeyboard.status` stays on "Visible" and the input field
  above the keyboard is hidden. The Activity entry point behaves correctly. Unity lists a fix in 6000.0.44f1 / 6000.1.0b12 /
  6000.2.0a5. Our 6000.6 should contain it, but HW2 failed on that same version, so the fix may not cover Quest.
- Meta forum: Unity 6 defaults to GameActivity; the GameActivity library removed the input box above the system keyboard. One
  user reported that the old Activity entry point crashed at startup.
- Khronos forum: OpenXR has no standard keyboard API. On Quest the keyboard is the OS overlay, requested through the manifest feature.
- An app-level keyboard (XR Interaction Toolkit "Spatial Keyboard" sample) avoids the OS keyboard entirely.

Most likely cause of the HW2 failure: **GameActivity**. The status that reads Visible while no keyboard appears is exactly the
Unity bug.

## Code
- `GPL.UI` `NativeKeyboardEntry` (`ITextEntry` over `TouchScreenKeyboard`): `Open()`, value through `Changed`, and `Diagnosis()`,
  which reports "NOT SHOWN" when the status says Visible but the app has not lost focus for 2 s.
- App `KeyboardTestPanel`, `Magic > App > Keyboard Test Panel (open scene)`: buttons for the number and text keyboards, the
  returned value, and the live diagnosis.

## Headset test (PO)
1. Open the Sandbox scene and run `Magic > App > Keyboard Test Panel (open scene)`. Save.
2. In the Build Profile, put Sandbox first for this test build only, and put MainHall back first afterwards.
3. Run each case and write down the diagnosis line and whether the keyboard appears and returns digits:

| # | Requires System Keyboard | Entry point | Expected |
|---|---|---|---|
| A | off | GameActivity | not shown (no manifest feature) — the control case |
| B | on | GameActivity | HW2 setting: shown, or "NOT SHOWN" (the bug) |
| C | on | Activity | shown if GameActivity is the cause (watch for the startup crash reported on the Meta forum) |

Settings: Meta XR > Project Setup Tool / OVR project config "Requires System Keyboard"; Player > Android > Other Settings >
Application Entry Point. Both change project-wide files: put them back after the test unless the result says to keep them.

## Decision after the test
- C works and B does not: GameActivity is the cause. Decide whether to switch the entry point (that touches the whole app;
  re-test audio and the Stop dialog) or keep the number pad.
- Neither works: document the keyboard as unusable and close PB-14 with the number pad (or an app-level keyboard later).

Sources: [Unity issue tracker: GameActivity TouchScreenKeyboard](https://issuetracker.unity3d.com/issues/android-gameactivity-touchscreenkeyboard-the-input-field-above-the-keyboard-is-always-hidden-and-touchscreenkeyboard-dot-status-is-stuck-on-visible-when-used-with-gameactivity-application-entry-point),
[Meta forum: input box gone with GameActivity](https://communityforums.atmeta.com/discussions/Questions_Discussions/quest-system-keyboard-input-box-is-gone-on-the-new-gameactivity-entry-point-unit/1345610),
[Khronos: OpenXR popup keyboard](https://community.khronos.org/t/openxr-popup-keyboard-for-vive-and-quest/109622),
[Unity discussions: XRI virtual keyboard](https://discussions.unity.com/t/xri-virtual-keyboard-not-showing-up/911021).
