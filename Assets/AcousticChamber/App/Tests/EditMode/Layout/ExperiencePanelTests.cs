using System.Reflection;
using GPL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AcousticChamber.App.Tests
{
    /// <summary>The experience chambers' panels (built from the layout kit) fit their canvas: the content needs no more height than the body has.</summary>
    public class ExperiencePanelTests
    {
        GameObject _canvas;
        AudioClip[] _clips;

        [SetUp]
        public void SetUp()
        {
            UiKit.MotionSuppressed = true;
            _canvas = new GameObject("Experience Canvas", typeof(RectTransform), typeof(Canvas)) { layer = OverlayPanel.HiddenCanvasLayer };
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _clips = new[] { AudioClip.Create("Speech", 48000, 1, 48000, false), AudioClip.Create("Noise", 48000, 1, 48000, false),
                             AudioClip.Create("Music", 48000, 1, 48000, false) };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            foreach (var c in _clips) Object.DestroyImmediate(c);
            UiKit.MotionSuppressed = false;
        }

        T Build<T>(Vector2 size, System.Action<T> setup = null) where T : MonoBehaviour
        {
            var c = _canvas.AddComponent<T>();
            setup?.Invoke(c);
            typeof(T).GetMethod("BuildPanel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, null);
            Assert.AreEqual(size, ((RectTransform)_canvas.transform).sizeDelta, "the panel sets the canvas size");
            var page = _canvas.GetComponentInChildren<UiSurface>().Root;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)page.transform);
            return c;
        }

        void AssertFits()
        {
            var surface = _canvas.GetComponentInChildren<UiSurface>();
            var body = surface.Body;
            float needed = LayoutUtility.GetPreferredHeight(body);
            Assert.LessOrEqual(needed, body.rect.height + 0.5f, "the body content needs " + needed + " u of " + body.rect.height);
            Assert.IsNotNull(surface.Toolbar, "a toolbar with the main action");
            Assert.AreEqual(116f, surface.NavigationBar.rect.height, 0.5f);
        }

        [Test]
        public void MovingSourcePanelFits()
        {
            Build<Chambers.MovingSource.MovingSourceExperience>(Chambers.MovingSource.MovingSourceExperience.Size,
                e => { e.clips = _clips; e.clipNames = new[] { "Speech", "Noise", "Music" }; });
            AssertFits();
        }

        [Test]
        public void HiddenSourcePanelFits()
        {
            Build<Chambers.HiddenSource.HiddenSourceExperience>(Chambers.HiddenSource.HiddenSourceExperience.Size);
            AssertFits();
        }

        [Test]
        public void HearingLossPanelFits()
        {
            // 2 noises + 7 library recordings: 9 sounds, 3 rows of 4 (PO 2026-11-10: no HW2 clips)
            var library = new AudioClip[7];
            for (int i = 0; i < library.Length; i++) library[i] = _clips[i % _clips.Length];
            var names = new[] { "Horn", "Horn ×2", "Choked horn", "Pap horn", "Pistol", "Opera voice", "Trumpet" };
            Build<Chambers.HearingLoss.HearingLossExperience>(Chambers.HearingLoss.HearingLossExperience.Size,
                e => { e.libraryClips = library; e.libraryNames = names; });
            AssertFits();
            var body = _canvas.GetComponentInChildren<UiSurface>().Body;
            Assert.AreEqual("Sounds", body.Find("Sounds Box").GetComponentInChildren<TMPro.TextMeshProUGUI>().text);
            var grid = body.Find("Sound Grid");
            Assert.AreEqual(3, grid.childCount - 1, "9 sounds in 3 rows of 4");
            Assert.AreEqual(9, grid.GetComponentsInChildren<UiButton>().Length);
            Assert.IsNull(_canvas.GetComponentInChildren<UiSurface>().Body.Find("Pure tone"), "no tone row");
            Assert.IsNotNull(_canvas.GetComponentInChildren<UiSurface>().Toolbar.Find("Config"));
            // every label fits its button on one line (the reason for 4 columns)
            foreach (var label in grid.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
            {
                label.ForceMeshUpdate();
                float room = ((RectTransform)label.transform.parent.parent).rect.width - 32f;
                Assert.LessOrEqual(label.preferredWidth, room + 0.5f, "'" + label.text + "' fits its button");
            }
        }
    }
}
