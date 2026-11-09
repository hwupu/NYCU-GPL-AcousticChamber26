using System;
using System.Collections.Generic;

namespace GPL.Stimuli
{
    /// <summary>One recording in the library: where it is, what it is, who made it, under which licence.</summary>
    public sealed class LibraryEntry
    {
        public string Id { get; }
        /// <summary>Path relative to <see cref="StimulusLibrary.Folder"/>.</summary>
        public string File { get; }
        public string Title { get; }
        public string Creator { get; }
        public string Source { get; }
        /// <summary>SPDX id where one exists (CC-BY-SA-4.0, ...), else a short phrase.</summary>
        public string Licence { get; }
        /// <summary>The credit line to print (paper, Credits page).</summary>
        public string Attribution { get; }
        public string Notes { get; }

        public LibraryEntry(string id, string file, string title, string creator, string source, string licence, string attribution,
                            string notes = "")
        {
            Id = id; File = file; Title = title; Creator = creator; Source = source; Licence = licence;
            Attribution = attribution; Notes = notes ?? "";
        }

        /// <summary>Log form: <c>recording=openair_bicycle_classic licence=CC-BY-SA-4.0</c>.</summary>
        public string Note() => "recording=" + Id + " licence=" + Licence.Replace(' ', '-');
    }

    /// <summary>
    /// The recordings shipped with GPL.Stimuli in <c>Library/</c> (README "Library", docs/AssetCredits.md). Pure catalog; the Editor
    /// loads the clips by <see cref="AssetPath"/>, <c>StimulusClips.Leveled</c> sets their level.
    /// </summary>
    public static class StimulusLibrary
    {
        public const string Folder = "Assets/AcousticChamber/Modules/Stimuli/Library";

        static readonly List<LibraryEntry> _all = new List<LibraryEntry>(OpenAir.Entries);

        public static IReadOnlyList<LibraryEntry> All => _all;

        public static LibraryEntry Find(string id) => _all.Find(e => e.Id == id);

        public static LibraryEntry FindByFile(string relativePath) =>
            _all.Find(e => string.Equals(e.File, relativePath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));

        public static string AssetPath(LibraryEntry e) => Folder + "/" + e.File;
    }
}
