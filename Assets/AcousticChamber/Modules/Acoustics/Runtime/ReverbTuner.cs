using GPL.Core;
using Meta.XR.Audio;
using UnityEngine;

namespace GPL.Acoustics
{
    /// <summary>
    /// Room acoustics of the Meta XR Audio shoebox:
    /// 1. Switches room modeling and late reverberation on (both OFF by default in the plugin). The room is only audible when the
    ///    sources are routed to an audio mixer group with the "Meta XR Audio Reflection" effect (Editor: <c>ReflectionMixer</c>).
    /// 2. Sets a reverberation time. Meta's shoebox component offers only material presets, so it is switched off and the room is
    ///    sent to the plugin from here every frame with one uniform reflection coefficient on all six surfaces, taken from a MEASURED
    ///    table (<see cref="Calibration"/>). <see cref="UsePreset"/> hands control back to the component (its material presets).
    /// Check the rendered time on the headset with <see cref="Rt60Meter"/>.
    /// </summary>
    public class ReverbTuner : MonoBehaviour
    {
        public MetaXRAudioRoomAcousticProperties room;
        [Tooltip("Room size used for the V/S scaling; falls back to the shoebox component's size.")]
        public ShoeboxRoom roomSize;

        /// <summary>Coefficient table; replace it before use for another plugin or a re-measurement.</summary>
        public Rt60Calibration Calibration { get; set; } = Rt60Calibration.MetaXrAudioShoebox;

        public bool Custom { get; private set; }
        /// <summary>The requested RT60 (s); 0 after <see cref="SetReflection"/>.</summary>
        public float TargetRt60 { get; private set; }
        public float Reflection { get; private set; }
        /// <summary>Plugin result of switching room modeling on (0 = success), for the log and the Debug page.</summary>
        public string RoomModelingStatus { get; private set; } = "not set";

        readonly float[] _walls = new float[6 * 4];
        readonly float[] _clutter = new float[4];

        void Awake() => EnableRoomModeling();

        public void EnableRoomModeling()
        {
            var plugin = MetaXRAudioNativeInterface.Interface;
            int roomResult = plugin.SetEnabled((int)EnableFlag.SIMPLE_ROOM_MODELING, true);
            int lateResult = plugin.SetEnabled((int)EnableFlag.LATE_REVERBERATION, true);
            RoomModelingStatus = "room_modeling=" + (roomResult == 0 ? "on" : "failed_" + roomResult) +
                                 " late_reverb=" + (lateResult == 0 ? "on" : "failed_" + lateResult);
            Debug.Log("[GPL] Reverb: " + RoomModelingStatus);
        }

        /// <summary>True after <see cref="SetDry"/> until the next <see cref="SetRt60"/>.</summary>
        public bool Dry { get; private set; }

        /// <summary>Applies an experience chamber's room: Dry = no room modeling and no late reverb, else the room's RT60.</summary>
        public void Apply(RoomChoice choice)
        {
            if (choice == RoomChoice.Dry) SetDry();
            else SetRt60(RoomChoices.Rt60(choice));
        }

        /// <summary>Switches room modeling and late reverberation off: the sources sound dry (the mixer effect stays in the path).</summary>
        public void SetDry()
        {
            Custom = false; // Update stops pushing the room
            Dry = true;
            var plugin = MetaXRAudioNativeInterface.Interface;
            int roomResult = plugin.SetEnabled((int)EnableFlag.SIMPLE_ROOM_MODELING, false);
            int lateResult = plugin.SetEnabled((int)EnableFlag.LATE_REVERBERATION, false);
            RoomModelingStatus = "room_modeling=" + (roomResult == 0 ? "off" : "failed_" + roomResult) +
                                 " late_reverb=" + (lateResult == 0 ? "off" : "failed_" + lateResult);
            Debug.Log("[GPL] Reverb: dry (" + RoomModelingStatus + ")");
        }

