using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AcousticChamber.App
{
    /// <summary>
    /// Rules of <c>Magic > App > New Chamber...</c> (PB-13): a copy of <c>Chambers/_Template</c> renamed to a new chamber ID. Pure:
    /// the Editor tool does the file work, these functions decide the names, the text rewrites and the GUID remap.
    /// </summary>
    public static class ChamberCopy
    {
        public const string TemplateId = "Template";
        public const string TemplateFolder = "_Template";
        public const string TemplateLabel = "C00";

        static readonly Regex CodeId = new Regex("^[A-Z][A-Za-z0-9]*$");
        static readonly Regex Label = new Regex("^C[0-9]{2,3}$");
        static readonly Regex MetaGuid = new Regex(@"^guid: ([0-9a-f]{32})\s*$", RegexOptions.Multiline);

        /// <summary>
        /// Null if <paramref name="id"/> can name a new chamber, otherwise why not. Stricter than <see cref="ChamberRules.Problem"/>:
        /// the ID also becomes a namespace and class prefix, so it starts with a capital letter and has letters and digits only.
        /// </summary>
        public static string IdProblem(string id, ICollection<string> existingIds)
        {
            if (string.IsNullOrEmpty(id)) return "the id is empty";
            if (!CodeId.IsMatch(id)) return "the id must start with a capital letter and have letters and digits only (it names the namespace and classes)";
            if (id == TemplateId) return "'" + TemplateId + "' is the template itself";
            if (existingIds != null)
                foreach (var e in existingIds)
                    if (string.Equals(e, id, StringComparison.OrdinalIgnoreCase)) return "a chamber '" + e + "' exists already";
            return ChamberRules.Problem(id, id);
        }

        /// <summary>Null if <paramref name="label"/> is a display ID like C03 (empty is allowed), otherwise why not.</summary>
        public static string LabelProblem(string label) =>
            string.IsNullOrEmpty(label) || Label.IsMatch(label) ? null : "the label looks like C03 (C and 2-3 digits) or stays empty";

        /// <summary>
        /// The new relative path of a file of the template folder ('/' separators): <c>_Template.ChamberDefinition.asset</c> →
        /// <c>Id.ChamberDefinition.asset</c>, <c>Scenes/_Template.unity</c> → <c>Scenes/Id.unity</c>, <c>Scripts/TemplateChamber.cs</c> →
        /// <c>Scripts/IdChamber.cs</c>, <c>AcousticChamber.Chambers.Template.Tests.asmdef</c> → <c>AcousticChamber.Chambers.Id.Tests.asmdef</c>.
        /// Only the file name changes (folders keep their names). A <c>.meta</c> file follows its asset.
        /// </summary>
        public static string RenamePath(string relPath, string id)
        {
            int slash = relPath.LastIndexOf('/');
            string dir = slash >= 0 ? relPath.Substring(0, slash + 1) : "";
            string name = slash >= 0 ? relPath.Substring(slash + 1) : relPath;
            name = name.Replace("AcousticChamber.Chambers." + TemplateId, "AcousticChamber.Chambers." + id);
            if (name.StartsWith(TemplateFolder + ".", StringComparison.Ordinal)) name = id + name.Substring(TemplateFolder.Length);
            else if (name.StartsWith(TemplateId, StringComparison.Ordinal)) name = id + name.Substring(TemplateId.Length);
            return dir + name;
        }

        /// <summary>Text files of a chamber copy that are rewritten (scene and asset YAML, code, asmdefs, params, docs).</summary>
        public static bool IsText(string path)
        {
            string p = path.ToLowerInvariant();
            foreach (var ext in new[] { ".cs", ".asmdef", ".unity", ".asset", ".params", ".md", ".meta", ".mat", ".prefab", ".txt", ".json" })
                if (p.EndsWith(ext, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// Rewrites the template's names in a text file: namespaces and assembly names, the class names <c>TemplateChamber</c> /
        /// <c>TemplateSetup</c> / <c>TemplateTests</c>, folder and scene paths, the Magic menu, the console prefix, the
        /// <c>Id</c> constant and the label C00. Other uses of the word "Template" stay (see <see cref="Leftovers"/>).
        /// </summary>
        public static string Rewrite(string text, string id, string label)
        {
            if (string.IsNullOrEmpty(label)) label = id;
            var sb = new StringBuilder(text);
            sb.Replace("AcousticChamber.Chambers." + TemplateId, "AcousticChamber.Chambers." + id);
            sb.Replace(TemplateId + "Chamber", id + "Chamber");
            sb.Replace(TemplateId + "Setup", id + "Setup");
            sb.Replace(TemplateId + "Tests", id + "Tests");
            sb.Replace("Chambers/" + TemplateFolder, "Chambers/" + id);
            sb.Replace(TemplateFolder + ".unity", id + ".unity");
            sb.Replace(TemplateFolder + ".ChamberDefinition", id + ".ChamberDefinition");
            sb.Replace("(open " + TemplateFolder + ")", "(open " + id + ")");
            sb.Replace("Magic/" + TemplateId + "/", "Magic/" + id + "/");
            sb.Replace("Magic > " + TemplateId + " >", "Magic > " + id + " >");
            sb.Replace("[Chamber:" + TemplateId + "]", "[Chamber:" + id + "]");
            sb.Replace("Id = \"" + TemplateId + "\"", "Id = \"" + id + "\"");
            sb.Replace(TemplateId + " (" + TemplateLabel, id + " (" + label);
            sb.Replace("The " + TemplateId + " chamber", "The " + id + " chamber");
            sb.Replace("the " + TemplateId + " chamber", "the " + id + " chamber");
            sb.Replace("the open " + TemplateId + " scene", "the open " + id + " scene");
            sb.Replace("open the " + TemplateId + " scene", "open the " + id + " scene");
            sb.Replace("\"" + TemplateId + " ", "\"" + id + " ");   // Undo names: "Template homing"
            sb.Replace("run Magic > App > Step 4 - " + TemplateId + " chamber stub", "Magic > App > New Chamber... creates it");
            sb.Replace("(" + TemplateLabel + ",", "(" + label + ",");
            return sb.ToString();
        }

        /// <summary>Lines that still contain the word "Template" after <see cref="Rewrite"/> (for the console, to review by hand).</summary>
        public static List<string> Leftovers(string text)
        {
            var r = new List<string>();
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                if (Regex.IsMatch(lines[i], @"\b" + TemplateId + @"\b")) r.Add((i + 1) + ": " + lines[i].Trim());
            return r;
        }

        /// <summary>The GUID of a .meta file, or null.</summary>
        public static string GuidOfMeta(string metaText)
        {
            var m = MetaGuid.Match(metaText);
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>Replaces every old GUID of <paramref name="map"/> (32 hex digits) with its new one.</summary>
        public static string RemapGuids(string text, IDictionary<string, string> map)
        {
            if (map.Count == 0) return text;
            return Regex.Replace(text, "[0-9a-f]{32}", m => map.TryGetValue(m.Value, out var n) ? n : m.Value);
        }
    }
}
