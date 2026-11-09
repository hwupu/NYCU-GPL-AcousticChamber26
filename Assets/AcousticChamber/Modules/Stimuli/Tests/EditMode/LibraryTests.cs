using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GPL.Stimuli.Tests
{
    public class LibraryTests
    {
        [Test]
        public void Catalog_IdsAndFilesAreUnique_FilesExist_CreditsFilled()
        {
            var ids = new HashSet<string>();
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in StimulusLibrary.All)
            {
                Assert.IsTrue(ids.Add(e.Id), "duplicate id " + e.Id);
                Assert.IsTrue(files.Add(e.File), "duplicate file " + e.File);
                Assert.IsNotEmpty(e.Licence, e.Id);
                Assert.IsNotEmpty(e.Attribution, e.Id);
                Assert.IsNotEmpty(e.Source, e.Id);
                Assert.IsTrue(File.Exists(StimulusLibrary.AssetPath(e)), "missing " + StimulusLibrary.AssetPath(e));
            }
            Assert.AreEqual(7, StimulusLibrary.All.Count);
        }

        [Test]
        public void MixToMono_AveragesChannels()
        {
            CollectionAssert.AreEqual(new[] { 0.5f, -0.25f }, Synth.MixToMono(new[] { 1f, 0f, -0.5f, 0f }, 2));
        }

        [Test]
        public void LevelTo_SetsRms_AndRefusesToClip()
        {
            var x = new float[1000];
            for (int i = 0; i < x.Length; i++) x[i] = (float)(0.1 * Math.Sin(i * 0.3));
            double gain = Synth.LevelTo(x, -30);
            Assert.AreEqual(-30.0, Synth.RmsDbfs(x), 1e-4);
            Assert.Greater(gain, -30);
            var y = (float[])x.Clone();
            Assert.Throws<InvalidOperationException>(() => Synth.LevelTo(y, -1));
            CollectionAssert.AreEqual(x, y, "unchanged after a refused level");
        }

        [Test]
        public void LibraryClip_ImportsAsPcm_AndLevels()
        {
            var e = StimulusLibrary.Find("openair_bicycle_classic");
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(StimulusLibrary.AssetPath(e));
            Assert.IsNotNull(clip, "imported");
            var importer = (AudioImporter)AssetImporter.GetAtPath(StimulusLibrary.AssetPath(e));
            Assert.AreEqual(AudioCompressionFormat.PCM, importer.defaultSampleSettings.compressionFormat, "LibraryImport: PCM");
            Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, importer.defaultSampleSettings.loadType, "LibraryImport: Decompress On Load");
            var leveled = StimulusClips.Leveled(clip, -26, out double gainDb);
            try
            {
                var data = new float[leveled.samples];
                leveled.GetData(data, 0);
                Assert.AreEqual(-26.0, Synth.RmsDbfs(data), 0.01);
                Assert.AreEqual(44100, leveled.frequency);
                TestContext.WriteLine(e.Note() + " gain_db=" + gainDb.ToString("0.00"));
            }
            finally { UnityEngine.Object.DestroyImmediate(leveled); }
        }

        [Test]
        public void AppendSilence_AddsZerosAfter_AndLeveledKeepsTheSoundLevel()
        {
            var x = new[] { 0.5f, -0.5f };
            CollectionAssert.AreEqual(new[] { 0.5f, -0.5f, 0f, 0f, 0f }, Synth.AppendSilence(x, 0.3, 10));
            Assert.AreSame(x, Synth.AppendSilence(x, 0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => Synth.AppendSilence(x, -1, 10));

            // all library recordings level to -26 dBFS RMS without clipping (crest factors 17-22 dB); the gap does not count in the level
            foreach (var e in StimulusLibrary.All)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(StimulusLibrary.AssetPath(e));
                var leveled = StimulusClips.Leveled(clip, -26, out _, silenceAfterS: 1.0);
                try
                {
                    Assert.AreEqual(clip.samples + clip.frequency, leveled.samples, e.Id + ": 1 s of silence appended");
                    var data = new float[leveled.samples];
                    leveled.GetData(data, 0);
                    var sound = new float[clip.samples];
                    Array.Copy(data, sound, sound.Length);
                    Assert.AreEqual(-26.0, Synth.RmsDbfs(sound), 0.01, e.Id);
                    for (int i = clip.samples; i < data.Length; i++) Assert.AreEqual(0f, data[i], e.Id + ": silence");
                }
                finally { UnityEngine.Object.DestroyImmediate(leveled); }
            }
        }
    }
}
