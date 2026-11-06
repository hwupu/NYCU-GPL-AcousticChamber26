using System.Collections.Generic;
using System.IO;
using GPL.Core;

namespace AcousticChamber.App
{
    /// <summary>
    /// App-wide state that outlives a scene: the last chamber entered (the launch routing, PB-06b, opens its menu when it
    /// has unfinished sessions). A key=value file <c>app.state</c> in <c>persistentDataPath</c>, written atomically. A
    /// missing or unreadable file is an empty state.
    /// </summary>
    public sealed class AppState
    {
        public const string FileName = "app.state";
        const string KeyLastChamber = "last_chamber";

        /// <summary>ID of the last chamber entered ("" = none).</summary>
        public string LastChamber = "";

        public static AppState Load(string path)
        {
            var s = new AppState();
            try
            {
                if (!AtomicFile.Exists(path)) return s;
                var d = KeyValueText.ParseToDictionary(AtomicFile.ReadAllText(path));
                if (d.TryGetValue(KeyLastChamber, out var v)) s.LastChamber = v ?? "";
            }
            catch (IOException) { }
            return s;
        }

        public void Save(string path)
        {
            var pairs = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(KeyLastChamber, LastChamber ?? "") };
            AtomicFile.WriteAllText(path, KeyValueText.Serialize(pairs, "AcousticChamber26 app state"));
        }
    }
}
