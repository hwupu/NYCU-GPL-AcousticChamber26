# 🎧 AcousticChamber26  

**Step into a VR sound chamber and interact with spatial sound.** Walk into the **Main Hall**. Each door leads to a
**chamber**, a small lab built to play with one acoustic idea.

## 🧪 The chambers
| ID | Chamber | Try this |
|---|---|---|
| **C01** | 🎯 **MovingSource** | *Grab a sound and throw it around.* Move a source around the room and hear its direction, distance and echoes change. |
| **C02** | 🙈 **HiddenSource** | *Hide and seek, by ear only.* A sound is hidden somewhere around you. Point to it. How close can you get? |
| **C03** | 🦻 **HearingLoss** | *Hear the world with someone else's ears.* Turn down the highs in one ear or both, and listen to how speech and space fall apart. |
| **C04** | 🗣️ **HumPerHW2** | *Can your eyes fool your ears?* Steer an invisible noise until it seems to come from a little card in front of you. Find out how far apart sound and sight can drift before you notice. |

## 📲 Get the app onto your headset
You need: a **Meta Quest 3** in developer mode, a USB-C cable, **Unity 6000.6.6f1** with Android Build Support, **Git LFS**
and **Meta Quest Developer Hub** (MQDH).

1. **Get the project**
   ```
   git lfs install
   git clone https://github.com/hwupu/NYCU-GPL-AcousticChamber26.git
   cd NYCU-GPL-AcousticChamber26
   git lfs pull
   ```
   Windows: turn on **Developer Mode** (Settings > System > For developers) before cloning.
2. **Open it** in Unity Hub (Unity 6000.6.6f1). The first import takes a few minutes. ☕
3. **Build:** `File > Build Profiles` > **Meta Quest**, plug in the headset, **Build And Run**.
4. **Play:** in the headset, open **Library > Unknown sources > AcousticChamber26**.

**Windows only:** you can also skip the build and play in the editor through **Quest Link**: start Link in the headset,
open `Assets/AcousticChamber/App/Scenes/MainHall.unity` and press Play.

**Mac:** Quest Link does not exist on macOS, so always build (step 3). You can also install an APK by dragging it onto the
headset in MQDH.

## 🕹️ Inside the lab
- **Point and click:** aim either controller's ray and pull the **trigger**.
- **Y button (left):** stop or pause, from anywhere.
- **Hold the Meta button:** recenter, if "forward" drifts.

1. **Main Hall:** pick a chamber. Switch the language here: English / 中文.
2. **Chamber menu:** type a **Study ID** (up to 4 digits; `1` becomes `S0001`), then choose
   - **Start**: a real session (data is saved),
   - **Debug**: play freely, change settings and save them as presets, manage the data files, or
   - **Back to Hall**.
3. **Hand the headset over.** The participant presses ready, looks ahead, and the chamber begins.
4. **Need a break?** Press **Y**: *Keep going*, *Pause* (continue later) or *Drop out*.
   To continue a paused session, enter the same Study ID and choose **Resume**.

## 💾 Get your data
Every session saves a CSV file (one row per event) and a small `.session` file used for resuming. They are stored in one
folder per chamber on the headset:
`Android/data/com.nycu.AcousticChamber26/files/<Chamber>/`

**Windows:** plug in the headset, choose **Allow** for file access inside the headset, then in File Explorer open
`This PC > Quest 3 > Internal shared storage > Android > data > com.nycu.AcousticChamber26 > files`.

**Mac:** macOS can't open the Quest as a drive. Use `adb` (install it once with `brew install --cask android-platform-tools`),
allow USB debugging in the headset, then run:
```
adb devices
adb pull /sdcard/Android/data/com.nycu.AcousticChamber26/files/ ./AcousticChamber-data
```
Or use the free app [OpenMTP](https://openmtp.ganeshrvel.com) to drag and drop.
