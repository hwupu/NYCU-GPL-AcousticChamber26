using System;
using UnityEngine;

namespace GPL.Input
{
    /// <summary>
    /// One per scene. Holds the scene's <see cref="BindingTable"/> and <see cref="ActionReader"/> and watches the global
    /// actions: pressing Stop (Y) raises <see cref="StopRequested"/>, but only while <see cref="StopEnabled"/> (a session
    /// is running) and not <see cref="Blocked"/> (a dialog is already open). Who shows the dialog is up to the listener.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class GlobalActions : MonoBehaviour
    {
        ActionReader _reader;

        public static GlobalActions Instance { get; private set; }

        /// <summary>The scene's reader (Quest controllers by default).</summary>
        public ActionReader Reader => _reader ?? (_reader = new ActionReader(new BindingTable(), new OvrControlSource()));
        public BindingTable Bindings => Reader.Bindings;

        /// <summary>True while a session (real or Debug run) is running; in the Hall and in menus Y does nothing.</summary>
        public bool StopEnabled { get; set; }

        /// <summary>True while the Stop dialog (or another modal step) is open.</summary>
        public bool Blocked { get; set; }

        public event Action StopRequested;

        /// <summary>Replaces the binding table (a chamber's declared actions). Keeps the control source.</summary>
        public void Use(BindingTable bindings) => _reader = new ActionReader(bindings, Reader.Source);

        /// <summary>Replaces the control source (tests, desktop).</summary>
        public void UseSource(IControlSource source) => Reader.Source = source;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("[GPL] Input: more than one GlobalActions in the scene; the last one wins.");
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update() => Poll();

        /// <summary>Checks the global actions once (called every frame).</summary>
        public void Poll()
        {
            if (StopEnabled && !Blocked && Reader.Down(InputActions.Stop)) StopRequested?.Invoke();
        }
    }
}
