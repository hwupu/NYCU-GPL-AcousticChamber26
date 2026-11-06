using System;
using System.Collections;
using GPL.Input;
using GPL.Logging;
using GPL.Params;
using GPL.Session;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// What a chamber gives the generic <see cref="ChamberMenu"/>: its parameter schema and default file, its log columns, a
    /// short description of an unfinished session, and the run itself (a coroutine). One per chamber scene, on the chamber
    /// menu's canvas. The menu owns the session (files, Start / Resume / Start new, Stop dialog, end rows).
    /// </summary>
    public abstract class ChamberBehaviour : MonoBehaviour
    {
        [Tooltip("Config/default.params: the preset 'Default'. Empty = the schema's defaults.")]
        public TextAsset defaultParams;

        public abstract ParameterSchema Schema { get; }

        /// <summary>The chamber's own CSV columns. Declare <see cref="ChamberMenu.VolumeColumn"/> to get the volume step as a column.</summary>
        public abstract LogSchema LogSchema { get; }

        /// <summary>The text of the preset "Default".</summary>
        public string DefaultParamsText => defaultParams != null ? defaultParams.text : Schema.Defaults().Serialize("defaults of the schema");

        /// <summary>The unfinished-session line of the chamber menu, e.g. "Block 2 of 4, 3 trials done".</summary>
        public virtual string Describe(SessionRecord r) => r.Describe();

        /// <summary>Why this session cannot be resumed (e.g. its plan hash no longer matches), or null. Checked before Resume.</summary>
        public virtual string ResumeProblem(SessionRecord r, ParameterSet snapshot) => null;

        /// <summary>
        /// The run: a real session, a resumed one (<see cref="ChamberRun.IsResume"/>) or a Debug run. Returning = completed.
        /// The menu stops the coroutine when the Stop dialog ends the run, then calls <see cref="Abort"/>.
        /// </summary>
        public abstract IEnumerator Run(ChamberRun run);

        /// <summary>Cleans up after the run was stopped from the Stop dialog (hide cues, stop sounds).</summary>
        public virtual void Abort(ChamberRun run) { }
    }

    /// <summary>One run of a chamber, handed to <see cref="ChamberBehaviour.Run"/>.</summary>
    public sealed class ChamberRun
    {
        public bool IsDebug { get; internal set; }
        public bool IsResume { get; internal set; }

        /// <summary>The session. In a Debug run an in-memory record (never saved) that holds the cursor all the same.</summary>
        public SessionRecord Record { get; internal set; }

        /// <summary>Null in a Debug run.</summary>
        public SessionStore Store { get; internal set; }

        /// <summary>Parameter values: the session's snapshot, or the Debug working copy.</summary>
        public ParameterSet Values { get; internal set; }

        public EventLog Log { get; internal set; }
        public LogContext Context => Log.Context;

        /// <summary>Headset volume step entered on Start / Resume (-1 in Debug runs).</summary>
        public int VolumeStep { get; internal set; } = -1;

        /// <summary>The overlay canvas of the chamber menu (build run screens on it; the menu window is hidden).</summary>
        public Transform Canvas { get; internal set; }

        public GlobalActions Actions { get; internal set; }

        /// <summary>True once the Stop dialog ended the run.</summary>
        public bool Stopped { get; internal set; }

        /// <summary>Writes the session file (real sessions; Debug runs keep nothing).</summary>
        public void Save()
        {
            if (!IsDebug && Store != null) Store.Save(Record, DateTime.Now);
        }
    }
}
