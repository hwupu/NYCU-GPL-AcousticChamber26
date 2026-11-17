using System.Collections;
using GPL.Core;
using GPL.SpatialSources;
using GPL.Stimuli;
using UnityEngine;

namespace AcousticChamber.Chambers.HumPerFinal
{
    /// <summary>
    /// The audio anchors (Design.md "Stimuli"): the rubber duck and the trumpet, visible and sounding, one at a time in a loop —
    /// duck sound, gap, trumpet sound, gap, repeat. Placed once around the head (room-fixed: a recenter does not move them). Each
    /// recording is leveled at load to the target's RMS (<see cref="LevelDbfs"/>, GPL.Stimuli <c>StimulusClips.Leveled</c>).
    /// The loop runs on scaled time, so the Stop dialog's freeze (time scale 0, listener paused) holds sound and timer together.
    /// </summary>
    public class AnchorLoop : MonoBehaviour
    {
        [Tooltip("Rubber duck source (child 'Model' = the visible placeholder).")]
        public PlacedSource duck;
        [Tooltip("Trumpet source (child 'Model' = the visible placeholder).")]
        public PlacedSource trumpet;
        [Tooltip("Recordings (Decompress On Load). GPL.Stimuli library: Freesound 348791 toy ducks quacking, OpenAIR tr-1780d-fanfare.wav (Step 4).")]
        public AudioClip duckRecording, trumpetRecording;
        public float levelDbfs = -26f;

        AudioClip _duck, _trumpet;
        double _duckGainDb, _trumpetGainDb;
        Coroutine _loop;
        bool _paused;
        float _gapAfterDuck, _gapAfterTrumpet;

        /// <summary>True from <see cref="Begin"/> to <see cref="End"/>.</summary>
        public bool Playing => _loop != null;
        public bool Paused => _paused;
        /// <summary>Times a sound started since <see cref="Begin"/> (duck and trumpet alternate).</summary>
        public int SoundsStarted { get; private set; }

        public GameObject DuckModel => Model(duck);
        public GameObject TrumpetModel => Model(trumpet);

        static GameObject Model(PlacedSource s)
        {
            var t = s != null ? s.transform.Find("Model") : null;
            return t != null ? t.gameObject : null;
        }

        void Awake()
        {
            ShowModels(false);
        }

        void OnDestroy()
        {
            StimulusClips.Release(_duck);
            StimulusClips.Release(_trumpet);
        }

        /// <summary>The leveled copies (made once, on first use).</summary>
        bool EnsureClips()
        {
            if (_duck != null && _trumpet != null) return true;
            if (duckRecording == null || trumpetRecording == null) { Debug.LogError("[Chamber:HumPerFinal] AnchorLoop: recordings missing (Step 4)."); return false; }
            _duck = StimulusClips.Leveled(duckRecording, levelDbfs, out _duckGainDb, "Anchor duck");
            _trumpet = StimulusClips.Leveled(trumpetRecording, levelDbfs, out _trumpetGainDb, "Anchor trumpet");
            if (duck != null) duck.Source.clip = _duck;
            if (trumpet != null) trumpet.Source.clip = _trumpet;
            Debug.Log("[Chamber:HumPerFinal] Anchors leveled to " + Fmt.F(levelDbfs, 1) + " dBFS RMS: duck " + duckRecording.name + " (" +
                      Fmt.F(_duck.length, 2) + " s, gain " + Fmt.F(_duckGainDb, 2) + " dB), trumpet " + trumpetRecording.name + " (" +
                      Fmt.F(_trumpet.length, 2) + " s, gain " + Fmt.F(_trumpetGainDb, 2) + " dB)");
            return true;
        }

