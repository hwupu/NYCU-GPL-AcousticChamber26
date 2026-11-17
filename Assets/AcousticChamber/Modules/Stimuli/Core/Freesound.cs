namespace GPL.Stimuli
{
    /// <summary>
    /// Freesound recordings in <c>Library/Freesound</c> (freesound.org), kept unchanged as downloaded (MP3, Freesound's file names:
    /// <c>&lt;sound id&gt;__&lt;user&gt;__&lt;name&gt;</c>). Added by the PO 2026-11-17. CC0: no credit required; credited anyway.
    /// </summary>
    public static class Freesound
    {
        const string Licence = "CC0-1.0";

        static LibraryEntry E(string id, int soundId, string file, string title, string user, string format) =>
            new LibraryEntry(id, "Freesound/" + file, title, user + " (Freesound)", "https://freesound.org/s/" + soundId + "/", Licence,
                             title + " by " + user + " -- https://freesound.org/s/" + soundId + "/ -- License: Creative Commons 0",
                             format + "; unchanged as downloaded");

        public static readonly LibraryEntry[] Entries =
        {
            E("freesound_toy_ducks_quacking", 348791, "348791__nebyoolae__toy-ducks-quacking.mp3", "Toy Ducks Quacking", "nebyoolae",
              "MP3 mono 44.1 kHz 128 kb/s, about 4.9 s (several squeezes of rubber ducks)"),
            E("freesound_quack", 791152, "791152__mari0411__quack.mp3", "quack", "Mari0411",
              "MP3 stereo 44.1 kHz 112 kb/s, about 0.34 s (one quack)"),
        };
    }
}
