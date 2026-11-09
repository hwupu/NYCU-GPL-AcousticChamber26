using GPL.Input;
using GPL.UI;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// Headset test panel for PB-09 (no study logic, Sandbox): a DEBUG panel holding <see cref="HearingLossTestView"/> (the pure-tone
    /// check of GPL.HearingLoss), with the output's state in the status bar. C03 shows the same view on its Config page.
    /// </summary>
    public class HearingLossTestPanel : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1100f, 760f);

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            var actions = GlobalActions.Instance != null ? GlobalActions.Instance : FindAnyObjectByType<GlobalActions>();
            if (actions != null) actions.Use(new BindingTable());
            var panel = TestPanelKit.Panel(transform, Size, "GPL.HearingLoss test panel");
            var status = panel.Surface.AddStatus("");
            HearingLossTestView.Create(panel.Surface.Body, status);
        }
    }
}
