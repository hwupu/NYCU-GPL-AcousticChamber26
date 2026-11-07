using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AcousticChamber.App.Editor
{
    /// <summary>
    /// <c>Magic > App > New Chamber...</c> (PB-13): copies <c>Chambers/_Template</c> to <c>Chambers/&lt;Id&gt;</c> and renames it:
    /// file names, namespaces, assemblies, classes (<c>&lt;Id&gt;Chamber</c>, <c>&lt;Id&gt;Setup</c>), the Magic menu, the console
    /// prefix, scene and definition paths; every asset gets a new GUID (references inside the copy follow, references to shared
    /// assets stay). The docs become empty skeletons, the tests a starter test. Then the definition is filled in and the catalog
    /// synced. Rules: <see cref="ChamberCopy"/>.
    /// </summary>
    public static class NewChamber
    {
        const string Log = "[GPL] New Chamber: ";

        [MenuItem("Magic/App/New Chamber...", priority = 21)]
        static void Open() => NewChamberWindow.Open();

        /// <summary>IDs of every ChamberDefinition in the project plus every folder under Chambers/ (without a leading '_').</summary>
        public static List<string> ExistingIds()
        {
            var ids = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(ChamberDefinition)))
            {
                var def = AssetDatabase.LoadAssetAtPath<ChamberDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def != null && !string.IsNullOrEmpty(def.id)) ids.Add(def.id);
            }
            if (Directory.Exists(HallSetup.ChambersRoot))
                foreach (var dir in Directory.GetDirectories(HallSetup.ChambersRoot))
                    ids.Add(Path.GetFileName(dir).TrimStart('_'));
            return ids;
        }

        /// <summary>Null if the chamber can be created, otherwise why not.</summary>
        public static string Problem(string id, string label) =>
            ChamberCopy.IdProblem(id, ExistingIds()) ?? ChamberCopy.LabelProblem(label);

        /// <summary>Creates the chamber. Returns null on success, otherwise the problem (nothing is written then).</summary>
        public static string Create(string id, string label, string title)
        {
            id = (id ?? "").Trim();
            label = (label ?? "").Trim();
            title = string.IsNullOrWhiteSpace(title) ? id : title.Trim();
            string problem = Problem(id, label);
            if (problem != null) { Debug.LogError(Log + problem); return problem; }

            string src = HallSetup.ChambersRoot + "/" + ChamberCopy.TemplateFolder;
            string dst = HallSetup.ChambersRoot + "/" + id;
            if (!Directory.Exists(src)) return Fail("the template folder " + src + " is missing");
            if (Directory.Exists(dst)) return Fail(dst + " exists already");

            // skipped: the docs (replaced by skeletons) and the Template's own regression tests (replaced by a starter test)
            bool Skip(string rel) =>
                rel.StartsWith("Docs/", StringComparison.Ordinal) && rel != "Docs.meta" ||
                rel.StartsWith("Tests/EditMode/" + ChamberCopy.TemplateId + "Tests.cs", StringComparison.Ordinal);

            var files = Directory.GetFiles(src, "*", SearchOption.AllDirectories)
                                 .Select(f => f.Replace('\\', '/').Substring(src.Length + 1))
                                 .Where(rel => !Skip(rel)).ToList();

            // new GUID for every asset and folder of the copy
            var map = new Dictionary<string, string>();
            foreach (var rel in files.Where(r => r.EndsWith(".meta", StringComparison.Ordinal)))
            {
                string guid = ChamberCopy.GuidOfMeta(File.ReadAllText(src + "/" + rel));
                if (guid != null && !map.ContainsKey(guid)) map[guid] = Guid.NewGuid().ToString("N");
            }

            var leftovers = new List<string>();
            try
            {
                foreach (var rel in files)
                {
                    string target = dst + "/" + ChamberCopy.RenamePath(rel, id);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    if (ChamberCopy.IsText(rel))
                    {
                        string text = ChamberCopy.RemapGuids(ChamberCopy.Rewrite(File.ReadAllText(src + "/" + rel), id, label), map);
                        File.WriteAllText(target, text);
                        if (rel.EndsWith(".cs", StringComparison.Ordinal))
                            foreach (var l in ChamberCopy.Leftovers(text)) leftovers.Add(ChamberCopy.RenamePath(rel, id) + ":" + l);
                    }
                    else File.Copy(src + "/" + rel, target);
                }
                WriteSkeletons(dst, id, label, title);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return Fail("copy failed (" + e.Message + "); remove " + dst + " before trying again");
            }

            AssetDatabase.Refresh();
            string defPath = dst + "/" + id + ".ChamberDefinition.asset";
            var def = AssetDatabase.LoadAssetAtPath<ChamberDefinition>(defPath);
            if (def == null) return Fail("the copied definition did not import: " + defPath);
            def.id = id;
            def.label = label;
            def.title = title;
            def.description = "New chamber (copied from _Template). Write Docs/Brief.md and Design.md first.";
            def.scenePath = dst + "/Scenes/" + id + ".unity";
            def.version = "0.1";
            def.enabled = true;
            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssets();
            HallSetup.SyncChambers();

            foreach (var l in leftovers) Debug.LogWarning(Log + "still says 'Template', check: " + l);
            Debug.Log(Log + "created " + dst + " (" + ChamberRules.MenuTitle(label, id) + ", " + files.Count + " files, " + map.Count +
                      " new GUIDs). Menu Magic > " + id + "; open Scenes/" + id + ".unity. Write Docs/Brief.md and Design.md before code (rule 5).");
            return null;
        }

        static string Fail(string problem)
        {
            Debug.LogError(Log + problem);
            return problem;
        }

        static void WriteSkeletons(string dst, string id, string label, string title)
        {
            string name = ChamberRules.MenuTitle(label, id);
            string status = "**Status:** draft (copied from `_Template` on " + DateTime.Now.ToString("yyyy-MM-dd") +
                            "). Fill in and get the PO's approval before code (data first, rule 5).";
            Directory.CreateDirectory(dst + "/Docs");
            void Doc(string file, string heading, params string[] sections)
            {
                var text = "# " + id + " (" + (string.IsNullOrEmpty(label) ? "no label" : label) + ") — " + heading + "\n\n" + status + "\n";
                foreach (var s in sections) text += "\n## " + s + "\nTODO\n";
                File.WriteAllText(dst + "/Docs/" + file, text);
            }
            Doc("Brief.md", "Brief", "Question and hypothesis", "Task in one paragraph", "Conditions", "Modules used");
            Doc("Design.md", "Design", "Posture and setting", "Flow (stages and resume policies)", "Trial", "Parameters (preset Default)",
                "Stimuli and levels", "Input bindings");
            Doc("DataFormat.md", "Data format", "Files", "Chamber columns", "Events");
            Doc("AnalysisPlan.md", "Analysis plan", "Measures", "Exclusions", "Planned tests");
            File.WriteAllText(dst + "/Docs/SceneSetup.md",
                "# " + id + " — Scene setup\n\n" +
                "Menu **Magic > " + id + "** (copied from `_Template`): Step 1 chamber menu, Step 2 homing, Step 3 instruction slides\n" +
                "(placeholders). The scene `Scenes/" + id + ".unity` already has them; re-run a step after changing it.\n");

            string tests = dst + "/Tests/EditMode";
            Directory.CreateDirectory(tests);
            File.WriteAllText(tests + "/" + id + "Tests.cs",
                "using NUnit.Framework;\n\n" +
                "namespace AcousticChamber.Chambers." + id + ".Tests\n{\n" +
                "    /// <summary>Starter tests of " + name + " (" + title + "); replace with the chamber's own.</summary>\n" +
                "    public class " + id + "Tests\n    {\n" +
                "        [Test]\n" +
                "        public void DefaultsMakeAPlan()\n        {\n" +
                "            var v = " + id + "Chamber.CreateSchema().Defaults();\n" +
                "            Assert.IsNotNull(" + id + "Chamber.Plan(v));\n" +
                "            Assert.IsNotEmpty(" + id + "Chamber.Plan(v).Describe());\n" +
                "        }\n    }\n}\n");
        }

        /// <summary>Headless (mirror project): creates the chamber "NewProbe" (C99); the test run that follows compiles it.</summary>
        public static void BatchProbe()
        {
            if (Create("NewProbe", "C99", "Probe") != null) EditorApplication.Exit(1);
        }
    }

    /// <summary>The dialog of <c>Magic > App > New Chamber...</c>.</summary>
    public class NewChamberWindow : EditorWindow
    {
        string _id = "", _label = "", _title = "";
        string _result;

        public static void Open()
        {
            var w = GetWindow<NewChamberWindow>(true, "New Chamber", true);
            w.minSize = new Vector2(460, 230);
            w.ShowUtility();
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Copies Chambers/_Template to Chambers/<Id> and renames it. The ID names the data files, the " +
                                    "namespace and the classes: never change it once data exist.", MessageType.Info);
            _id = EditorGUILayout.TextField(new GUIContent("Chamber ID", "e.g. MovingSource (capital first, letters and digits)"), _id);
            _label = EditorGUILayout.TextField(new GUIContent("Label", "Display ID, e.g. C03 (Hall and README only)"), _label);
            _title = EditorGUILayout.TextField(new GUIContent("Title", "Hall card title; empty = the ID"), _title);
            string problem = string.IsNullOrEmpty(_id) ? "Enter an ID." : NewChamber.Problem(_id.Trim(), _label.Trim());
            if (problem != null) EditorGUILayout.HelpBox(problem, MessageType.Warning);
            if (_result != null) EditorGUILayout.HelpBox(_result, MessageType.Error);
            using (new EditorGUI.DisabledScope(problem != null))
            {
                if (GUILayout.Button("Create"))
                {
                    _result = NewChamber.Create(_id, _label, _title);
                    if (_result == null) Close();
                }
            }
        }
    }
}
