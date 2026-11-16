using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using GPL.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace GPL.Acoustics
{
    /// <summary>One acoustic setting to measure: <see cref="Apply"/> sets the room (e.g. <c>() => reverb.SetRt60(0.7f)</c>).</summary>
    public sealed class Rt60Condition
    {
        public string Name;
        /// <summary>Target RT60 (s) the T_mid is compared with; NaN for a sweep step.</summary>
        public double TargetRt60 = double.NaN;
        /// <summary>The coefficient of a sweep step (NaN otherwise); for the time estimate.</summary>
        public double ReflectionValue = double.NaN;
        public Action Apply;

        public static Rt60Condition Rt60(string name, ReverbTuner reverb, float seconds) =>
            new Rt60Condition { Name = name, TargetRt60 = seconds, Apply = () => reverb.SetRt60(seconds) };

        public static Rt60Condition Reflection(ReverbTuner reverb, double reflection) =>
            new Rt60Condition { Name = "R=" + Fmt.F(reflection, 4), ReflectionValue = reflection, Apply = () => reverb.SetReflection((float)reflection) };
    }

    /// <summary>What to measure. Defaults = the module protocol (<see cref="Rt60Protocol"/>).</summary>
    public sealed class Rt60Plan
    {
        public string Kind = Rt60Report.KindMeasurement;
        /// <summary>Chamber id for the report and the file name.</summary>
        public string Chamber = "Chamber";
        public ReverbTuner Reverb;
        /// <summary>The mixer group the chamber's spatial sources use (with the "Meta XR Audio Reflection" effect).</summary>
        public AudioMixerGroup Output;
        public List<Rt60Condition> Conditions = new List<Rt60Condition>();
        public float[] Azimuths = Rt60Protocol.Azimuths;
        public float DistanceM = Rt60Protocol.DistanceM;
        public int Repeats = Rt60Protocol.Repeats;
        /// <summary>true = pause or stop every other sound of the chamber; false = may resume. Called once each.</summary>
        public Action<bool> SilenceOthers;
        /// <summary>Called at the end (also after a cancel) to put the chamber's own room back.</summary>
        public Action Restore;
        /// <summary>Report folder; default <c>persistentDataPath/Calibration</c>.</summary>
        public string ReportDirectory;
        /// <summary>Extra header lines (chamber settings, headset volume step if known, ...).</summary>
        public List<KeyValuePair<string, string>> Meta = new List<KeyValuePair<string, string>>();

        /// <summary>A calibration sweep: <paramref name="reflections"/> (default the whole <see cref="Rt60Protocol.SweepReflections"/> grid), 0 deg, 3 repeats.</summary>
        public static Rt60Plan Sweep(string chamber, ReverbTuner reverb, AudioMixerGroup output, double[] reflections = null)
        {
            var plan = new Rt60Plan { Kind = Rt60Report.KindSweep, Chamber = chamber, Reverb = reverb, Output = output,
                                      Azimuths = Rt60Protocol.SingleAzimuth };
            foreach (double r in reflections ?? Rt60Protocol.SweepReflections) plan.Conditions.Add(Rt60Condition.Reflection(reverb, r));
            return plan;
        }

        /// <summary>Seconds the plan takes (silence + recording per recording), for a status line and the progress dialog.</summary>
        public double EstimatedSeconds()
        {
            double s = 0;
            foreach (var c in Conditions) s += SecondsPerRecording(c) * Azimuths.Length * Repeats;
            return s;
        }

        /// <summary>Planned seconds of one recording of <paramref name="c"/>: settle + record + about 0.2 s of frames.</summary>
        public double SecondsPerRecording(Rt60Condition c)
        {
            double t = ExpectedRt60(c);
            return Rt60Protocol.SettleSeconds(t) + Rt60Protocol.RecordSeconds(t) + 0.2;
        }

        double ExpectedRt60(Rt60Condition c)
        {
            if (!double.IsNaN(c.TargetRt60)) return c.TargetRt60;
            if (!double.IsNaN(c.ReflectionValue) && Reverb != null && Reverb.room != null) return Reverb.ExpectedRt60For(c.ReflectionValue);
            return 0.5; // Dry or unknown: the protocol minimums (1 s settle, 3 s record)
        }
    }

    /// <summary>Outcome of a run: the report (in memory also when cancelled or refused) and where it was written (only a completed run is written, PB-27).</summary>
    public sealed class Rt60RunResult
    {
        public Rt60Report Report;
        public bool Completed;
        public string Error = "";
        public string CsvPath = "", TxtPath = "";

        /// <summary>Short status line: <c>T_mid 0.71 s (target 0.7) ...</c> per condition, or the error.</summary>
        public string Summary()
        {
            if (Report == null) return "RT60: " + Error;
            var parts = new List<string>();
            foreach (var c in Report.Conditions())
            {
                var first = Report.Rows.First(r => r.Condition == c);
                if (!double.IsNaN(first.Reflection) && first.Reflection <= 0) { parts.Add(c + " no reverberation"); continue; }
                var s = Report.TmidStats(c);
                bool flagged = Report.TmidFlaggedCount(c, out _) > 0;
                parts.Add(c + " " + (s.N > 0 ? Fmt.F(s.Mean, 2) + " ± " + Fmt.F(s.Sd, 2) + " s" : "n/a") + (flagged ? " (flagged, see report)" : ""));
            }
            string head = Completed ? "RT60 T_mid: " : "RT60 incomplete (" + Error + "): ";
            return head + string.Join(", ", parts) + (TxtPath != "" ? ". Report: " + Path.GetFileName(TxtPath) : "");
        }

        /// <summary>
        /// One line per condition for the result dialog (PB-27): <c>Room 0.7 s   T_mid 0.69 ± 0.01 s   -1.4 %   flagged</c>; Dry
        /// <c>no reverberation</c>; a target outside the table's range adds <c>outside the table</c> (set by the caller via
        /// <paramref name="outsideTable"/>).
        /// </summary>
        public List<string> ResultLines(Func<string, bool> outsideTable = null)
        {
            var lines = new List<string>();
            if (Report == null) return lines;
            foreach (var c in Report.Conditions())
            {
                var first = Report.Rows.First(r => r.Condition == c);
                if (!double.IsNaN(first.Reflection) && first.Reflection <= 0) { lines.Add(c + "   no reverberation"); continue; }
                var s = Report.TmidStats(c);
                string line = c + "   T_mid " + (s.N > 0 ? Fmt.F(s.Mean, 2) + " ± " + Fmt.F(s.Sd, 2) + " s" : "n/a");
                if (s.N > 0 && !double.IsNaN(first.TargetRt60) && first.TargetRt60 > 0)
                {
                    double dev = 100.0 * (s.Mean / first.TargetRt60 - 1.0);
                    line += "   " + (dev >= 0 ? "+" : "") + Fmt.F(dev, 1) + " %";
                }
                if (Report.TmidFlaggedCount(c, out _) > 0) line += "   flagged";
                if (outsideTable != null && outsideTable(c)) line += "   outside the table";
                lines.Add(line);
            }
            return lines;
        }
    }

    /// <summary>
    /// Runs an RT60 plan (README "RT60 measurement protocol"): checks and bypasses the output filters on the AudioListener, creates
    /// its own probe source (routed like the chamber's sources, acoustics on, HRTF intensity 0, flat distance curve, no Doppler),
    /// applies each condition, places the probe at each azimuth (relative to the head yaw at the start, distance
    /// <see cref="Rt60Plan.DistanceM"/>, ear height), records each repeat, analyzes both ears in octave bands and writes the CSV and
    /// TXT report. Lives on its own GameObject: destroying it (scene change, <see cref="Cancel"/>) restores the filters, the sounds
    /// and the room; an incomplete run writes no files (PB-27), its report text goes to the Console.
    /// </summary>
    public class Rt60Runner : MonoBehaviour
    {
        Rt60Plan _plan;
        Action<string> _progress;
        Action<Rt60RunResult> _done;
        Rt60RunResult _result;
        readonly List<IOutputFilter> _bypassed = new List<IOutputFilter>();
        bool _silenced, _finished;
        AudioSource _probe;
        Rt60Meter _meter;

        public bool Running => !_finished;
        public Rt60RunResult Result => _result;
        public Rt60Plan Plan => _plan;

        // ---- progress (PB-27: the progress dialog polls these) ----
        Rt60Eta _eta;
        float _startTime;
        /// <summary>The current step, e.g. <c>Room 0.7 s, -90°, repeat 2 of 3</c> (empty before the first recording).</summary>
        public string Step { get; private set; } = "";
        /// <summary>Done part of the planned time, 0..1.</summary>
        public float Fraction => _eta != null ? (float)_eta.Fraction : 0f;
        /// <summary>Real seconds still needed (the plan's estimate, corrected by the recordings done so far).</summary>
        public double SecondsLeft => _eta != null ? _eta.SecondsLeft(Time.realtimeSinceStartup - _startTime) : _plan != null ? _plan.EstimatedSeconds() : 0.0;

        /// <summary>Starts a run; <paramref name="progress"/> gets status lines, <paramref name="done"/> the result (always once).</summary>
        public static Rt60Runner Begin(Rt60Plan plan, Action<string> progress, Action<Rt60RunResult> done)
        {
            var go = new GameObject("RT60 Runner");
            var runner = go.AddComponent<Rt60Runner>();
            runner._plan = plan;
            runner._progress = progress;
            runner._done = done;
            runner.StartCoroutine(runner.Run());
            return runner;
        }

        /// <summary>Waits until the run has finished (for a chamber coroutine).</summary>
        public IEnumerator Wait()
        {
            while (this != null && Running) yield return null;
        }

        /// <summary>Stops the run; everything is restored; no report files are written (PB-27).</summary>
        public void Cancel()
        {
            if (_finished) return;
            Finish(false, "cancelled");
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (!_finished) Finish(false, "cancelled (runner destroyed)");
        }

        void Say(string s)
        {
            Debug.Log("[GPL] RT60: " + s);
            _progress?.Invoke(s);
        }

        IEnumerator Run()
        {
            var plan = _plan;
            var reverb = plan.Reverb;
            string platform = Rt60Platform.Current;
            bool openSpace = reverb == null || reverb.OpenSpace;
            float w = reverb != null ? reverb.RoomWidth : 0f, d = reverb != null ? reverb.RoomDepth : 0f, h = reverb != null ? reverb.RoomHeight : 0f;
            var now = DateTime.Now;
            string stem = Rt60Report.FileStem(plan.Chamber, now);
            var report = new Rt60Report(stem, plan.Kind, plan.Chamber, platform, w, d, h, openSpace);
            _result = new Rt60RunResult { Report = report };

            if (reverb == null || reverb.room == null) { Finish(false, "no ReverbTuner or shoebox"); yield break; }
            if (plan.Conditions.Count == 0) { Finish(false, "no conditions"); yield break; }
            var meter = _meter = Rt60Meter.Ensure();
            if (meter == null) { Finish(false, "no AudioListener"); yield break; }

            // output chain: bypass known filters, refuse unknown ones
            var chain = new List<string>();
            foreach (var b in meter.GetComponents<Behaviour>())
            {
                if (b == null || b is Rt60Meter || b is AudioListener) continue;
                if (b is IOutputFilter f)
                {
                    chain.Add(f.FilterName + " (" + f.Describe() + ")");
                    if (b.enabled && !f.Bypassed) { f.Bypassed = true; _bypassed.Add(f); }
                    continue;
                }
                if (b.enabled && IsAudioFilter(b)) { Finish(false, "unknown audio filter on the listener: " + b.GetType().Name + "; measurement refused"); yield break; }
            }

            report.AddMeta("date_local", now.ToString("yyyy-MM-dd HH:mm:ss", Fmt.Inv));
            report.AddMeta("date_utc", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", Fmt.Inv));
            report.AddMeta("device", SystemInfo.deviceModel);
            report.AddMeta("os", SystemInfo.operatingSystem);
            report.AddMeta("unity", Application.unityVersion);
            report.AddMeta("app", Application.productName + " " + Application.version);
            report.AddMeta("meta_xr_audio", MetaAudioVersion());
            report.AddMeta("spatializer", AudioSettings.GetSpatializerPluginName());
            AudioSettings.GetDSPBufferSize(out int bufferLength, out int numBuffers);
            report.AddMeta("sample_rate_hz", Fmt.I(AudioSettings.outputSampleRate));
            report.AddMeta("dsp_buffer", Fmt.I(bufferLength) + " x " + Fmt.I(numBuffers));
            report.AddMeta("speaker_mode", AudioSettings.speakerMode.ToString());
            report.AddMeta("room_modeling", reverb.RoomModelingStatus);
            report.AddMeta("calibration", reverb.CalibrationStatus);
            report.AddMeta("walls", "uniform reflection coefficient on all 6 surfaces and all 4 plugin bands; clutter " +
                                    Fmt.F(reverb.room.clutterFactor, 2) + "; shoebox locked to listener: " + Fmt.B(reverb.room.lockPositionToListener));
            report.AddMeta("mixer_group", plan.Output != null ? plan.Output.audioMixer.name + "/" + plan.Output.name : "none (no reflection effect!)");
            report.AddMeta("output_filters", chain.Count == 0 ? "none" : string.Join("; ", chain) + " -> bypassed during recording");
            report.AddMeta("probe", "own AudioSource: spatialBlend 1, Meta acoustics on, HRTF intensity 0 (HRTF EQ off; Meta keeps ITD/ILD), " +
                                    "flat distance curve, Doppler 0, volume 1, impulse amplitude " + Fmt.F(Rt60Meter.ImpulseAmplitude, 2));
            report.AddMeta("positions", "azimuth " + string.Join(", ", Array.ConvertAll(plan.Azimuths, a => Fmt.F(a, 1))) +
                                        " deg (0 = ahead, negative = left, relative to the head yaw at the start), " +
                                        Fmt.F(plan.DistanceM, 2) + " m, ear height; listener at the room centre (shoebox locked to the listener)");
            report.AddMeta("repeats", Fmt.I(plan.Repeats));
            report.AddMeta("ears", "left and right recorded separately; ear mean = mean of the two decay times");
            report.AddMeta("levels", "software levels; headset volume not read");
            foreach (var kv in plan.Meta) report.AddMeta(kv.Key, kv.Value);

            if (plan.Output == null) report.Problems.Add("probe not routed to a mixer group: the room is not rendered");
            if (!reverb.Calibrated) report.Problems.Add(ReverbTuner.UncalibratedNote + " (" + reverb.CalibrationStatus + ")");
            else if (reverb.HeadsetTable) report.Problems.Add("calibration table used on this headset only, not imported into the build (" + reverb.CalibrationStatus + ")");

            // reference pose: the listener now
            var listener = meter.transform;
            Vector3 origin = listener.position;
            float yaw = listener.eulerAngles.y;

            _probe = MakeProbe(plan.Output);
            plan.SilenceOthers?.Invoke(true);
            _silenced = true;
            yield return null;
            yield return null;

            int total = plan.Conditions.Count * plan.Azimuths.Length * plan.Repeats, done = 0;
            _eta = new Rt60Eta(plan.EstimatedSeconds());
            _startTime = Time.realtimeSinceStartup;
            double plannedDone = 0;
            int conditionIndex = 0;
            Say("measuring " + total + " recordings, about " + Fmt.F(plan.EstimatedSeconds() / 60.0, 1) + " min. Stay still and quiet.");
            foreach (var c in plan.Conditions)
            {
                conditionIndex++;
                double perRecording = plan.SecondsPerRecording(c);
                c.Apply?.Invoke();
                yield return null;
                yield return null;
                double reflection = reverb.Custom ? reverb.Reflection : double.NaN;
                double expected = !double.IsNaN(c.TargetRt60) ? Math.Max(c.TargetRt60, reverb.ExpectedRt60()) : reverb.ExpectedRt60();
                double settle = Rt60Protocol.SettleSeconds(expected), record = Rt60Protocol.RecordSeconds(expected);
                report.AddMeta("timing " + c.Name, "settle " + Fmt.F(settle, 2) + " s, record " + Fmt.F(record, 2) + " s; " + reverb.Note());
                for (int p = 0; p < plan.Azimuths.Length; p++)
                {
                    float az = plan.Azimuths[p];
                    var rot = Quaternion.Euler(0f, yaw, 0f);
                    float rad = az * Mathf.Deg2Rad;
                    _probe.transform.position = origin + rot * new Vector3(plan.DistanceM * Mathf.Sin(rad), 0f, plan.DistanceM * Mathf.Cos(rad));
                    for (int rep = 1; rep <= plan.Repeats; rep++)
                    {
                        done++;
                        Step = StepText(plan, c, conditionIndex, az, rep);
                        Say(c.Name + ", " + Fmt.F(az, 0) + " deg, repeat " + rep + " (" + done + "/" + total + ")");
                        float[] left = null, right = null; int fs = 48000;
                        yield return meter.Record(_probe, settle, record, (l, r, rate) => { left = l; right = r; fs = rate; });
                        plannedDone += perRecording;
                        _eta.Update(plannedDone, Time.realtimeSinceStartup - _startTime);
                        if (left == null || left.Length < fs / 2)
                        {
                            report.Problems.Add(c.Name + " az " + Fmt.F(az, 0) + " repeat " + rep + ": nothing recorded");
                            continue;
                        }
                        var bl = ImpulseResponseAnalysis.Analyze(left, fs);
                        yield return null; // spread the filtering over frames
                        var br = ImpulseResponseAnalysis.Analyze(right, fs);
                        report.Add(c.Name, c.TargetRt60, reflection, p + 1, az, plan.DistanceM, rep, bl, br);
                        var tm = Rt60Protocol.Tmid(Mean(bl, br));
                        Debug.Log("[GPL] RT60: " + c.Name + " az=" + Fmt.F(az, 0) + " rep=" + rep + " T_mid=" + Fmt.F(tm, 3) + " s");
                    }
                }
            }
            Finish(true, "");
            Destroy(gameObject);
        }

        /// <summary>Sweep: <c>Reflection 0.85 (14 of 21), repeat 1 of 3</c>; measurement: <c>Room 0.7 s, -90°, repeat 2 of 3</c>.</summary>
        static string StepText(Rt60Plan plan, Rt60Condition c, int index, float az, int rep)
        {
            string repeat = plan.Repeats > 1 ? ", repeat " + rep + " of " + plan.Repeats : "";
            if (!double.IsNaN(c.ReflectionValue))
                return "Reflection " + Fmt.F(c.ReflectionValue, 3) + " (" + index + " of " + plan.Conditions.Count + ")" + repeat + ".";
            string where = plan.Azimuths.Length > 1 ? ", " + Fmt.F(az, 0) + "°" : "";
            return c.Name + where + repeat + ".";
        }

        static BandDecay[] Mean(BandDecay[] l, BandDecay[] r)
        {
            var m = new BandDecay[l.Length];
            for (int i = 0; i < l.Length; i++) m[i] = ImpulseResponseAnalysis.Mean(l[i], r[i]);
            return m;
        }

        void Finish(bool completed, string error)
        {
            if (_finished) return;
            _finished = true;
            foreach (var f in _bypassed) if (f != null) f.Bypassed = false;
            _bypassed.Clear();
            if (_meter != null) _meter.Abort();
            if (_probe != null) Destroy(_probe.gameObject);
            if (_silenced) { _silenced = false; try { _plan.SilenceOthers?.Invoke(false); } catch (Exception e) { Debug.LogException(e); } }
            try { _plan?.Restore?.Invoke(); } catch (Exception e) { Debug.LogException(e); }

            _result = _result ?? new Rt60RunResult();
            _result.Completed = completed;
            _result.Error = error ?? "";
            if (!completed && _result.Report != null) _result.Report.Problems.Insert(0, "INCOMPLETE: " + error);
            // PB-27 (PO 2026-11-16): an incomplete run (cancelled, refused, failed) leaves no report files; the Console keeps its text
            if (_result.Report != null && completed) Write(_result);
            else if (_result.Report != null) Debug.Log("[GPL] RT60: incomplete run, no report written (PB-27).\n" + _result.Report.Text());
            Debug.Log("[GPL] RT60: " + _result.Summary());
            _done?.Invoke(_result);
        }

        void Write(Rt60RunResult result)
        {
            try
            {
                string dir = !string.IsNullOrEmpty(_plan?.ReportDirectory) ? _plan.ReportDirectory : Path.Combine(Application.persistentDataPath, "Calibration");
                Directory.CreateDirectory(dir);
                result.CsvPath = Path.Combine(dir, result.Report.ReportId + ".csv");
                result.TxtPath = Path.Combine(dir, result.Report.ReportId + ".txt");
                AtomicFile.WriteAllText(result.CsvPath, result.Report.Csv());
                AtomicFile.WriteAllText(result.TxtPath, result.Report.Text());
                Debug.Log("[GPL] RT60 report written: " + result.TxtPath + "\n" + result.Report.Text());
            }
            catch (Exception e)
            {
                result.Error = (result.Error == "" ? "" : result.Error + "; ") + "report not written: " + e.Message;
                Debug.LogError("[GPL] RT60: report not written: " + e);
            }
        }

        AudioSource MakeProbe(AudioMixerGroup output)
        {
            var go = new GameObject("RT60 Probe");
            go.SetActive(false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.volume = 1f;
            s.spatialBlend = 1f;
            s.spatialize = true;
            s.spatializePostEffects = false;
            s.dopplerLevel = 0f;
            s.spread = 0f;
            s.bypassReverbZones = true;
            s.rolloffMode = AudioRolloffMode.Custom;
            s.maxDistance = 100f;
            s.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 1f, 1f));
            s.outputAudioMixerGroup = output;
            var meta = go.AddComponent<MetaXRAudioSource>();
            meta.EnableSpatialization = true;
            meta.EnableAcoustics = true;
            var exp = go.AddComponent<MetaXRAudioSourceExperimentalFeatures>();
            exp.HrtfIntensity = 0f;
            go.SetActive(true);
            return s;
        }

        /// <summary>Built-in Unity filters and any script with OnAudioFilterRead.</summary>
        static bool IsAudioFilter(Behaviour b)
        {
            if (b is AudioLowPassFilter || b is AudioHighPassFilter || b is AudioEchoFilter || b is AudioDistortionFilter ||
                b is AudioReverbFilter || b is AudioChorusFilter) return true;
            return b is MonoBehaviour && b.GetType().GetMethod("OnAudioFilterRead",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null;
        }

        [DllImport("MetaXRAudioUnity")] // MetaXRAudioNativeInterface.UnityNativeInterface.binaryName
        static extern void MetaXRAudio_Unity_GetVersion(ref int major, ref int minor, ref int patch);

        static string MetaAudioVersion()
        {
            try
            {
                int a = 0, b = 0, c = 0;
                MetaXRAudio_Unity_GetVersion(ref a, ref b, ref c);
                return "native " + a + "." + b + "." + c + " (package 85.0.0)";
            }
            catch (Exception e) { return "unknown (" + e.GetType().Name + ")"; }
        }
    }
}