        /// <summary>Uniform coefficient for a target RT60 (s) from the calibration table, scaled to this room's V/S.</summary>
        public void SetRt60(float seconds)
        {
            if (room == null) { Debug.LogWarning("[GPL] Reverb: no shoebox component assigned"); return; }
            if (Dry) { Dry = false; EnableRoomModeling(); }
            TargetRt60 = Mathf.Max(0.05f, seconds);
            ApplyUniform((float)Calibration.ReflectionForRt60(TargetRt60, VolumeOverSurface()));
            Debug.Log("[GPL] Reverb: " + Note());
        }

        /// <summary>Set the coefficient directly (calibration sweeps); the target RT60 is unknown then (0).</summary>
        public void SetReflection(float reflection)
        {
            if (room == null) return;
            TargetRt60 = 0f;
            ApplyUniform(Mathf.Clamp01(reflection));
        }

        /// <summary>Back to the shoebox component's material presets.</summary>
        public void UsePreset()
        {
            Custom = false;
            if (room != null) room.enabled = true;
        }

        /// <summary>RT60 the calibration table expects for the current custom coefficient, in this room.</summary>
        public float ExpectedRt60() => (float)Calibration.Rt60ForReflection(Reflection, VolumeOverSurface());

        /// <summary>Calibrated estimate of the component's material presets (mean over six surfaces and four bands).</summary>
        public float PresetRt60()
        {
            if (room == null) return 0f;
            var presets = new[] { room.leftMaterial, room.rightMaterial, room.ceilingMaterial, room.floorMaterial, room.frontMaterial, room.backMaterial };
            float sum = 0f;
            foreach (var preset in presets)
                foreach (float band in MetaXRAudioRoomAcousticProperties.GetMaterialPresetBands(preset)) sum += band;
            return (float)Calibration.Rt60ForReflection(sum / (presets.Length * 4), VolumeOverSurface());
        }

        /// <summary>Log note: <c>mode=custom rt60_target_s=0.7 reflection=0.812 rt60_expected_s=0.7</c> or <c>mode=preset ...</c>.</summary>
        public string Note() => Custom
            ? "mode=custom rt60_target_s=" + Fmt.F(TargetRt60) + " reflection=" + Fmt.F(Reflection, 4) + " rt60_expected_s=" + Fmt.F(ExpectedRt60(), 2)
            : "mode=preset preset=" + (room != null ? room.floorMaterial.ToString() : "none") + " rt60_expected_s=" + Fmt.F(PresetRt60(), 2);

        /// <summary>One line for a Debug status.</summary>
        public string Describe() => Custom
            ? "RT60 " + Fmt.F(TargetRt60, 2) + " s (calibrated, R " + Fmt.F(Reflection, 3) + ")"
            : "Meta preset " + (room != null ? room.floorMaterial.ToString() : "?") + ", RT60 about " + Fmt.F(PresetRt60(), 1) + " s (calibrated estimate)";

        void Update()
        {
            if (Custom) Push();
        }

        void OnDisable() => UsePreset();

        void ApplyUniform(float reflection)
        {
            Reflection = reflection;
            for (int i = 0; i < _walls.Length; i++) _walls[i] = Reflection;
            Custom = true;
            room.enabled = false; // its Update would overwrite the room every frame
            Push();
        }

        double VolumeOverSurface() => roomSize != null
            ? roomSize.VolumeOverSurface
            : RoomMath.VolumeOverSurface(room.width, room.depth, room.height);

        void Push()
        {
            var plugin = MetaXRAudioNativeInterface.Interface;
            plugin.SetAdvancedBoxRoomParameters(room.width, room.height, room.depth, room.lockPositionToListener, room.transform.position, _walls);
            float factor = room.clutterFactor; // same band spreading as Meta's component
            for (int band = _clutter.Length - 1; band >= 0; --band) { _clutter[band] = factor; factor *= 0.5f; }
            plugin.SetRoomClutterFactor(_clutter);
        }
    }
}
