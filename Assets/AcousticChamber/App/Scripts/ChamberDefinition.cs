using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// One chamber as the app knows it (Architecture section 5): an asset in the chamber's folder,
    /// <c>Chambers/&lt;Folder&gt;/&lt;Folder&gt;.ChamberDefinition.asset</c>. <c>Magic > App > Sync Chambers</c> collects all
    /// definitions into the <see cref="ChamberCatalog"/> and the build scene list. The App never references a chamber's code.
    /// Still to come with PB-07: supported modes, default resume policy, input bindings, posture.
    /// </summary>
    [CreateAssetMenu(fileName = "Chamber.ChamberDefinition", menuName = "AcousticChamber/Chamber Definition")]
    public class ChamberDefinition : ScriptableObject
    {
        [Tooltip("Chamber name: data folder, file names, 'chamber' column. Letters, digits and '-' only; never change it once data exist.")]
        public string id = "";

        [Tooltip("Display ID (C01, C02, ...): Hall and README only; may change without touching data.")]
        public string label = "";

        [Tooltip("Title on the Hall card (system screens are English only).")]
        public string title = "";

        [TextArea(2, 5)] public string description = "";

        [Tooltip("Cover image of the Hall card (16:9). Empty = a solid placeholder.")]
        public Texture2D thumbnail;

        [Tooltip("The chamber's scene (asset path); set by the chamber's setup step.")]
        public string scenePath = "";

        [Tooltip("Written into every log row.")]
        public string version = "0.1";

        [Tooltip("Off = hidden from the Hall (the scene stays in the build).")]
        public bool enabled = true;

        /// <summary>"C04 · HumPerHW2".</summary>
        public string MenuTitle => ChamberRules.MenuTitle(label, id);

        /// <summary>The card title: <see cref="title"/>, or the ID when it is empty.</summary>
        public string CardTitle => string.IsNullOrEmpty(title) ? id : title;
    }
}
