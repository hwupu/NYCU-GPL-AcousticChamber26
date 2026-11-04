using TMPro;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// A TextMeshPro text showing the string for <see cref="key"/> in the current language (<see cref="Loc"/>); refreshes
    /// when the language changes. One font family (Noto Sans TC) covers both languages, so no font switching is needed.
    /// From HW2's LocalizedText.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string key;
        object[] _args = System.Array.Empty<object>();
        TMP_Text _text;

        void OnEnable()
        {
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => Loc.Changed -= Refresh;

        public void Set(string newKey, params object[] args)
        {
            key = newKey;
            _args = args ?? System.Array.Empty<object>();
            Refresh();
        }

        public void SetArgs(params object[] args)
        {
            _args = args ?? System.Array.Empty<object>();
            Refresh();
        }

        public void Refresh()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (!string.IsNullOrEmpty(key)) _text.text = Loc.Get(key, _args);
        }
    }
}
