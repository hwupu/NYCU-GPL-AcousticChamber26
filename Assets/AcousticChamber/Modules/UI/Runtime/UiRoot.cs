using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// Scene entry point of GPL.UI: makes <see cref="theme"/> the theme of <see cref="UiKit"/> and registers the module's
    /// strings. Put one in every scene that shows GUI (Editor setup does this); it runs before other UI scripts.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class UiRoot : MonoBehaviour
    {
        public UiTheme theme;

        void Awake()
        {
            if (theme != null) UiKit.Theme = theme;
            else Debug.LogWarning("[GPL] UiRoot: no UiTheme assigned; using default colors and TextMeshPro's default font (no 中文).");
            UiStrings.EnsureRegistered();
        }
    }
}
