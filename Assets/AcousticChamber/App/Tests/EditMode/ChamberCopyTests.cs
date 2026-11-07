using System.Collections.Generic;
using NUnit.Framework;

namespace AcousticChamber.App.Tests
{
    public class ChamberCopyTests
    {
        [Test]
        public void IdRules()
        {
            var existing = new[] { "Template", "HumPerHW2", "HearingLoss" };
            Assert.IsNull(ChamberCopy.IdProblem("MovingSource", existing));
            Assert.IsNotNull(ChamberCopy.IdProblem("", existing));
            Assert.IsNotNull(ChamberCopy.IdProblem("movingSource", existing));   // lower-case start
            Assert.IsNotNull(ChamberCopy.IdProblem("Moving-Source", existing));  // '-' cannot name a namespace
            Assert.IsNotNull(ChamberCopy.IdProblem("Moving Source", existing));
            Assert.IsNotNull(ChamberCopy.IdProblem("Template", existing));
            Assert.IsNotNull(ChamberCopy.IdProblem("Hearingloss", existing)); // HearingLoss exists (case-insensitive: file systems)
            Assert.IsNull(ChamberCopy.LabelProblem("C03"));
            Assert.IsNull(ChamberCopy.LabelProblem(""));
            Assert.IsNotNull(ChamberCopy.LabelProblem("3"));
        }

        [Test]
        public void RenamePaths()
        {
            Assert.AreEqual("MovingSource.ChamberDefinition.asset", ChamberCopy.RenamePath("_Template.ChamberDefinition.asset", "MovingSource"));
            Assert.AreEqual("MovingSource.ChamberDefinition.asset.meta", ChamberCopy.RenamePath("_Template.ChamberDefinition.asset.meta", "MovingSource"));
            Assert.AreEqual("Scenes/MovingSource.unity", ChamberCopy.RenamePath("Scenes/_Template.unity", "MovingSource"));
            Assert.AreEqual("Scripts/MovingSourceChamber.cs", ChamberCopy.RenamePath("Scripts/TemplateChamber.cs", "MovingSource"));
            Assert.AreEqual("Editor/MovingSourceSetup.cs.meta", ChamberCopy.RenamePath("Editor/TemplateSetup.cs.meta", "MovingSource"));
            Assert.AreEqual("Tests/EditMode/AcousticChamber.Chambers.MovingSource.Tests.asmdef",
                            ChamberCopy.RenamePath("Tests/EditMode/AcousticChamber.Chambers.Template.Tests.asmdef", "MovingSource"));
            Assert.AreEqual("Slides/Placeholder-1.png", ChamberCopy.RenamePath("Slides/Placeholder-1.png", "MovingSource"));
            Assert.AreEqual("Config/default.params", ChamberCopy.RenamePath("Config/default.params", "MovingSource"));
        }

        [Test]
        public void RewriteNames()
        {
            const string code =
                "namespace AcousticChamber.Chambers.Template\n" +
                "    /// The Template chamber (C00, Docs/Design.md)\n" +
                "    public class TemplateChamber : ProtocolChamber\n" +
                "        public const string Id = \"Template\";\n" +
                "        public const string Folder = \"Assets/AcousticChamber/Chambers/_Template\";\n" +
                "        public const string ScenePath = Folder + \"/Scenes/_Template.unity\";\n" +
                "        [MenuItem(\"Magic/Template/Step 1 - Chamber menu (open _Template)\", priority = 100)]\n" +
                "        Debug.LogError(\"[Chamber:Template] run Magic > Template > Step 2\");\n" +
                "# Template (C00) preset Default.\n" +
                "  m_EditorClassIdentifier: AcousticChamber.Chambers.Template::AcousticChamber.Chambers.Template.TemplateChamber\n" +
                "  m_Name: _Template.ChamberDefinition\n";
            string r = ChamberCopy.Rewrite(code, "MovingSource", "C03");
            StringAssert.Contains("namespace AcousticChamber.Chambers.MovingSource\n", r);
            StringAssert.Contains("The MovingSource chamber (C03, Docs", r);
            StringAssert.Contains("class MovingSourceChamber", r);
            StringAssert.Contains("Id = \"MovingSource\"", r);
            StringAssert.Contains("Chambers/MovingSource\"", r);
            StringAssert.Contains("/Scenes/MovingSource.unity", r);
            StringAssert.Contains("Magic/MovingSource/Step 1 - Chamber menu (open MovingSource)", r);
            StringAssert.Contains("[Chamber:MovingSource] run Magic > MovingSource > Step 2", r);
            StringAssert.Contains("# MovingSource (C03) preset", r);
            StringAssert.Contains("AcousticChamber.Chambers.MovingSource::AcousticChamber.Chambers.MovingSource.MovingSourceChamber", r);
            StringAssert.Contains("m_Name: MovingSource.ChamberDefinition", r);
            CollectionAssert.IsEmpty(ChamberCopy.Leftovers(r));
            Assert.AreEqual(1, ChamberCopy.Leftovers("a\nsome Template word\n").Count);

            const string setup =
                "    /// <summary>Magic steps of the Template chamber. Idempotent; they edit only the open Template scene.</summary>\n" +
                "    Debug.LogError(\"x missing: run Magic > App > Step 4 - Template chamber stub.\");\n" +
                "    Undo.RecordObject(logic, \"Template homing\");\n" +
                "    /// <summary>Headless (mirror project): open the Template scene, run Steps 1 to 3, save.</summary>\n";
            string s = ChamberCopy.Rewrite(setup, "MovingSource", "C03");
            CollectionAssert.IsEmpty(ChamberCopy.Leftovers(s), s);
            StringAssert.Contains("missing: Magic > App > New Chamber... creates it.", s);
            StringAssert.Contains("\"MovingSource homing\"", s);
        }

        [Test]
        public void GuidRemap()
        {
            const string meta = "fileFormatVersion: 2\nguid: 0ab546a98cb82ee45860a0928343f1f5\nMonoImporter:\n";
            Assert.AreEqual("0ab546a98cb82ee45860a0928343f1f5", ChamberCopy.GuidOfMeta(meta));
            Assert.IsNull(ChamberCopy.GuidOfMeta("no guid here"));
            var map = new Dictionary<string, string> { { "0ab546a98cb82ee45860a0928343f1f5", "11111111111111111111111111111111" } };
            Assert.AreEqual("m_Script: {fileID: 11500000, guid: 11111111111111111111111111111111, type: 3} other 22222222222222222222222222222222",
                ChamberCopy.RemapGuids("m_Script: {fileID: 11500000, guid: 0ab546a98cb82ee45860a0928343f1f5, type: 3} other 22222222222222222222222222222222", map));
        }
    }
}