        /// <summary>Places both anchors on the circle around <paramref name="origin"/> (facing it), shows them and starts the loop with the duck.</summary>
        public void Begin(Vector3 origin, Quaternion yaw, float radiusM, float duckAzDeg, float trumpetAzDeg, float gapAfterDuckS, float gapAfterTrumpetS)
        {
            End();
            if (duck == null || trumpet == null || !EnsureClips()) return;
            Put(duck, origin, yaw, radiusM, duckAzDeg);
            Put(trumpet, origin, yaw, radiusM, trumpetAzDeg);
            _gapAfterDuck = Mathf.Max(0f, gapAfterDuckS);
            _gapAfterTrumpet = Mathf.Max(0f, gapAfterTrumpetS);
            _paused = false;
            SoundsStarted = 0;
            ShowModels(true);
            _loop = StartCoroutine(Loop());
        }

        static void Put(PlacedSource s, Vector3 origin, Quaternion yaw, float radius, float az)
        {
            s.Anchor = PlacedSource.Anchoring.RoomFixed;
            s.Place(origin, yaw, radius, az);
            s.transform.rotation = Ring.FacingOrigin(s.transform.position, origin);
        }

        /// <summary>Stops the sound and hides the models.</summary>
        public void End()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
            _paused = false;
            if (duck != null && duck.Source != null) duck.Source.Stop();
            if (trumpet != null && trumpet.Source != null) trumpet.Source.Stop();
            ShowModels(false);
        }

        /// <summary>Holds the loop (sound and timer), e.g. while an RT60 measurement records; the models stay visible.</summary>
        public void Pause(bool paused)
        {
            if (!Playing || paused == _paused) return;
            _paused = paused;
            foreach (var s in new[] { duck, trumpet })
            {
                if (s == null || s.Source == null) continue;
                if (paused) s.Source.Pause();
                else s.Source.UnPause();
            }
        }

        void ShowModels(bool on)
        {
            var d = DuckModel;
            var t = TrumpetModel;
            if (d != null) d.SetActive(on);
            if (t != null) t.SetActive(on);
        }

        IEnumerator Loop()
        {
            for (int i = 0; ; i = 1 - i)
            {
                var s = i == 0 ? duck : trumpet;
                float wait = s.Source.clip.length + (i == 0 ? _gapAfterDuck : _gapAfterTrumpet);
                s.Source.Play();
                SoundsStarted++;
                for (float t = 0f; t < wait;)
                {
                    yield return null;
                    if (!_paused) t += Time.deltaTime; // scaled: 0 while the Stop dialog freezes the session
                }
            }
        }

        /// <summary>Software RMS of the leveled anchor clips (dBFS), for <c>anchor_level_dbfs</c>; NaN before the first Begin.</summary>
        public float LevelOfDuck => _duck != null && duck != null ? duck.LevelDbfs : float.NaN;
        public float LevelOfTrumpet => _trumpet != null && trumpet != null ? trumpet.LevelDbfs : float.NaN;

        /// <summary>
        /// Log note of the <c>anchors</c> event: listener-relative positions, clips, gaps:
        /// <c>duck=x,y,z;trumpet=x,y,z clip_duck=348791__nebyoolae__toy-ducks-quacking gain_duck_db=.. clip_trumpet=.. gap_duck_s=1.5 gap_trumpet_s=2.5</c>.
        /// </summary>
        public string Note(Transform head)
        {
            string P(PlacedSource s)
            {
                var p = head != null ? head.InverseTransformPoint(s.transform.position) : s.transform.position;
                return Fmt.F(p.x, 3) + "," + Fmt.F(p.y, 3) + "," + Fmt.F(p.z, 3);
            }
            return "duck=" + P(duck) + ";trumpet=" + P(trumpet) +
                   " clip_duck=" + (duckRecording != null ? duckRecording.name : "") + " gain_duck_db=" + Fmt.F(_duckGainDb, 2) +
                   " clip_trumpet=" + (trumpetRecording != null ? trumpetRecording.name : "") + " gain_trumpet_db=" + Fmt.F(_trumpetGainDb, 2) +
                   " level_dbfs=" + Fmt.F(LevelOfDuck, 2) + "," + Fmt.F(LevelOfTrumpet, 2) +
                   " gap_duck_s=" + Fmt.F(_gapAfterDuck) + " gap_trumpet_s=" + Fmt.F(_gapAfterTrumpet);
        }
    }
}
