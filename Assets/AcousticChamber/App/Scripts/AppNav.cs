using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AcousticChamber.App
{
    /// <summary>
    /// Scene switches of the app (Architecture section 5): Hall to a chamber and back. Each scene is loaded singly behind a
    /// fade to black (<see cref="FadeSec"/> out with the scene's <see cref="OVRScreenFade"/>, the same back in from the new
    /// scene's; <see cref="SceneFade"/> adds it). Overlay panels are compositor layers the fade does not cover, so callers
    /// hide their window first. Entering a chamber records it in <see cref="AppState"/>.
    /// </summary>
    public static class AppNav
    {
        public const float FadeSec = 0.3f;
        public const string HallScenePath = "Assets/AcousticChamber/App/Scenes/MainHall.unity";

        /// <summary>True from the start of a switch until the new scene has loaded; further requests are ignored.</summary>
        public static bool Busy { get; private set; }

        public static string StatePath => Path.Combine(Application.persistentDataPath, AppState.FileName);

        /// <summary>Set by the Hall's launch routing; the chamber menu reads and clears it (<see cref="TakeRoutedAtLaunch"/>).</summary>
        public static bool RoutedAtLaunch { get; set; }

        public static bool TakeRoutedAtLaunch()
        {
            bool r = RoutedAtLaunch;
            RoutedAtLaunch = false;
            return r;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Busy = false;
            RoutedAtLaunch = false;
        }

        /// <summary>Null if <paramref name="scenePath"/> can be loaded, otherwise why not.</summary>
        public static string Problem(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath)) return "no scene set";
            if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0) return "scene not in the build (run Magic > App > Sync Chambers): " + scenePath;
            return null;
        }

        /// <summary>Records the chamber as the last one and loads its scene. False if busy or the scene cannot be loaded.</summary>
        public static bool EnterChamber(ChamberDefinition chamber)
        {
            if (chamber == null || Busy) return false;
            string problem = Problem(chamber.scenePath);
            if (problem != null)
            {
                Debug.LogError("[GPL] AppNav: " + chamber.id + ": " + problem);
                return false;
            }
            try
            {
                var state = AppState.Load(StatePath);
                state.LastChamber = chamber.id;
                state.Save(StatePath);
            }
            catch (IOException e) { Debug.LogWarning("[GPL] AppNav: could not save " + AppState.FileName + ": " + e.Message); }
            Debug.Log("[GPL] AppNav: enter chamber " + chamber.id);
            return Load(chamber.scenePath);
        }

        /// <summary>Loads the Main Hall.</summary>
        public static bool BackToHall()
        {
            if (Busy) return false;
            Debug.Log("[GPL] AppNav: back to the Hall");
            return Load(HallScenePath);
        }

        static bool Load(string scenePath)
        {
            string problem = Problem(scenePath);
            if (problem != null)
            {
                Debug.LogError("[GPL] AppNav: " + problem);
                return false;
            }
            Busy = true;
            Runner.Get().StartCoroutine(Switch(scenePath));
            return true;
        }

        static IEnumerator Switch(string scenePath)
        {
            Time.timeScale = 1f; // a scene must never start frozen (the Stop dialog sets 0)
            var fade = OVRScreenFade.instance;
            if (fade != null)
            {
                fade.fadeTime = FadeSec;
                fade.FadeOut();
                yield return new WaitForSecondsRealtime(FadeSec);
            }
            var op = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            while (op != null && !op.isDone) yield return null;
            Busy = false;
        }

        /// <summary>Runs the switch coroutine; survives the scene change.</summary>
        sealed class Runner : MonoBehaviour
        {
            static Runner _instance;

            public static Runner Get()
            {
                if (_instance != null) return _instance;
                var go = new GameObject("GPL AppNav");
                DontDestroyOnLoad(go);
                return _instance = go.AddComponent<Runner>();
            }
        }
    }
}
