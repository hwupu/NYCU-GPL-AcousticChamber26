using UnityEngine;

namespace GPL.UI
{
    /// <summary>Shows the accent outline of a button while the controller ray is on it (called by <see cref="UiPointer"/>).</summary>
    public class HoverHighlight : MonoBehaviour
    {
        public GameObject outline;

        public void SetHover(bool on)
        {
            if (outline != null && outline.activeSelf != on) outline.SetActive(on);
        }

        void OnDisable() => SetHover(false);
    }
}
