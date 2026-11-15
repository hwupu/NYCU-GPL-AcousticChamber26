namespace GPL.Core
{
    /// <summary>
    /// A processor on the app's final audio output (a component with <c>OnAudioFilterRead</c> on the AudioListener, e.g. the
    /// hearing-loss filter). Measurements of the rendered sound (GPL.Acoustics RT60) set <see cref="Bypassed"/> while they record and
    /// restore it afterwards; a bypassed filter must pass the audio through unchanged and add nothing to it.
    /// </summary>
    public interface IOutputFilter
    {
        /// <summary>Short name for reports, e.g. <c>OutputHearingLoss</c>.</summary>
        string FilterName { get; }

        /// <summary>True = the audio passes unchanged. Main thread; read on the audio thread.</summary>
        bool Bypassed { get; set; }

        /// <summary>Current setting for a report, e.g. <c>hl L=0,0,...;R=0,0,...</c>.</summary>
        string Describe();
    }
}
