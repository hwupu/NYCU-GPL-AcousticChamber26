using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// The icon library: one white sprite per icon, looked up by name (the names of docs/design/src/icons, without the
    /// "akar-icons--" prefix: "chevron-left", "gear"). Built by <c>UiSetup.EnsureIconSet</c> from the PNGs that
    /// <c>pnpm icons</c> exports. Icons are white; <see cref="UiIcon"/> tints them with the label color.
    /// </summary>
    [CreateAssetMenu(menuName = "GPL/UI Icon Set", fileName = "UiIconSet")]
    public class UiIconSet : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string name;
            public Sprite sprite;
        }

        public List<Entry> icons = new List<Entry>();

        Dictionary<string, Sprite> _map;

        /// <summary>The sprite of <paramref name="name"/>, or null (a missing icon is logged once by the caller).</summary>
        public Sprite Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_map == null || _map.Count != icons.Count)
            {
                _map = new Dictionary<string, Sprite>(icons.Count);
                foreach (var e in icons) _map[e.name] = e.sprite;
            }
            return _map.TryGetValue(name, out var s) ? s : null;
        }

        void OnValidate() => _map = null;
    }
}
