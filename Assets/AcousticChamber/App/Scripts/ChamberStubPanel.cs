using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// Stand-in for a chamber menu (PB-06a): the header with ‹ back to the Hall and the chamber's title, nothing else. The
    /// real chamber menu (Study ID, Start, Debug) replaces it in PB-07.
    /// </summary>
    public class ChamberStubPanel : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 700f);

        public ChamberDefinition chamber;

        UiPanel _panel;

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            var th = UiKit.Theme;
            var canvas = (RectTransform)transform;
            UiKit.Clear(canvas);
            canvas.sizeDelta = Size;
            _panel = UiPanel.Create(canvas, "Stub", Size);
            _panel.Surface.RootBack = Back;
            UiNavigationBar.Create(_panel.Surface, chamber != null ? chamber.MenuTitle : "Chamber", UiNavigationBar.Leading.Back);
            var note = UiKit.Text(_panel.Surface.Body, "Note", "Stub chamber scene.\nThe chamber menu (Study ID, Start, Debug) comes with PB-07.\n‹ returns to the Main Hall.",
                                  th.bodySize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.Center);
            UiLay.Size(note.gameObject, prefW: 0f, flexW: 1f, flexH: 1f);
        }

        void Back()
        {
            if (AppNav.Busy) return;
            _panel.Shown = false;
            if (!AppNav.BackToHall()) _panel.Shown = true;
        }
    }
}
